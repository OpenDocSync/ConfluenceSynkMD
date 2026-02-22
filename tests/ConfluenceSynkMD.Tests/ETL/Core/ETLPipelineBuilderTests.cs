using FluentAssertions;
using ConfluenceSynkMD.ETL.Core;
using NSubstitute;

namespace ConfluenceSynkMD.Tests.ETL.Core;

public class ETLPipelineBuilderTests
{
    private readonly ETLPipelineBuilder _sut = new();

    private static IPipelineStep CreateStep(string name)
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepName.Returns(name);
        return step;
    }

    [Fact]
    public void Build_Should_ReturnEmpty_When_NoStepsAdded()
    {
        // Act
        var steps = _sut.Build();

        // Assert
        steps.Should().BeEmpty();
    }

    [Fact]
    public void Build_Should_OrderExtractorsFirst_When_AllPhasesAdded()
    {
        // Arrange
        var loader = CreateStep("Loader");
        var extractor = CreateStep("Extractor");
        var transformer = CreateStep("Transformer");

        _sut.AddLoader(loader)
            .AddExtractor(extractor)
            .AddTransformer(transformer);

        // Act
        var steps = _sut.Build();

        // Assert
        steps.Should().HaveCount(3);
        steps[0].StepName.Should().Be("Extractor");
        steps[1].StepName.Should().Be("Transformer");
        steps[2].StepName.Should().Be("Loader");
    }

    [Fact]
    public void Build_Should_ReturnSingleStep_When_OnlyExtractor()
    {
        // Arrange
        var extractor = CreateStep("OnlyExtractor");
        _sut.AddExtractor(extractor);

        // Act
        var steps = _sut.Build();

        // Assert
        steps.Should().ContainSingle()
             .Which.StepName.Should().Be("OnlyExtractor");
    }

    [Fact]
    public void AddExtractor_Should_ThrowArgumentNull_When_StepIsNull()
    {
        // Act
        var act = () => _sut.AddExtractor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTransformer_Should_ReturnSameBuilder_When_ChainCalled()
    {
        // Arrange
        var step = CreateStep("T1");

        // Act
        var returned = _sut.AddTransformer(step);

        // Assert
        returned.Should().BeSameAs(_sut);
    }

    [Fact]
    public void Build_Should_PreserveInsertionOrder_When_MultipleSamePhase()
    {
        // Arrange
        var e1 = CreateStep("E1");
        var e2 = CreateStep("E2");
        var e3 = CreateStep("E3");
        _sut.AddExtractor(e1).AddExtractor(e2).AddExtractor(e3);

        // Act
        var steps = _sut.Build();

        // Assert
        steps.Select(s => s.StepName).Should().ContainInOrder("E1", "E2", "E3");
    }
}
