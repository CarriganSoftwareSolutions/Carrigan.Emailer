namespace Carrigan.Emailer;

//IGNORE SPELLING: SMTP

public sealed record class EmailDomainSmtpEndpoint
{
    public required string EmailDomain { get; init; }

    public required string SmtpServer { get; init; }

    public required int SmtpPort { get; init; }
}