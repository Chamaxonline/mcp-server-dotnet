using FluentAssertions;
using McpServerDotnet.Core;
using Xunit;

namespace McpServerDotnet.Core.Tests;

public sealed class AzureAuthOptionsTests
{
    [Fact]
    public void DefaultMode_IsManagedIdentity()
    {
        var opts = new AzureAuthOptions();

        opts.Mode.Should().Be(AuthMode.ManagedIdentity);
    }

    [Fact]
    public void SectionName_IsAzureAuth()
    {
        AzureAuthOptions.SectionName.Should().Be("AzureAuth");
    }

    [Fact]
    public void AdditionalScopes_DefaultsToEmpty()
    {
        var opts = new AzureAuthOptions();

        opts.AdditionalScopes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AuthMode.ManagedIdentity)]
    [InlineData(AuthMode.ApiKey)]
    [InlineData(AuthMode.AzureCli)]
    public void AllAuthModes_AreDefinedInEnum(AuthMode mode)
    {
        Enum.IsDefined(mode).Should().BeTrue();
    }

    [Fact]
    public void ApiKey_DefaultsToNull()
    {
        var opts = new AzureAuthOptions();

        opts.ApiKey.Should().BeNull();
    }
}
