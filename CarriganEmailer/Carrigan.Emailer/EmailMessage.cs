//Ignore Spelling: Bcc emailer
using Carrigan.Core.Extensions;

namespace Carrigan.Emailer;

public class EmailMessage
{
    public EmailMessage(Guid id, string toAddress, string subject, string? htmlBody, string? textBody) : this(id, new EmailAddress(toAddress), subject, htmlBody, textBody) { }
    public EmailMessage(Guid id, EmailAddress to, string subject, string? htmlBody, string? textBody) : this(id, null, [to], null, null, null, subject, htmlBody, textBody, null) { }
    public EmailMessage(Guid id, IEnumerable<EmailAddress> to, string subject, string? htmlBody, string? textBody) : this(id, null, to, null, null, null, subject, htmlBody, textBody, null) { }
    public EmailMessage(Guid id, EmailAddress to, string subject, string? htmlBody, string? textBody, IEnumerable<string> unsubscribeOptions) : this(id, null, [to], null, null, null, subject, htmlBody, textBody, unsubscribeOptions) { }
    public EmailMessage(Guid id, IEnumerable<EmailAddress> to, string subject, string? htmlBody, string? textBody, IEnumerable<string> unsubscribeOptions) : this(id, null, to, null, null, null, subject, htmlBody, textBody, unsubscribeOptions) { }
    public EmailMessage(Guid id, EmailAddress? from, IEnumerable<EmailAddress> to, IEnumerable<EmailAddress>? cc, IEnumerable<EmailAddress>? bcc, IEnumerable<EmailAddress>? replyTo, string subject, string? htmlBody, string? textBody, IEnumerable<string>? unsubscribeOptions)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(subject);
        if (to.None())
            throw new ArgumentException("At least one To address is required.", nameof(to));
        if(subject.IsWhiteSpace())
            throw new ArgumentException("You must provide a subject.", nameof(subject));
        if (htmlBody.IsNullOrWhiteSpace() && textBody.IsNullOrWhiteSpace())
            throw new ArgumentException("Either htmlBody or textBody must be provided.");

        Id = id;
        From = from;
        To = to;
        Cc = cc ?? [];
        Bcc = bcc ?? [];
        ReplyTo = replyTo ?? [];
        Subject = subject;
        HtmlBody = htmlBody ?? string.Empty;
        TextBody = textBody ?? string.Empty;
        UnsubscribeOptions = unsubscribeOptions ?? [];
    }

    public Guid Id { get; set; }
    /// <summary>
    /// WHy is From nullable? Because if the email message is being sent using a default account, 
    /// the From address will be determined by the emailer configuration and may not be explicitly set on the message. 
    /// In such cases, the From property can be left null to indicate that the default account's email address should be used as the sender. 
    /// This allows for flexibility in how email messages are created and sent, 
    /// while still providing the option to specify a custom sender when needed.
    /// </summary>
    public EmailAddress? From { get; set; }
    public IEnumerable<EmailAddress> To { get; set; }

    public IEnumerable<EmailAddress> Cc { get; set; } = [];

    public IEnumerable<EmailAddress> Bcc { get; set; } = [];

    public IEnumerable<EmailAddress> ReplyTo { get; set; } = [];

    public string Subject { get; set; }

    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }

    public IEnumerable<string> UnsubscribeOptions { get; set; } = [];
}
