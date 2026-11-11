using MailKit.Security;
using MimeKit;
//Ignore Spelling: Emailer SMTP eml
namespace Carrigan.Emailer.Tests;

// These tests exercise only in-memory configuration and local pickup files.
// They never connect to an SMTP server or send a message over the network.
public class EmailerConfigurationTests
{
    private sealed class Configuration : IEmailConfiguration
    {
        public EmailAddress? DefaultAccount { get; set; } = new("sender@example.com");
        public string? Password { get; set; } = "test-only-password";
        public IEnumerable<EmailDomainSmtpEndpoint> EmailDomainSmtpEndpoints { get; set; } =
            [new EmailDomainSmtpEndpoint { EmailDomain = "example.com", SmtpServer = "smtp.example.com", SmtpPort = 587 }];
        public SecurityEnum? SecurityOption { get; set; } = SecurityEnum.Tls;
        public string? PickupDirectory { get; set; }
        public bool UsePickupDirectory { get; set; }
        public bool? UseNetworkDelivery { get; set; } = true;
        public string? GetPasswordForAccount(string accountAddress) => Password;
    }

    private sealed class ExposedEmailer : Emailer
    {
        internal static SecureSocketOptions Security() => GetSocketSecurityOption();
    }

    [Fact]
    public void ThrowIfNull_NullConfiguration_ThrowsEmailerConfigurationException()
    {
        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() =>
            EmailerConfigurationException.ThrowIfNull(null));

        Assert.Contains("null", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateConfiguration_ValidNetworkConfiguration_IsAcceptedWithoutNetworkCall()
    {
        Configuration configuration = new();

        Emailer.ValidateConfiguration(configuration);

        Assert.Equal(SecureSocketOptions.StartTls, ExposedEmailer.Security());
    }

    [Theory]
    [InlineData(SecurityEnum.None, SecureSocketOptions.None)]
    [InlineData(SecurityEnum.Auto, SecureSocketOptions.Auto)]
    [InlineData(SecurityEnum.Ssl, SecureSocketOptions.SslOnConnect)]
    [InlineData(SecurityEnum.Tls, SecureSocketOptions.StartTls)]
    public void ValidateConfiguration_SecurityOption_IsMapped(SecurityEnum setting, SecureSocketOptions expected)
    {
        Configuration configuration = new() { SecurityOption = setting };

        Emailer.ValidateConfiguration(configuration);

        Assert.Equal(expected, ExposedEmailer.Security());
    }

    [Fact]
    public void ValidateConfiguration_NullConfiguration_ThrowsEmailerConfigurationException() =>
        Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(null!));

    [Fact]
    public void ValidateConfiguration_NullDeliveryFlag_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { UseNetworkDelivery = null };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("UseNetworkDelivery", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_NoDeliveryMethod_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { UseNetworkDelivery = false, UsePickupDirectory = false };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("No delivery method", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_NoDefaultAccount_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { DefaultAccount = null };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("DefaultAccount", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_MissingPassword_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { Password = null };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateConfiguration_MissingEndpoint_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { EmailDomainSmtpEndpoints = [] };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("SMTP endpoint", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_NullSecurityOption_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { SecurityOption = null };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        Assert.Contains("SecurityOption", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_InvalidSecurityOption_ThrowsEmailerConfigurationException()
    {
        Configuration configuration = new() { SecurityOption = (SecurityEnum)int.MaxValue };

        Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));
    }

    [Fact]
    public void ValidateConfiguration_MultipleErrors_WrapsAggregateException()
    {
        Configuration configuration = new() { SecurityOption = null, DefaultAccount = null };

        EmailerConfigurationException exception = Assert.Throws<EmailerConfigurationException>(() => Emailer.ValidateConfiguration(configuration));

        AggregateException inner = Assert.IsType<AggregateException>(exception.InnerException);
        Assert.True(inner.InnerExceptions.Count >= 2);
    }

    [Fact]
    public void ValidateConfiguration_PickupOnly_NetworkEndpointEnumerationIsNotRequired()
    {
        string directory = NewPickupDirectory();
        try
        {
            Configuration configuration = new()
            {
                UseNetworkDelivery = false,
                UsePickupDirectory = true,
                PickupDirectory = directory
            };

            Emailer.ValidateConfiguration(configuration);

            Assert.True(Directory.Exists(directory));
        }
        finally
        {
            RemovePickupDirectory(directory);
        }
    }

    [Fact]
    public async Task WriteEmailsToPickupDirectoryAsync_TextMessage_WritesReadableEmlAndRecordsSuccess()
    {
        string directory = NewPickupDirectory();
        try
        {
            Emailer.ValidateConfiguration(new Configuration
            {
                UseNetworkDelivery = false,
                UsePickupDirectory = true,
                PickupDirectory = directory
            });
            Guid id = Guid.NewGuid();
            EmailMessage source = new(id, "recipient@example.com", "Offline test", null, "Hello offline");

            EmailerResults results = await Emailer.WriteEmailsToPickupDirectoryAsync(source);

            Assert.Equal(id, Assert.Single(results.SuccessfulEmailIds));
            string[] files = Directory.GetFiles(directory, "*.eml", SearchOption.AllDirectories);
            Assert.Equal(2, files.Length);
            foreach (string file in files)
            {
                using MimeMessage parsed = MimeMessage.Load(file);
                Assert.Equal("Offline test", parsed.Subject);
                Assert.Equal("Hello offline", parsed.TextBody.Trim());
                Assert.Equal("sender@example.com", Assert.Single(parsed.From.Mailboxes).Address);
            }
        }
        finally
        {
            RemovePickupDirectory(directory);
        }
    }

    [Fact]
    public async Task SendEmailsAsync_PickupOnly_DoesNotNeedSmtpServer()
    {
        string directory = NewPickupDirectory();
        try
        {
            Emailer.ValidateConfiguration(new Configuration
            {
                UseNetworkDelivery = false,
                UsePickupDirectory = true,
                PickupDirectory = directory,
                EmailDomainSmtpEndpoints = []
            });
            Guid id = Guid.NewGuid();
            EmailMessage source = new(id, "recipient@example.com", "Offline test", null, "Hello");
            Emailer emailer = new();

            EmailerResults results = await emailer.SendEmailsAsync(source);

            Assert.Equal(id, Assert.Single(results.SuccessfulEmailIds));
            Assert.Empty(results.Errors);
        }
        finally
        {
            RemovePickupDirectory(directory);
        }
    }

    private static string NewPickupDirectory() =>
        Path.Combine(Path.GetTempPath(), "Carrigan.Emailer.Tests", Guid.NewGuid().ToString("N"));

    private static void RemovePickupDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
