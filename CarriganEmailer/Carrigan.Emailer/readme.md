# Carrigan.Emailer

`Carrigan.Emailer` is a configuration-driven email sender for .NET that supports SMTP delivery through MailKit and an optional pickup-directory mode that writes `.eml` files to disk.

- Target framework: `net10.0`
- License: Apache-2.0

---

## Important notice

`Carrigan.Emailer` is a delivery library.
It helps applications send email, but it does not by itself make an application compliant with anti-spam laws, marketing-email rules, privacy laws, retention rules, consent requirements, unsubscribe requirements, or provider policies.

Application developers remain responsible for ensuring that their use of this library complies with all applicable legal, regulatory, contractual, provider, and organizational requirements.

Examples include, without limitation:

- obtaining any required recipient consent
- honoring unsubscribe or opt-out requests where required
- providing any required sender identification, notices, or disclosures
- complying with retention, deletion, privacy, and security obligations
- following the terms and technical requirements of the selected email provider

---

## What it does

- SMTP network delivery using `MailKit.Net.Smtp.SmtpClient`
- Pickup-directory delivery that writes `.eml` files to disk
- Domain-based SMTP endpoint mapping through `EmailDomainSmtpEndpoint`
- Default sender support through `IEmailConfiguration.DefaultAccount`
- Optional `Cc`, `Bcc`, and `Reply-To`
- Optional `List-Unsubscribe` header support
- `Microsoft.Extensions.Logging` integration through `ILogger<Emailer>`
- Structured send results through `EmailerResults`

---

## Install

```bash
dotnet add package Carrigan.Emailer
```

---

## Configuration

`Carrigan.Emailer` uses a static configuration store internally.
Call:

```csharp
Emailer.ValidateConfiguration(configuration);
```

once during startup before sending email.

### Implement `IEmailConfiguration`

```csharp
using Carrigan.Emailer;

public sealed class MyEmailConfiguration : IEmailConfiguration
{
    public EmailAddress? DefaultAccount { get; set; } = new("no-reply@example.com", "My App");

    public IEnumerable<EmailDomainSmtpEndpoint> EmailDomainSmtpEndpoints { get; set; } =
    [
        new EmailDomainSmtpEndpoint
        {
            EmailDomain = "example.com",
            SmtpServer = "smtp.example.com",
            SmtpPort = 587
        }
    ];

    public SecurityEnum? SecurityOption { get; set; } = SecurityEnum.Tls;

    public string? PickupDirectory { get; set; }

    public bool UsePickupDirectory { get; set; }

    public bool? UseNetworkDelivery { get; set; } = true;

    public string? GetPasswordForAccount(string accountAddress)
    {
        return accountAddress switch
        {
            "no-reply@example.com" => Environment.GetEnvironmentVariable("EMAIL_NO_REPLY_PASSWORD"),
            _ => null
        };
    }
}
```

### Example startup configuration

```csharp
using Carrigan.Emailer;

MyEmailConfiguration config = new()
{
    DefaultAccount = new EmailAddress("no-reply@example.com", "My App"),

    EmailDomainSmtpEndpoints =
    [
        new EmailDomainSmtpEndpoint
        {
            EmailDomain = "example.com",
            SmtpServer = "smtp.example.com",
            SmtpPort = 587
        }
    ],

    SecurityOption = SecurityEnum.Tls,
    UseNetworkDelivery = true,
    UsePickupDirectory = false,
    PickupDirectory = "C:\\maildrop"
};

Emailer.ValidateConfiguration(config);
```

### What validation enforces

`Emailer.ValidateConfiguration(...)` will throw if, among other checks:

- `DefaultAccount` is missing or invalid
- `UseNetworkDelivery` is null
- neither network nor pickup delivery is enabled
- `SecurityOption` is null or invalid when network delivery is enabled
- `GetPasswordForAccount(DefaultAccount.Address)` returns null or empty when network delivery is enabled
- no matching `EmailDomainSmtpEndpoint` exists for `DefaultAccount.Host` when network delivery is enabled
- pickup-directory mode is enabled but `PickupDirectory` is invalid or unusable

---

## Sending email

```csharp
using Carrigan.Emailer;
using Microsoft.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger<Emailer> logger = loggerFactory.CreateLogger<Emailer>();

Emailer emailer = new(logger);

EmailMessage message = new(
    id: Guid.NewGuid(),
    toAddress: "recipient@example.com",
    subject: "Hello from Carrigan.Emailer",
    htmlBody: "<p>Hello!</p>",
    textBody: "Hello!"
);

EmailerResults results = await emailer.SendEmailsAsync(message);

if (results.Errors.Any())
{
    throw new AggregateException("One or more emails failed to send.", results.Errors);
}
```

### About `From`

- If `EmailMessage.From` is null, the library uses `configuration.DefaultAccount`.

### About message bodies

- `EmailMessage` requires at least one body.
- You may provide HTML, plain text, or both.

---

## Pickup directory mode

Enable pickup-directory output in configuration:

```csharp
config.UsePickupDirectory = true;
config.PickupDirectory = "C:\\maildrop";
```

Then call the same send method:

```csharp
EmailerResults results = await emailer.SendEmailsAsync(message);
```

Pickup output structure:

- `{PickupDirectory}/{recipientFolderName}/inbox/{timestamp}_{guid}.eml`
- `{PickupDirectory}/{senderFolderName}/sent/{timestamp}_{guid}.eml`

Folder names are derived from email addresses and sanitized for use as paths.

> Note: pickup-directory mode is primarily intended for development, testing, and local inspection scenarios. The library writes message files, but it does not manage retention, cleanup, archival, or directory security for you.

---

## List-Unsubscribe header

If `EmailMessage.UnsubscribeOptions` contains one or more values, the library adds a `List-Unsubscribe` header.

Example:

```csharp
EmailMessage newsletter = new(
    Guid.NewGuid(),
    new EmailAddress("recipient@example.com"),
    "Newsletter",
    "<p>...</p>",
    null,
    unsubscribeOptions: new[]
    {
        "mailto:unsubscribe@example.com?subject=unsubscribe",
        "https://example.com/unsubscribe?id=123"
    }
);

await emailer.SendEmailsAsync(newsletter);
```

---

## Convenience helper

For applications that use a simple HTML-email flow, `Emailer` also provides:

```csharp
await emailer.SendEmailAsync(
    toEmailAddress: "recipient@example.com",
    subject: "Confirm your account",
    htmlMessage: "<p>Click the link...</p>"
);
```

If sending fails, it throws an `AggregateException` containing the underlying errors.

---

## Results and error handling

`SendEmailsAsync(...)` returns `EmailerResults`, which exposes:

- `SuccessfulEmailIds`
- `PermanentlyFailedEmailIds`
- `PermanentlyFailedRecipients`
- `OtherErrors`
- `HostErrors`
- `AccountErrors`
- `EmailErrors`
- `Errors`
- `EmailIdsToDelete`

---

## Current repository status

The repository includes a test project, but it is currently a scaffold for future unit tests rather than a mature coverage suite.

## Support Development

If this package helps you, consider supporting development:

https://github.com/sponsors/CarriganSoftwareSolutions