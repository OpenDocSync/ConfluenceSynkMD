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
    }

    [Fact]
    public void Migration_message_inlines_the_migration_table_instead_of_pointing_at_CHANGELOG()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--mode", "Upload" });

        result.Should().NotBeNull();
        // The earlier draft pointed at a CHANGELOG that didn't exist. The fix
        // is to inline the migration table directly in the error message.
        result!.Message.Should().NotContain("CHANGELOG");
        result.Message.Should().Contain("Migration table:");
        result.Message.Should().Contain("--mode Upload          ->  upload");
        result.Message.Should().Contain("--mode Download        ->  download");
        result.Message.Should().Contain("--mode LocalExport     ->  local");
        result.Message.Should().Contain("--mode Upload --local  ->  local");
    }

    [Fact]
    public void Mode_flag_match_is_case_insensitive()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--MODE", "Upload" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("upload");
    }

    [Theory]
    [InlineData("--mode=Upload", "upload")]
    [InlineData("--mode=Download", "download")]
    [InlineData("--mode=LocalExport", "local")]
    [InlineData("--MODE=Upload", "upload")]
    [InlineData("--Mode=download", "download")]
    public void Detects_equals_form_of_mode_flag(string equalsArg, string expectedSubcommand)
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { equalsArg, "--path", "./docs", "--conf-space", "DEV" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be(expectedSubcommand);
    }

    [Theory]
    [InlineData("--mode:Upload", "upload")]
    [InlineData("--mode:Download", "download")]
    [InlineData("--mode:LocalExport", "local")]
    public void Detects_colon_form_of_mode_flag(string colonArg, string expectedSubcommand)
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { colonArg, "--path", "./docs", "--conf-space", "DEV" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be(expectedSubcommand);
    }

    [Fact]
    public void Detects_legacy_local_flag_and_suggests_local_subcommand()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "upload", "--path", "./docs", "--local", "--conf-space", "DEV" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("local");
        result.Message.Should().Contain("`--local` was removed in v0.1.0");
        result.Message.Should().Contain("confluencesynkmd local <other args...>");
    }

    [Fact]
    public void Local_flag_match_is_case_insensitive()
    {
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--LOCAL" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("local");
    }

    [Fact]
    public void Equals_form_with_empty_value_falls_back_to_placeholder()
    {
        // `--mode=` with nothing after the equals.
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "--mode=" });

        result.Should().NotBeNull();
        result!.SuggestedSubcommand.Should().Be("<upload|download|local>");
    }

    [Fact]
    public void New_subcommand_args_are_not_flagged_as_legacy()
    {
        // The new `local` subcommand should NOT trigger a migration hint
        // even though the word "local" appears in args.
        var result = CliMigrationCheck.CheckLegacyModeFlag(
            new[] { "local", "--path", "./docs", "--conf-space", "DEV" });

        result.Should().BeNull();
    }
}
