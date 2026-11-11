using MimeKit;
//IGNORE SPELLING: EMailer
namespace Carrigan.Emailer.Tests;

public class EmailerMimeMessageTests
{
    private sealed class ExposedEmailer : Emailer
    {
        internal static MimeMessage Convert(EmailMessage message) => GetMimeMessageFromEmail(message);
    }

    private static EmailMessage CreateMessage(string? html, string? text) =>
        new(Guid.NewGuid(), new EmailAddress("from@example.com", "Sender"),
            [new EmailAddress("to@example.com", "Recipient")], null, null, null,
            "Subject", html, text, null);

    [Fact]
    public void Convert_TextOnly_ProducesPlainTextBody()
    {
        EmailMessage source = CreateMessage(null, "Hello world");
        using MimeMessage message = ExposedEmailer.Convert(source);

        Assert.Equal("Subject", message.Subject);
        Assert.Equal("Hello world", message.TextBody);
        Assert.Null(message.HtmlBody);
        Assert.Equal("from@example.com", Assert.Single(message.From.Mailboxes).Address);
        Assert.Equal("to@example.com", Assert.Single(message.To.Mailboxes).Address);
    }

    [Fact]
    public void Convert_HtmlOnly_ProducesHtmlBody()
    {
        using MimeMessage message = ExposedEmailer.Convert(CreateMessage("<p>Hello</p>", null));

        Assert.Equal("<p>Hello</p>", message.HtmlBody);
        Assert.Null(message.TextBody);
    }

    [Fact]
    public void Convert_HtmlAndText_ProducesAlternativeBody()
    {
        using MimeMessage message = ExposedEmailer.Convert(CreateMessage("<p>Hello</p>", "Hello"));

        Assert.IsType<Multipart>(message.Body, exactMatch: false);
        Assert.Equal("Hello", message.TextBody);
        Assert.Equal("<p>Hello</p>", message.HtmlBody);
    }

    [Fact]
    public void Convert_OptionalRecipients_AreMapped()
    {
        EmailMessage source = CreateMessage(null, "Hello");
        source.Cc = [new EmailAddress("cc@example.com")];
        source.Bcc = [new EmailAddress("bcc@example.com")];
        source.ReplyTo = [new EmailAddress("reply@example.com")];

        using MimeMessage message = ExposedEmailer.Convert(source);

        Assert.Equal("cc@example.com", Assert.Single(message.Cc.Mailboxes).Address);
        Assert.Equal("bcc@example.com", Assert.Single(message.Bcc.Mailboxes).Address);
        Assert.Equal("reply@example.com", Assert.Single(message.ReplyTo.Mailboxes).Address);
    }

    [Fact]
    public void Convert_UnsubscribeOptions_AddsListUnsubscribeHeader()
    {
        EmailMessage source = CreateMessage(null, "Hello");
        source.UnsubscribeOptions = ["https://example.com/unsubscribe", "mailto:unsubscribe@example.com"];

        using MimeMessage message = ExposedEmailer.Convert(source);

        Assert.Equal("<https://example.com/unsubscribe>, <mailto:unsubscribe@example.com>", message.Headers["List-Unsubscribe"]);
    }

    [Fact]
    public void Convert_NoRemainingBodyAfterMutation_ThrowsFormatException()
    {
        EmailMessage source = CreateMessage(null, "Hello");
        source.HtmlBody = null;
        source.TextBody = null;

        Assert.Throws<FormatException>(() => ExposedEmailer.Convert(source));
    }
}
