//Ignore Spelling: Smtp
namespace Carrigan.Emailer;
public interface IEmailConfiguration
{
    public EmailAddress? DefaultAccount { get; set; }
    public string? GetPasswordForAccount(string accountAddress);
    public IEnumerable<EmailDomainSmtpEndpoint> EmailDomainSmtpEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the transport security option used for SMTP connections.
    /// </summary>
    /// <remarks>
    /// Selects how the library should negotiate or require transport security when sending
    /// email over the network.
    ///
    /// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
    /// unless you have a specific, validated reason to use a different option.
    /// Unencrypted or weaker configurations may expose credentials and message data in transit.
    /// </remarks>
    public SecurityEnum? SecurityOption { get; set; }

    /// <remarks>
    /// Pickup directory mode is intended primarily for development, testing, local inspection,
    /// or other non-production scenarios where writing <c>.eml</c> files to disk is desirable.
    ///
    /// This option is not intended to turn the configured pickup directory into a managed
    /// production mail drop. The library writes message files, but it does not manage the
    /// lifecycle of the pickup directory or its contents.
    ///
    /// In particular, this library does not:
    /// <list type="bullet">
    /// <item><description>delete old pickup files,</description></item>
    /// <item><description>enforce retention limits,</description></item>
    /// <item><description>rotate or archive files,</description></item>
    /// <item><description>monitor disk usage,</description></item>
    /// <item><description>secure or sanitize the directory after delivery.</description></item>
    /// </list>
    ///
    /// Depending on message content, pickup files may contain recipient addresses, sender addresses,
    /// message bodies, attachments, links, or other sensitive information. Applications using this
    /// option are responsible for securing the directory, controlling access, and implementing any
    /// needed retention or cleanup behavior.
    /// </remarks>
    public string? PickupDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether email messages should be written to a pickup directory
    /// instead of being sent over the network.
    /// </summary>
    /// <remarks>
    /// Pickup directory mode is intended primarily for development, testing, local inspection,
    /// or other non-production scenarios where writing <c>.eml</c> files to disk is desirable.
    ///
    /// This option is not intended to turn the configured pickup directory into a managed
    /// production mail drop. The library writes message files, but it does not manage the
    /// lifecycle of the pickup directory or its contents.
    ///
    /// In particular, this library does not:
    /// <list type="bullet">
    /// <item><description>delete old pickup files,</description></item>
    /// <item><description>enforce retention limits,</description></item>
    /// <item><description>rotate or archive files,</description></item>
    /// <item><description>monitor disk usage,</description></item>
    /// <item><description>secure or sanitize the directory after delivery.</description></item>
    /// </list>
    ///
    /// Depending on message content, pickup files may contain recipient addresses, sender addresses,
    /// message bodies, attachments, links, or other sensitive information. Applications using this
    /// option are responsible for securing the directory, controlling access, and implementing any
    /// needed retention or cleanup behavior.
    /// </remarks>
    public bool UsePickupDirectory { get; set; }
    public bool? UseNetworkDelivery { get; set; }
}
