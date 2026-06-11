# Carrigan.Emailer

`Carrigan.Emailer` is a configuration-driven email delivery library for .NET built on top of MailKit.
It supports SMTP delivery and a pickup-directory mode that writes `.eml` files to disk for development and testing scenarios.

## Status

This repository is in early development.

- Current package targets: `net9.0` and `net10.0`
- The test project currently exists as a scaffold for future coverage
- The library API is usable, but the repository is still being hardened for broader public consumption

## Features

- SMTP delivery through MailKit
- Pickup-directory delivery for local inspection and test workflows
- Domain-based SMTP endpoint configuration
- Default sender support
- Optional `Cc`, `Bcc`, and `Reply-To`
- Optional `List-Unsubscribe` header support
- `ILogger<Emailer>` integration for diagnostics
- Structured send results through `EmailerResults`

## Repository layout

- `CarriganEmailer/Carrigan.Emailer/` - main library project
- `CarriganEmailer/Carrigan.Emailer.Tests/` - xUnit test project scaffold
- `.github/workflows/` - GitHub Actions workflows
- `LICENSE` - Apache 2.0 license for the repository

## Install

```bash
dotnet add package Carrigan.Emailer
```

## Quick start

Implement `IEmailConfiguration`, validate it once during startup, then send email through `Emailer`.

```csharp
using Carrigan.Emailer;
using Microsoft.Extensions.Logging;

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

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger<Emailer> logger = loggerFactory.CreateLogger<Emailer>();

MyEmailConfiguration configuration = new();
Emailer.ValidateConfiguration(configuration);

Emailer emailer = new(logger);

EmailMessage message = new(
    id: Guid.NewGuid(),
    toAddress: "recipient@example.com",
    subject: "Hello from Carrigan.Emailer",
    htmlBody: "<p>Hello!</p>",
    textBody: "Hello!"
);

EmailerResults results = await emailer.SendEmailsAsync(message);
```

## Configuration model

The current configuration contract is based on `IEmailConfiguration`.

Key members:

- `DefaultAccount` - the fallback sender address
- `GetPasswordForAccount(string accountAddress)` - returns the SMTP credential for the sender account
- `EmailDomainSmtpEndpoints` - maps an email domain to an SMTP server and port
- `SecurityOption` - SMTP security mode
- `UseNetworkDelivery` - enables SMTP delivery
- `UsePickupDirectory` / `PickupDirectory` - enables `.eml` file output to disk

Example SMTP endpoint:

```csharp
new EmailDomainSmtpEndpoint
{
    EmailDomain = "example.com",
    SmtpServer = "smtp.example.com",
    SmtpPort = 587
}
```

## Pickup directory mode

Pickup-directory mode is intended primarily for development, testing, and message inspection workflows.
It writes `.eml` files to disk. It is not intended to act as a fully managed production mail drop.

If you enable pickup-directory mode, your application remains responsible for:

- securing the directory
- managing retention and cleanup
- controlling access to any sensitive message content written to disk

## Testing

The repository includes an xUnit test project, but it is currently only a scaffold.
That is intentional at this stage and should not be read as a claim of meaningful automated coverage yet.

## Legal and operational notice

This library provides technical email-delivery functionality only.
It does not, by itself, make an application compliant with anti-spam laws, privacy laws, consent requirements, unsubscribe requirements, retention rules, or provider policies.

Applications using this library remain responsible for legal compliance, operational safeguards, credential handling, and secure treatment of message data.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Additional repository files

Depending on where this repository is hosted and how organization-wide defaults are configured, contribution and community-health files may also be supplied through a separate `.github` repository.

## Support Development

If this package helps you, consider supporting development:

https://github.com/sponsors/CarriganSoftwareSolutions