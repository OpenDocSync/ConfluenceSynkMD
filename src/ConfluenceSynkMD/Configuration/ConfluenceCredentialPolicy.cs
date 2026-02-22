using ConfluenceSynkMD.Models;

namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// Defines whether a synchronization mode requires Confluence API credentials.
/// </summary>
public static class ConfluenceCredentialPolicy
{
    /// <summary>
    /// Returns true when the selected mode performs Confluence API calls and therefore
    /// requires valid Confluence credentials.
    /// </summary>
    public static bool RequiresCredentials(SyncMode mode) => mode != SyncMode.LocalExport;
}
