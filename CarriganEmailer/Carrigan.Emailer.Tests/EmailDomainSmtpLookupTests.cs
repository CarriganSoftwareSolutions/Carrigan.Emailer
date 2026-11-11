namespace Carrigan.Emailer.Tests;

public class EmailDomainSmtpLookupTests
{
    private static EmailDomainSmtpEndpoint Endpoint(string domain = "example.com", string server = "smtp.example.com", int port = 587) =>
        new() { EmailDomain = domain, SmtpServer = server, SmtpPort = port };

    [Fact]
    public void Constructor_NullEndpoints_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new EmailDomainSmtpLookup(null!));

        Assert.Equal("endpoints", exception.ParamName);
    }

    [Fact]
    public void Constructor_EmptyEndpoints_CreatesEmptyLookup()
    {
        EmailDomainSmtpLookup lookup = new([]);

        Assert.Null(lookup.Lookup("example.com"));
        Assert.False(lookup.TryLookup("example.com", out EmailDomainSmtpEndpoint? endpoint));
        Assert.Null(endpoint);
        Assert.True(lookup.DoesNotContainDomain("example.com"));
    }

    [Fact]
    public void Lookup_KnownDomain_IsCaseInsensitive()
    {
        EmailDomainSmtpEndpoint expected = Endpoint();
        EmailDomainSmtpLookup lookup = new([expected]);

        Assert.Same(expected, lookup.Lookup("EXAMPLE.COM"));
        Assert.True(lookup.TryLookup("Example.Com", out EmailDomainSmtpEndpoint? actual));
        Assert.Same(expected, actual);
        Assert.False(lookup.DoesNotContainDomain("example.com"));
    }

    [Fact]
    public void Constructor_DuplicateDomainIgnoringCase_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new EmailDomainSmtpLookup([Endpoint(), Endpoint("EXAMPLE.COM")]));

        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_BlankDomain_ThrowsArgumentException(string domain) => 
        Assert.Throws<ArgumentException>(() => new EmailDomainSmtpLookup([Endpoint(domain)]));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_BlankServer_ThrowsArgumentException(string server) => 
        Assert.Throws<ArgumentException>(() => new EmailDomainSmtpLookup([Endpoint(server: server)]));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    public void Constructor_InvalidPort_ThrowsArgumentOutOfRangeException(int port) => 
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDomainSmtpLookup([Endpoint(port: port)]));

    [Theory]
    [InlineData(1)]
    [InlineData(65535)]
    public void Constructor_ValidPort_AcceptsEndpoint(int port)
    {
        EmailDomainSmtpLookup lookup = new([Endpoint(port: port)]);

        Assert.Equal(port, lookup.Lookup("example.com")?.SmtpPort);
    }

    [Fact]
    public void Constructor_MultipleErrors_ThrowsAggregateException()
    {
        AggregateException exception = Assert.Throws<AggregateException>(() =>
            new EmailDomainSmtpLookup([Endpoint(domain: ""), Endpoint(server: ""), Endpoint(port: 0)]));

        Assert.Equal(4, exception.InnerExceptions.Count);
    }

    [Fact]
    public void Constructor_NullEndpoint_ThrowsArgumentNullException() => 
        Assert.Throws<ArgumentNullException>(() => new EmailDomainSmtpLookup([null!]));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Lookup_InvalidDomain_ThrowsArgumentException(string? domain)
    {
        EmailDomainSmtpLookup lookup = new([]);

        Assert.ThrowsAny<ArgumentException>(() => lookup.Lookup(domain!));
        Assert.ThrowsAny<ArgumentException>(() => lookup.TryLookup(domain!, out _));
        Assert.ThrowsAny<ArgumentException>(() => lookup.DoesNotContainDomain(domain!));
    }

    [Fact]
    public void ContainsDuplicateDomains_CaseInsensitiveDuplicate_ReturnsTrue() => 
        Assert.True(EmailDomainSmtpLookup.ContainsDuplicateDomains([Endpoint(), Endpoint("EXAMPLE.COM")]));

    [Fact]
    public void ContainsDuplicateDomains_UniqueDomains_ReturnsFalse() =>
        Assert.False(EmailDomainSmtpLookup.ContainsDuplicateDomains([Endpoint(), Endpoint("other.com")]));
}
