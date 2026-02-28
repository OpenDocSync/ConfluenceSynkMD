using FluentAssertions;
using ConfluenceSynkMD.Services;
using Serilog;
using NSubstitute;
using System.Reflection;

namespace ConfluenceSynkMD.Tests.Services;

public class PlantUmlRendererTests
{
    private static readonly Lock EnvLock = new();

    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new PlantUmlRenderer(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void BuildCommand_Should_UsePlantUmlCmd_When_EnvironmentVariableSet()
    {
        lock (EnvLock)
        {
            var originalCmd = Environment.GetEnvironmentVariable("PLANTUML_CMD");
            var originalJar = Environment.GetEnvironmentVariable("PLANTUML_JAR");
            try
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", "plantuml-custom");
                Environment.SetEnvironmentVariable("PLANTUML_JAR", null);

                var (command, arguments) = InvokeBuildCommand("diagram.puml", "svg");

                command.Should().Be("plantuml-custom");
                arguments.Should().Contain("-tsvg");
                arguments.Should().Contain("\"diagram.puml\"");
            }
            finally
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", originalCmd);
                Environment.SetEnvironmentVariable("PLANTUML_JAR", originalJar);
            }
        }
    }

    [Fact]
    public void BuildCommand_Should_UseJavaJar_When_JarConfiguredAndExists()
    {
        var jarFile = Path.Combine(Path.GetTempPath(), $"plantuml-{Guid.NewGuid():N}.jar");
        File.WriteAllText(jarFile, string.Empty);

        try
        {
            lock (EnvLock)
            {
                var originalCmd = Environment.GetEnvironmentVariable("PLANTUML_CMD");
                var originalJar = Environment.GetEnvironmentVariable("PLANTUML_JAR");
                try
                {
                    Environment.SetEnvironmentVariable("PLANTUML_CMD", null);
                    Environment.SetEnvironmentVariable("PLANTUML_JAR", jarFile);

                    var (command, arguments) = InvokeBuildCommand("diagram.puml", "png");

                    command.Should().Be("java");
                    arguments.Should().Contain("-jar");
                    arguments.Should().Contain(jarFile);
                    arguments.Should().Contain("-tpng");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("PLANTUML_CMD", originalCmd);
                    Environment.SetEnvironmentVariable("PLANTUML_JAR", originalJar);
                }
            }
        }
        finally
        {
            if (File.Exists(jarFile))
            {
                File.Delete(jarFile);
            }
        }
    }

    [Fact]
    public void BuildCommand_Should_FallbackToPlantUmlExecutable_When_NoEnvironmentConfigured()
    {
        lock (EnvLock)
        {
            var originalCmd = Environment.GetEnvironmentVariable("PLANTUML_CMD");
            var originalJar = Environment.GetEnvironmentVariable("PLANTUML_JAR");
            try
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", null);
                Environment.SetEnvironmentVariable("PLANTUML_JAR", null);

                var (command, arguments) = InvokeBuildCommand("diagram.puml", "png");

                command.Should().Be("plantuml");
                arguments.Should().Contain("-tpng");
            }
            finally
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", originalCmd);
                Environment.SetEnvironmentVariable("PLANTUML_JAR", originalJar);
            }
        }
    }

    [Fact]
    public void RenderAsync_Should_Throw_When_ExecutableMissing()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext<PlantUmlRenderer>().Returns(logger);
        var renderer = new PlantUmlRenderer(logger);

        lock (EnvLock)
        {
            var originalCmd = Environment.GetEnvironmentVariable("PLANTUML_CMD");
            var originalJar = Environment.GetEnvironmentVariable("PLANTUML_JAR");
            try
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", "plantuml-command-that-does-not-exist");
                Environment.SetEnvironmentVariable("PLANTUML_JAR", null);

                var exception = Assert.ThrowsAnyAsync<Exception>(
                    () => renderer.RenderAsync("@startuml\nAlice -> Bob: Hi\n@enduml"))
                    .GetAwaiter()
                    .GetResult();

                exception.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PLANTUML_CMD", originalCmd);
                Environment.SetEnvironmentVariable("PLANTUML_JAR", originalJar);
            }
        }
    }

    private static (string Command, string Arguments) InvokeBuildCommand(string inputFile, string format)
    {
        var method = typeof(PlantUmlRenderer).GetMethod("BuildCommand", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var result = method!.Invoke(null, [inputFile, format]);
        result.Should().BeOfType<ValueTuple<string, string>>();

        return ((string Command, string Arguments))result!;
    }
}
