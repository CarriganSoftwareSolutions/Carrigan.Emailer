//Ignore Spelling: Emailer
using Carrigan.Emailer;

namespace Carrigan.Emailer;

public class EmailerResults
{
    public List<EmailAddress> PermanentlyFailedRecipients = [];
    public List<Guid> PermanentlyFailedEmailIds = [];
    public List<Guid> SuccessfulEmailIds = [];
    public List<Exception> OtherErrors { get; set; } = [];
    public List<Exception> HostErrors { get; set; } = [];
    public List<Exception> AccountErrors { get; set; } = [];
    public List<Exception> EmailErrors { get; set; } = [];
    public IEnumerable<Guid> EmailIdsToDelete => 
        PermanentlyFailedEmailIds.Distinct().AsEnumerable().Concat(SuccessfulEmailIds.AsEnumerable());
    public IEnumerable<Exception> Errors => 
        OtherErrors.Concat(HostErrors).Concat(AccountErrors).Concat(EmailErrors);
    internal void Add(EmailStatusRecord status)
    {
        if(status.Status == EmailStatusEnum.PermanentAddressFailure && status.UndeliverableAddress is not null)
        {
            PermanentlyFailedRecipients.Add(status.UndeliverableAddress);
        }

        if(status.Status == EmailStatusEnum.PermanentAddressFailure || status.Status == EmailStatusEnum.PermanentFormatFailure)
        {
            PermanentlyFailedEmailIds.Add(status.EmailId);
        }

        if (status.Status == EmailStatusEnum.Success)
        {
            SuccessfulEmailIds.Add(status.EmailId);
        }
        if (status.Ex is not null)
            EmailErrors.Add(status.Ex);
    }
}
