namespace Carrigan.Emailer.Tests;
//IGNORE SPELLING: BCC
public class EmailMessageTests
{
    [Fact]
    public void Constructor_StringRecipient_CreatesRecipient()
    {
        Guid id = Guid.NewGuid();
        EmailMessage message = new(id, "recipient@example.com", "Test subject", "<b>HTML</b>", null);

        Assert.Equal(id, message.Id);
        Assert.Equal("recipient@example.com", Assert.Single(message.To).Address);
        Assert.Equal("Test subject", message.Subject);
        Assert.Equal("<b>HTML</b>", message.HtmlBody);
        Assert.Equal(string.Empty, message.TextBody);
        Assert.Empty(message.Cc);
        Assert.Empty(message.Bcc);
        Assert.Empty(message.ReplyTo);
        Assert.Empty(message.UnsubscribeOptions);
        Assert.Null(message.From);
    }

    [Fact]
    public void Constructor_TextOnlyBody_IsAccepted()
    {
        EmailMessage message = new(Guid.NewGuid(), new EmailAddress("recipient@example.com"), "Test subject", null, "Hello");

        Assert.Equal(string.Empty, message.HtmlBody);
        Assert.Equal("Hello", message.TextBody);
    }

    [Fact]
    public void Constructor_FullMessage_PreservesFields()
    {
        EmailAddress sender = new("sender@example.com");
        EmailAddress to = new("to@example.com");
        EmailAddress cc = new("cc@example.com");
        EmailAddress bcc = new("bcc@example.com");
        EmailAddress replyTo = new("reply@example.com");
        EmailMessage message = new(Guid.NewGuid(), sender, [to], [cc], [bcc], [replyTo], "Subject", "<p>HTML</p>", "Plain", ["https://example.com/unsubscribe"]);

        Assert.Same(sender, message.From);
        Assert.Same(to, Assert.Single(message.To));
        Assert.Same(cc, Assert.Single(message.Cc));
        Assert.Same(bcc, Assert.Single(message.Bcc));
        Assert.Same(replyTo, Assert.Single(message.ReplyTo));
        Assert.Equal("https://example.com/unsubscribe", Assert.Single(message.UnsubscribeOptions));
    }

    [Fact]
    public void Constructor_RecipientEnumerable_IsEnumeratedOnce()
    {
        int enumerations = 0;
        IEnumerable<EmailAddress> Recipients()
        {
            enumerations++;
            if (enumerations > 1)
                throw new InvalidOperationException("Repeated enumeration");
            yield return new EmailAddress("recipient@example.com");
        }

        EmailMessage message = new(Guid.NewGuid(), Recipients(), "Subject", null, "Text");

        Assert.Single(message.To);
        Assert.Single(message.To);
        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void Constructor_RecipientCollection_IsSnapshot()
    {
        List<EmailAddress> recipients = [new("first@example.com")];
        EmailMessage message = new(Guid.NewGuid(), recipients, "Subject", null, "Text");
        recipients.Add(new EmailAddress("second@example.com"));

        Assert.Single(message.To);
    }

    [Fact]
    public void Constructor_NullRecipients_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new EmailMessage(Guid.NewGuid(), (IEnumerable<EmailAddress>)null!, "Subject", null, "Text"));

        Assert.Equal("to", exception.ParamName);
    }

    [Fact]
    public void Constructor_EmptyRecipients_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new EmailMessage(Guid.NewGuid(), [], "Subject", null, "Text"));

        Assert.Equal("to", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullSubject_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new EmailMessage(Guid.NewGuid(), "recipient@example.com", null!, null, "Text"));

        Assert.Equal("subject", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptySubject_ThrowsArgumentException(string subject)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new EmailMessage(Guid.NewGuid(), "recipient@example.com", subject, null, "Text"));

        Assert.Equal("subject", exception.ParamName);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  ", "	")]
    public void Constructor_EmptyBodies_ThrowsArgumentException(string? html, string? text) => 
        Assert.Throws<ArgumentException>(() => new EmailMessage(Guid.NewGuid(), "recipient@example.com", "Subject", html, text));
}
