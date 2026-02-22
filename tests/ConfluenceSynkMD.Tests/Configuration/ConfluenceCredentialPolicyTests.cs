using FluentAssertions;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Models;

namespace ConfluenceSynkMD.Tests.Configuration;

public class ConfluenceCredentialPolicyTests
{
    [Fact]
    public void Upload_RequiresCredentials()
    {
        ConfluenceCredentialPolicy.RequiresCredentials(SyncMode.Upload).Should().BeTrue();
    }

    [Fact]
    public void Download_RequiresCredentials()
    {
        ConfluenceCredentialPolicy.RequiresCredentials(SyncMode.Download).Should().BeTrue();
    }

    [Fact]
    public void LocalExport_DoesNotRequireCredentials()
    {
        ConfluenceCredentialPolicy.RequiresCredentials(SyncMode.LocalExport).Should().BeFalse();
    }
}
