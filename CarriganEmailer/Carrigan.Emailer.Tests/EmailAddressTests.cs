namespace Carrigan.Emailer.Tests;

public class EmailAddressTests
{
    [Fact]
    public void Constructor_ValidAddress_StoresAddressAndHost()
    {
        EmailAddress address = new("user@example.com", "Example User");

        Assert.Equal("user@example.com", address.Address);
        Assert.Equal("Example User", address.DisplayName);
        Assert.Equal("example.com", address.Host);
        Assert.False(address.IsEmpty());
        Assert.True(address.IsNotEmpty());
    }

    [Fact]
    public void Constructor_NoDisplayName_UsesEmptyDisplayName()
    {
        EmailAddress address = new("user@example.com");

        Assert.Equal(string.Empty, address.DisplayName);
        Assert.True(address.IsNotEmpty());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("invalid address")]
    public void Constructor_InvalidAddress_IsEmpty(string value)
    {
        EmailAddress address = new(value);

        Assert.True(address.IsEmpty());
        Assert.False(address.IsNotEmpty());
        Assert.Equal(string.Empty, address.Host);
        Assert.Equal("_", address.GetFolderName());
    }

    [Fact]
    public void GetFolderName_ValidAddress_ReturnsLowercaseAddress()
    {
        EmailAddress address = new("USER@EXAMPLE.COM");

        Assert.Equal("user@example.com", address.GetFolderName());
    }
}
