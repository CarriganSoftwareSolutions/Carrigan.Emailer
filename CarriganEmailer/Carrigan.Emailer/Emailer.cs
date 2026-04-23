//Ignore spelling: Emailer

using Carrigan.Core.Extensions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.ComponentModel;

namespace Carrigan.Emailer;

/// <remarks>
/// This library provides technical email-delivery functionality only.
/// It does not by itself make an application compliant with anti-spam laws,
/// privacy laws, consent requirements, unsubscribe requirements, retention rules,
/// provider policies, or other legal, regulatory, contractual, or organizational obligations.
///
/// Applications using this library remain responsible for:
/// <list type="bullet">
/// <item><description>obtaining any required consent or authorization before sending email,</description></item>
/// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
/// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
/// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
/// </list>
///
/// Where this library writes message data to disk, logs, or other storage locations,
/// those outputs may contain recipient addresses, sender addresses, message bodies,
/// attachments, links, or other sensitive information. The consuming application is
/// responsible for securing those locations, controlling access, and implementing any
/// necessary cleanup, retention, archival, or deletion behavior.
/// </remarks>
public class Emailer
{
    private static IEmailConfiguration? _configuration;
    private static EmailDomainSmtpLookup? _configuationSmptEndPoints;
    private readonly ILogger<Emailer>? _logger;

    /// <remarks>
    /// This library provides technical email-delivery functionality only.
    /// It does not by itself make an application compliant with anti-spam laws,
    /// privacy laws, consent requirements, unsubscribe requirements, retention rules,
    /// provider policies, or other legal, regulatory, contractual, or organizational obligations.
    ///
    /// Applications using this library remain responsible for:
    /// <list type="bullet">
    /// <item><description>obtaining any required consent or authorization before sending email,</description></item>
    /// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
    /// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
    /// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
    /// </list>
    ///
    /// Where this library writes message data to disk, logs, or other storage locations,
    /// those outputs may contain recipient addresses, sender addresses, message bodies,
    /// attachments, links, or other sensitive information. The consuming application is
    /// responsible for securing those locations, controlling access, and implementing any
    /// necessary cleanup, retention, archival, or deletion behavior.
    /// </remarks>
    public static void ValidateConfiguration(IEmailConfiguration configuration)
    {
        EmailerConfigurationException.ThrowIfConfigurationError(configuration);
        _configuration = configuration;
        _configuationSmptEndPoints = new(configuration.EmailDomainSmtpEndpoints);
    }

    /// <remarks>
    /// This library provides technical email-delivery functionality only.
    /// It does not by itself make an application compliant with anti-spam laws,
    /// privacy laws, consent requirements, unsubscribe requirements, retention rules,
    /// provider policies, or other legal, regulatory, contractual, or organizational obligations.
    ///
    /// Applications using this library remain responsible for:
    /// <list type="bullet">
    /// <item><description>obtaining any required consent or authorization before sending email,</description></item>
    /// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
    /// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
    /// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
    /// </list>
    ///
    /// Where this library writes message data to disk, logs, or other storage locations,
    /// those outputs may contain recipient addresses, sender addresses, message bodies,
    /// attachments, links, or other sensitive information. The consuming application is
    /// responsible for securing those locations, controlling access, and implementing any
    /// necessary cleanup, retention, archival, or deletion behavior.
    /// </remarks>
    public Emailer(ILogger<Emailer>? logger = null) => 
        _logger = logger;

    protected static SecureSocketOptions GetSocketSecurityOption()
    {
        EmailerConfigurationException.ThrowIfNull(_configuration);
        return _configuration.SecurityOption switch
        {
            SecurityEnum.Auto => SecureSocketOptions.Auto,
            SecurityEnum.Ssl => SecureSocketOptions.SslOnConnect,
            SecurityEnum.Tls => SecureSocketOptions.StartTls,
            SecurityEnum.None => SecureSocketOptions.None,
            _ => throw new InvalidEnumArgumentException("Unexpected enum value encountered."),
        };
    }


