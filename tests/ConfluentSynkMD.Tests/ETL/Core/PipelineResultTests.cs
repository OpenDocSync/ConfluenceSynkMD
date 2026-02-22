using FluentAssertions;
using ConfluentSynkMD.ETL.Core;

namespace ConfluentSynkMD.Tests.ETL.Core;

public class PipelineResultTests
{
    private const string TestStepName = "TestStep";

    [Fact]
    public void Success_Should_SetStatusAndMetrics_When_Created()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);
        const int itemsProcessed = 42;

        // Act
        var result = PipelineResult.Success(TestStepName, itemsProcessed, duration);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        result.StepName.Should().Be(TestStepName);
        result.ItemsProcessed.Should().Be(itemsProcessed);
        result.Duration.Should().Be(duration);
    }

    [Fact]
    public void Warning_Should_TrackFailedItems_When_Created()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(200);
        const string message = "3 items skipped";

        // Act
        var result = PipelineResult.Warning(TestStepName, 10, 3, duration, message);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Warning);
        result.ItemsFailed.Should().Be(3);
        result.Message.Should().Be(message);
    }

    [Fact]
    public void CriticalError_Should_IncludeException_When_Provided()
    {
        // Arrange
        var exception = new InvalidOperationException("Something broke");

        // Act
        var result = PipelineResult.CriticalError(TestStepName, "Failure", exception);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Abort_Should_SetAbortStatus_When_Created()
    {
        // Arrange & Act
        var result = PipelineResult.Abort(TestStepName, "Intentionally aborted");

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Abort);
        result.Message.Should().Contain("Intentionally aborted");
    }

    [Fact]
    public void CanContinue_Should_ReturnTrue_When_StatusIsSuccess()
    {
        // Arrange
        var result = PipelineResult.Success(TestStepName, 1, TimeSpan.Zero);

        // Act & Assert
        result.CanContinue.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_Should_ReturnTrue_When_StatusIsWarning()
    {
        // Arrange
        var result = PipelineResult.Warning(TestStepName, 1, 0, TimeSpan.Zero, "warn");

        // Act & Assert
        result.CanContinue.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_Should_ReturnFalse_When_StatusIsCriticalError()
    {
        // Arrange
        var result = PipelineResult.CriticalError(TestStepName, "boom");

        // Act & Assert
        result.CanContinue.Should().BeFalse();
    }

    [Fact]
    public void ToString_Should_IncludeAllFields_When_Called()
    {
        // Arrange
        var result = PipelineResult.Success(TestStepName, 5, TimeSpan.FromMilliseconds(100));

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
        str.Should().Contain(TestStepName);
        str.Should().Contain("5");
    }
}
