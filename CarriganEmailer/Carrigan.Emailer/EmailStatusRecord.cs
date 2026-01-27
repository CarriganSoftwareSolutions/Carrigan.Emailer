using Carrigan.Emailer;

namespace Carrigan.Emailer;
internal class EmailStatusRecord
{
    public EmailStatusRecord(EmailStatusEnum status, Guid emailId, EmailAddress? undeliverableAddress, Exception? ex)
    {
        Status = status;
        EmailId = emailId;
        UndeliverableAddress = undeliverableAddress;
        Ex = ex;

    }
    public EmailStatusRecord(EmailStatusEnum status, Guid emailId) : this(status, emailId, null, null)
    {
        Status = status;
        EmailId = emailId;
    }

    public EmailStatusRecord(EmailStatusEnum status, Guid emailId, Exception ex) : this(status, emailId, null, ex)
    {
        Ex = ex;
    }

    public EmailStatusEnum Status { get; set; }
    public Guid EmailId { get; set; }
    public EmailAddress? UndeliverableAddress { get; set; }
    public  Exception? Ex { get; set; }
}
