//Ignore Spelling: Bcc

using Carrigan.Core.Extensions;
using Carrigan.Emailer;

namespace Carrigan.Emailer;

public class EmailMessage
{
    public EmailMessage(Guid id, string toAddress, string subject, string? htmlBody, string? textBody) : this(id, new EmailAddress(toAddress), subject, htmlBody, textBody) { }
    public EmailMessage(Guid id, EmailAddress to, string subject, string? htmlBody, string? textBody) : this(id, null, [to], null, null, subject, htmlBody, textBody, null) { }
    public EmailMessage(Guid id, EmailAddress to, string subject, string? htmlBody, string? textBody, IEnumerable<string> unsubscribeOptions) : this(id, null, [to], null, null, subject, htmlBody, textBody, unsubscribeOptions) { }
    private EmailMessage(Guid id, EmailAddress? from, IEnumerable<EmailAddress> to, IEnumerable<EmailAddress>? cc, IEnumerable<EmailAddress>? bcc, string subject, string? htmlBody, string? textBody, IEnumerable<string>? unsubscribeOptions)
    {
        Id = id;
        From = from;
        To = to;
        Cc = cc ?? [];
        Bcc = bcc ?? [];
        if (htmlBody.IsNullOrWhiteSpace() && textBody.IsNullOrWhiteSpace())
            throw new InvalidOperationException($"You must provide at least one message body either as a {nameof(htmlBody)} or a {nameof(textBody)}.");
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        if (unsubscribeOptions is not null)
            UnsubscribeOptions = unsubscribeOptions;
        else
            UnsubscribeOptions = [];
    }

    public Guid Id { get; set; }
    public EmailAddress? From { get; set; }
    public IEnumerable<EmailAddress> To { get; set; }
    public IEnumerable<EmailAddress> Cc { get; set; }
    public IEnumerable<EmailAddress> Bcc { get; set; }

    public string Subject { get; set; }

    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }

    public IEnumerable<string> UnsubscribeOptions { get; set; }
}
