using ConfluenceSynkMD.Configuration;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Configuration;

/// <summary>
/// Regression test R1 (subcommand parity migration): verifies the legacy
/// `--mode` flag is rewritten into a clear migration hint that names the
/// matching subcommand. Without this, users on the old syntax would get
/// System.CommandLine's bare "Unrecognized option" error and lose time
/// figuring out the v0.1.0 CLI shape.
/// </summary>
public class CliMigrationCheckTests
{
    [Fact]
    public void Returns_null_when_args_do_not_contain_mode_flag()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "upload", "--path", "./docs", "--conf-space", "DEV" });

        result.Should().BeNull();
    }

    [Fact]
    public void Returns_null_for_empty_args()
    {
        CliMigrationCheck.CheckLegacyModeFlag(Array.Empty<string>()).Should().BeNull();
    }

    [Fact]
    public void Returns_null_for_null_args()
    {
        CliMigrationCheck.CheckLegacyModeFlag(null!).Should().BeNull();
    }

    [Theory]
    [InlineData("Upload", "upload")]
    [InlineData("upload", "upload")]
    [InlineData("UPLOAD", "upload")]
    [InlineData("Download", "download")]
    [InlineData("download", "download")]
    [InlineData("LocalExport", "local")]
    [InlineData("localexport", "local")]
    public void Maps_legacy_mode_value_to_correct_subcommand(string modeValue, string expectedSubcommand)
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--mode", modeValue, "--path", "./docs" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be(expectedSubcommand);
        result.Message.Should().Contain($"`{expectedSubcommand}` subcommand");
        result.Message.Should().Contain("v0.1.0");
    }

    [Fact]
    public void Falls_back_to_placeholder_when_mode_value_is_unrecognized()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--mode", "Sync", "--path", "./docs" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("<upload|download|local>");
    }

    [Fact]
    public void Detects_mode_flag_at_any_position_in_args()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--path", "./docs", "--conf-space", "DEV", "--mode", "Upload" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("upload");
    }

    [Fact]
    public void Handles_mode_flag_at_end_of_args_without_value()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--path", "./docs", "--mode" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("<upload|download|local>");
    }

    [Fact]
    public void Migration_message_includes_the_full_invocation_template()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--mode", "Upload" });

        result.Should().NotBeNull();
        result!.Message.Should().Contain("confluencesynkmd upload <other args...>");
        result.Message.Should().Contain("CHANGELOG");
    }

    [Fact]
    public void Mode_flag_match_is_case_insensitive()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--MODE", "Upload" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("upload");
    }
}
