
using Carrigan.Emailer;

namespace Carrigan.Emailer;
public interface IEmailConfiguration
{
    public EmailAddress? DefaultAccount { get; set; }
    public string? GetPasswordForAccount(string accountAddress);
    public IEnumerable<EmailDomainSmtpEndpoint> EmailDomainSmtpEndpoints { get; set; }
    public SecurityEnum? SecurityOption { get; set; }
    public string? PickupDirectory { get; set; }
    public bool UsePickupDirectory { get; set; }
    public bool? UseNetworkDelivery { get; set; }
}
