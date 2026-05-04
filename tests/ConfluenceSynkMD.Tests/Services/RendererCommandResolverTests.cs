using ConfluenceSynkMD.Services;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Services;

/// <summary>
/// Regression test R2 (renderer cmd-args parsing backward compat): verifies
/// the shared <see cref="RendererCommandResolver"/> handles every shape the
/// renderer env vars (DRAWIO_CMD, PLANTUML_CMD, LATEX_PDFLATEX_CMD,
/// LATEX_GHOSTSCRIPT_CMD) are now expected to support — single-binary names
/// (the old contract) AND multi-token invocations (the v0.1.0 contract).
/// </summary>
public class RendererCommandResolverTests
{
    [Fact]
    public void Single_binary_name_returns_filename_with_empty_args_prefix()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("drawio");

        fileName.Should().Be("drawio");
        argsPrefix.Should().BeEmpty();
    }

    [Fact]
    public void Multi_token_string_splits_on_whitespace()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("drawio --no-sandbox --disable-gpu");

        fileName.Should().Be("drawio");
        argsPrefix.Should().Be("--no-sandbox --disable-gpu");
    }

    [Fact]
    public void Java_jar_invocation_parses_into_command_plus_args()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("java -jar /opt/plantuml.jar");

        fileName.Should().Be("java");
        argsPrefix.Should().Be("-jar /opt/plantuml.jar");
    }

    [Fact]
    public void Quoted_arg_with_space_is_kept_as_a_single_token()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("wrapper \"arg with space\" --flag");

        fileName.Should().Be("wrapper");
        argsPrefix.Should().Be("arg with space --flag");
    }

    [Fact]
    public void Quoted_path_with_spaces_is_preserved()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("\"/Applications/draw.io.app/Contents/MacOS/draw.io\" --no-sandbox");

        fileName.Should().Be("/Applications/draw.io.app/Contents/MacOS/draw.io");
        argsPrefix.Should().Be("--no-sandbox");
    }

    [Fact]
    public void Multiple_spaces_between_tokens_collapse()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("drawio    --no-sandbox     --disable-gpu");

        fileName.Should().Be("drawio");
        argsPrefix.Should().Be("--no-sandbox --disable-gpu");
    }

    [Fact]
    public void Tab_and_other_whitespace_split_tokens_too()
    {
        var (fileName, argsPrefix) = RendererCommandResolver.Parse("java\t-jar\t/opt/plantuml.jar");

        fileName.Should().Be("java");
        argsPrefix.Should().Be("-jar /opt/plantuml.jar");
    }

    [Fact]
    public void Empty_input_throws()
    {
        Action act = () => RendererCommandResolver.Parse(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Whitespace_only_input_throws()
    {
        Action act = () => RendererCommandResolver.Parse("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CombineArgs_returns_call_args_when_prefix_is_empty()
    {
        RendererCommandResolver.CombineArgs(string.Empty, "--export --format png").Should().Be("--export --format png");
    }

    [Fact]
    public void CombineArgs_returns_prefix_when_call_args_is_empty()
    {
        RendererCommandResolver.CombineArgs("--no-sandbox --disable-gpu", string.Empty).Should().Be("--no-sandbox --disable-gpu");
    }

    [Fact]
    public void CombineArgs_joins_prefix_and_call_args_with_single_space()
    {
        RendererCommandResolver.CombineArgs("--no-sandbox", "--export").Should().Be("--no-sandbox --export");
    }

    [Fact]
    public void CombineArgs_handles_both_empty()
    {
        RendererCommandResolver.CombineArgs(string.Empty, string.Empty).Should().BeEmpty();
    }
}