    protected static MimeMessage GetMimeMessageFromEmail(EmailMessage email)
    {

        MimeMessage mimeMessage = new();
        EmailAddress? from = email.From ?? _configuration?.DefaultAccount;
        mimeMessage.From.Add(new MailboxAddress(from?.DisplayName ?? string.Empty, from?.Address ?? throw new NullReferenceException("All emails require a from address.")));

        mimeMessage.To.AddRange(email.To.Select(to => new MailboxAddress(to.DisplayName, to.Address)));
        if(email.Cc.Any())
            mimeMessage.Cc.AddRange(email.Cc.Select(cc => new MailboxAddress(cc.DisplayName, cc.Address)));
        if(email.Bcc.Any())
            mimeMessage.Bcc.AddRange(email.Bcc.Select(bcc => new MailboxAddress(bcc.DisplayName, bcc.Address)));
        if(email.ReplyTo.Any())
            mimeMessage.ReplyTo.AddRange(email.ReplyTo.Select(replyTo => new MailboxAddress(replyTo.DisplayName, replyTo.Address)));
        mimeMessage.Subject = email.Subject;

        if (email.HtmlBody.IsNotNullOrWhiteSpace() && email.TextBody.IsNotNullOrWhiteSpace())
        {
            mimeMessage.Body = new BodyBuilder
            {
                TextBody = email.TextBody,
                HtmlBody = email.HtmlBody
            }.ToMessageBody();
        }
        else if (email.HtmlBody.IsNotNullOrWhiteSpace() && email.TextBody.IsNullOrWhiteSpace())
            mimeMessage.Body = new TextPart("html") { Text = email.HtmlBody };
        else if (email.HtmlBody.IsNullOrWhiteSpace() && email.TextBody.IsNotNullOrWhiteSpace())
            mimeMessage.Body = new TextPart("plain") { Text = email.TextBody };
        else
            throw new FormatException($"You must provide at least one message body either as a {nameof(email.HtmlBody)} or a {nameof(email.HtmlBody)}.");

        if (email.UnsubscribeOptions.Any())
            mimeMessage.Headers.Add($"List-Unsubscribe", string.Join(", ", email.UnsubscribeOptions.Select(value => $"<{value}>")));

        return mimeMessage;
    }

