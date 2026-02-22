using FluentAssertions;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace ConfluenceSynkMD.Tests.ETL.Core;

public class PipelineRunnerTests
{
    private readonly PipelineRunner _sut;

    public PipelineRunnerTests()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext<PipelineRunner>().Returns(logger);
        _sut = new PipelineRunner(logger);
    }

    private static TranslationBatchContext CreateContext() =>
        new()
        {
            Options = new SyncOptions(SyncMode.Upload, "/tmp/test", "TEST")
        };

    private static IPipelineStep CreateSuccessStep(string name, int items = 1)
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns(name);
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
            .Returns(PipelineResult.Success(name, items, TimeSpan.FromMilliseconds(10)));
        return step;
    }

    // ─── Existing tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_Should_ReturnAbort_When_NoSteps()
    {
        var context = CreateContext();
        var result = await _sut.RunAsync(Array.Empty<IPipelineStep>(), context);
        result.Status.Should().Be(PipelineResultStatus.Abort);
    }

    [Fact]
    public async Task RunAsync_Should_ReturnSuccess_When_AllStepsSucceed()
    {
        var steps = new[] { CreateSuccessStep("E1", 5), CreateSuccessStep("T1", 5) };
        var context = CreateContext();
        var result = await _sut.RunAsync(steps, context);
        result.Status.Should().Be(PipelineResultStatus.Success);
    }

    [Fact]
    public async Task RunAsync_Should_StopAtFirstCriticalError_When_StepFails()
    {
        var step1 = CreateSuccessStep("E1");
        var step2 = Substitute.For<IPipelineStep>();
        step2.StepName.Returns("T1");
        step2.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
             .Returns(PipelineResult.CriticalError("T1", "Catastrophic failure"));
        var step3 = CreateSuccessStep("L1");

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step1, step2, step3 }, context);

        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        await step3.DidNotReceive().ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_ContinueAfterWarning_When_WarningReturned()
    {
        var step1 = Substitute.For<IPipelineStep>();
        step1.StepName.Returns("E1");
        step1.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
             .Returns(PipelineResult.Warning("E1", 8, 2, TimeSpan.FromMilliseconds(10), "2 items skipped"));
        var step2 = CreateSuccessStep("T1");

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step1, step2 }, context);

        result.Status.Should().Be(PipelineResultStatus.Success, because: "warning does not abort");
        await step2.Received(1).ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_WrapException_When_StepThrowsUnhandled()
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns("BadStep");
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Boom!"));

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step }, context);

        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task RunAsync_Should_PropagateCancel_When_CancellationRequested()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var step = CreateSuccessStep("E1");
        var context = CreateContext();
        var act = () => _sut.RunAsync(new[] { step }, context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_Should_AccumulateResults_When_MultipleSteps()
    {
        var steps = new[]
        {
            CreateSuccessStep("E1"),
            CreateSuccessStep("T1"),
            CreateSuccessStep("L1")
        };
        var context = CreateContext();
        await _sut.RunAsync(steps, context);

        context.StepResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task RunAsync_Should_AggregateMetrics_When_AllSucceed()
    {
        var steps = new[]
        {
            CreateSuccessStep("E1", items: 10),
            CreateSuccessStep("T1", items: 10),
            CreateSuccessStep("L1", items: 10)
        };
        var context = CreateContext();
        var result = await _sut.RunAsync(steps, context);

        result.ItemsProcessed.Should().Be(30, because: "10 items × 3 steps");
    }

    [Fact]
    public async Task RunAsync_Should_IncludeLinkDiagnosticsInFinalSummary()
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns("Transform");
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ctx = callInfo.Arg<TranslationBatchContext>();
                ctx.UnresolvedLinkFallbackCount = 2;
                ctx.WebUiPageIdFallbackCount = 1;
                return PipelineResult.Success("Transform", 3, TimeSpan.FromMilliseconds(10));
            });

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step }, context);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.Message.Should().Contain("2 unresolved link fallback");
        result.Message.Should().Contain("1 WebUI page-id fallback");
    }

    // ─── New tests: Abort step behavior ─────────────────────────────────────

    [Fact]
    public async Task RunAsync_Should_StopAtAbort_When_StepReturnsAbort()
    {
        var step1 = Substitute.For<IPipelineStep>();
        step1.StepName.Returns("Aborter");
        step1.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
             .Returns(PipelineResult.Abort("Aborter", "Missing configuration"));
        var step2 = CreateSuccessStep("Next");

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step1, step2 }, context);

        result.Status.Should().Be(PipelineResultStatus.Abort);
        result.Message.Should().Contain("Missing configuration");
        await step2.DidNotReceive().ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>());
    }

    // ─── New tests: Exception details captured ──────────────────────────────

    [Fact]
    public async Task RunAsync_Should_CaptureExceptionMessage_When_StepThrows()
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns("Failing");
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Bad argument"));

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step }, context);

        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Message.Should().Contain("Bad argument");
        result.Exception!.Message.Should().Be("Bad argument");
    }

    // ─── New tests: StepResults captured even on failure ─────────────────────

    [Fact]
    public async Task RunAsync_Should_AddResultToContext_EvenOnCriticalError()
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns("FailStep");
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
             .Returns(PipelineResult.CriticalError("FailStep", "Something broke"));

        var context = CreateContext();
        await _sut.RunAsync(new[] { step }, context);

        context.StepResults.Should().HaveCount(1);
        context.StepResults[0].Status.Should().Be(PipelineResultStatus.CriticalError);
    }

    // ─── New tests: Failed items tracking ───────────────────────────────────

    [Fact]
    public async Task RunAsync_Should_TrackFailedItems_When_StepsReportFailures()
    {
        var step1 = Substitute.For<IPipelineStep>();
        step1.StepName.Returns("E1");
        step1.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
             .Returns(PipelineResult.Warning("E1", 10, 3, TimeSpan.FromMilliseconds(5), "3 items had issues"));
        var step2 = CreateSuccessStep("T1", items: 7);

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step1, step2 }, context);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(17); // 10 + 7
    }

    // ─── New tests: Single step pipeline ────────────────────────────────────

    [Fact]
    public async Task RunAsync_Should_Succeed_When_SingleSuccessStep()
    {
        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { CreateSuccessStep("OnlyStep", 42) }, context);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(42);
        context.StepResults.Should().HaveCount(1);
    }

    // ─── New tests: Unresolved link samples ─────────────────────────────────

    [Fact]
    public async Task RunAsync_Should_ReportUnresolvedLinkSamples_When_Present()
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns("Transform");
        step.ExecuteAsync(Arg.Any<TranslationBatchContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ctx = callInfo.Arg<TranslationBatchContext>();
                ctx.UnresolvedLinkFallbackCount = 1;
                ctx.UnresolvedLinkSamples.Add("missing-page.md");
                return PipelineResult.Success("Transform", 5, TimeSpan.FromMilliseconds(10));
            });

        var context = CreateContext();
        var result = await _sut.RunAsync(new[] { step }, context);

        result.Status.Should().Be(PipelineResultStatus.Success);
        context.UnresolvedLinkSamples.Should().Contain("missing-page.md");
    }
}