    internal async Task<EmailStatusRecord> SendEmailNetworkAsync(EmailMessage email, SmtpClient client)
    {
        Guid emailId = email.Id;
        MimeMessage mimeMessage;
        try
        { 
            mimeMessage = GetMimeMessageFromEmail(email);
        }
        catch(Exception ex)
        {
            return new EmailStatusRecord(EmailStatusEnum.PermanentFormatFailure, email.Id, ex);
        }
        try
        {
            await client.SendAsync(mimeMessage);
            return new EmailStatusRecord(EmailStatusEnum.Success, email.Id);
        }
        catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
        {
            string failedAddress = ex.Mailbox?.Address ?? "null";
            _logger?.LogError(ex, "Permanent delivery failure for email address: {failedAddress} for email id of {emailId}", failedAddress, emailId);
            return new EmailStatusRecord(EmailStatusEnum.PermanentAddressFailure, email.Id, new EmailAddress(failedAddress), ex);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentNullException || ex is InvalidOperationException)
        {
            _logger?.LogError(ex, "Email id of {emailId} failed due to a possible formatting error.", emailId);
            return new EmailStatusRecord(EmailStatusEnum.PermanentFormatFailure, email.Id, ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Email id of {emailId} failed and will be retried later if it hasn't expired.", emailId);
            return new EmailStatusRecord(EmailStatusEnum.TransientFailure, email.Id, ex);
        }
    }

    protected async Task<EmailerResults> SendEmailsNetworkAsync(params IEnumerable<EmailMessage> emails)
    {
        EmailerConfigurationException.ThrowIfNull(_configuration);
        EmailerResults results = new();
        EmailAddress? defaultFrom = _configuration.DefaultAccount;
        foreach(EmailMessage email in emails)
        {
            email.From ??= defaultFrom;
        }
        try
        {
            // Group emails by host (using sender's host or default)
            foreach (IGrouping<string, EmailMessage> hostGroup in emails.GroupBy(email => email.From?.Host ?? throw new NullReferenceException("All emails require a from address.")))
            {
                using SmtpClient client = new();
                try
                {
                    string emailDomain = hostGroup.Key;
                    EmailDomainSmtpEndpoint? emailDomainSmtpEndpoint = _configuationSmptEndPoints?.Lookup(emailDomain) ?? throw new NullReferenceException($"Attempted to send an email from a domain name, {emailDomain}, that doesn't have a mapped SMPT server.");
                    string smptServerHost = emailDomainSmtpEndpoint.SmtpServer;
                    int smptPort = emailDomainSmtpEndpoint.SmtpPort;
                    SecureSocketOptions secureSocketOption = GetSocketSecurityOption();
                    string secureSocketOptionString = secureSocketOption.ToString();
                    try
                    {
                        await client.ConnectAsync(smptServerHost, smptPort, secureSocketOption);
                        // Now group within the host by sender address
                        foreach (IGrouping<string, EmailMessage> addressGroup in hostGroup.GroupBy(email => email.From?.Address ?? throw new NullReferenceException("All emails require a from address.")))
                        {
                            string emailAddress = addressGroup.Key;
                            try
                            {
                                await client.AuthenticateAsync(emailAddress, _configuration.GetPasswordForAccount(emailAddress) ?? string.Empty);
                                foreach (EmailMessage email in addressGroup)
                                {
                                    results.Add(await SendEmailNetworkAsync(email, client));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Attempt to send emails from the account, {emailAddress}, failed.", emailAddress);
                                results.AccountErrors.Add(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Attempt to send emails from the host, {smptServerHost}, on port, {smptServerHost}, using Secure Socket Option, {secureSocketOptionString}, failed.", smptServerHost, smptPort, secureSocketOptionString.ToString());
                        results.HostErrors.Add(ex);
                    }
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "An Error Occurred");
                    results.OtherErrors.Add(ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An Error Occurred");
            results.OtherErrors.Add(ex);
        }
        return results;
    }

    private static async Task WriteEmailToAddressPath(string pickupDirectory, EmailAddress? address, string mailFolder, EmailMessage email)
    {
        DateTime today = DateTime.Now;
        string directoryPath = Path.Combine(pickupDirectory, address?.GetFolderName() ?? "_", mailFolder);

        string fileName = $"{today.ToFileName()}_{Guid.NewGuid()}.eml";
        string filePath = Path.Combine(directoryPath, fileName);

        Directory.CreateDirectory(directoryPath);

        using FileStream stream = new(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        MimeMessage mimeMessage = GetMimeMessageFromEmail(email);
        await Task.Run(() => mimeMessage.WriteTo(stream));
        return;
    }


    /// <remarks>
    /// This library provides technical email-delivery functionality only.
    /// It does not by itself make an application compliant with anti-spam laws,
    /// privacy laws, consent requirements, unsubscribe requirements, retention rules,
    /// provider policies, or other legal, regulatory, contractual, or organizational obligations.
    ///
    /// Applications using this library remain responsible for:
    /// <list type="bullet">
    /// <item><description>obtaining any required consent or authorization before sending email,</description></item>
    /// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
    /// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
    /// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
    /// </list>
    ///
    /// Where this library writes message data to disk, logs, or other storage locations,
    /// those outputs may contain recipient addresses, sender addresses, message bodies,
    /// attachments, links, or other sensitive information. The consuming application is
    /// responsible for securing those locations, controlling access, and implementing any
    /// necessary cleanup, retention, archival, or deletion behavior.
    /// </remarks>
    public static async Task<EmailerResults> WriteEmailsToPickupDirectoryAsync(params IEnumerable<EmailMessage> emails)
    {
        EmailerConfigurationException.ThrowIfNull(_configuration);
        if (_configuration.UsePickupDirectory)
        {
            string directoryPath = _configuration.PickupDirectory ?? throw new NullReferenceException("Email drop directory not provided.");

            foreach (EmailMessage email in emails)
            {
                foreach (EmailAddress address in email.To.Concat(email.Bcc).Concat(email.Cc).Distinct())
                {
                    await WriteEmailToAddressPath(directoryPath, address, "inbox", email);
                }
                await WriteEmailToAddressPath(directoryPath, email.From ?? _configuration?.DefaultAccount, "sent", email);
            }
        }
        return new EmailerResults();
    }

    
    /// <remarks>
    /// This library provides technical email-delivery functionality only.
    /// It does not by itself make an application compliant with anti-spam laws,
    /// privacy laws, consent requirements, unsubscribe requirements, retention rules,
    /// provider policies, or other legal, regulatory, contractual, or organizational obligations.
    ///
    /// Applications using this library remain responsible for:
    /// <list type="bullet">
    /// <item><description>obtaining any required consent or authorization before sending email,</description></item>
    /// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
    /// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
    /// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
    /// </list>
    ///
    /// Where this library writes message data to disk, logs, or other storage locations,
    /// those outputs may contain recipient addresses, sender addresses, message bodies,
    /// attachments, links, or other sensitive information. The consuming application is
    /// responsible for securing those locations, controlling access, and implementing any
    /// necessary cleanup, retention, archival, or deletion behavior.
    /// </remarks>
    public async Task<EmailerResults> SendEmailsAsync(params IEnumerable<EmailMessage> emails)
    {
        EmailerConfigurationException.ThrowIfNull(_configuration);
        EmailerResults? results = null;
        if (_configuration.UsePickupDirectory)
            results = await WriteEmailsToPickupDirectoryAsync(emails);
        if(_configuration.UseNetworkDelivery ?? throw new FormatException($"E-mailer configuration error. {nameof(_configuration.UseNetworkDelivery)} is null, please set a valid value."))
            results = await SendEmailsNetworkAsync(emails);
        return results ?? throw new FormatException($"E-mailer configuration error. No delivery method enabled, please enable either {nameof(_configuration.UseNetworkDelivery)} or {_configuration.UsePickupDirectory}");
    }

    /// <remarks>
    /// This library provides technical email-delivery functionality only.
    /// It does not by itself make an application compliant with anti-spam laws,
    /// privacy laws, consent requirements, unsubscribe requirements, retention rules,
    /// provider policies, or other legal, regulatory, contractual, or organizational obligations.
    ///
    /// Applications using this library remain responsible for:
    /// <list type="bullet">
    /// <item><description>obtaining any required consent or authorization before sending email,</description></item>
    /// <item><description>providing any required notices, sender identification, or unsubscribe handling,</description></item>
    /// <item><description>complying with applicable retention, deletion, privacy, and security requirements, and</description></item>
    /// <item><description>following the rules and technical requirements of the selected email provider or delivery environment.</description></item>
    /// </list>
    ///
    /// Where this library writes message data to disk, logs, or other storage locations,
    /// those outputs may contain recipient addresses, sender addresses, message bodies,
    /// attachments, links, or other sensitive information. The consuming application is
    /// responsible for securing those locations, controlling access, and implementing any
    /// necessary cleanup, retention, archival, or deletion behavior.
    /// </remarks>
    //This is used by Identity to send HTML encoded emails, as such the IsBodyHtml flag is set accordingly.
    public async Task SendEmailAsync(string toEmailAddress, string subject, string htmlMessage)
    {
        EmailMessage email = new(Guid.NewGuid(), toEmailAddress, subject, htmlMessage, null);
        EmailerResults results = await SendEmailsAsync(email);
        if (results.Errors.Any())
            throw new AggregateException("Could not send email.", results.Errors);
        else
            return;

    }
}
