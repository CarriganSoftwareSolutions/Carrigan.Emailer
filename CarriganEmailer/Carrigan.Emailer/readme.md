# Carrigan.Emailer

`Carrigan.Emailer` is a small, configuration-driven email sender for .NET that supports **SMTP delivery** (via MailKit) and an optional **pickup directory** mode (writes `.eml` files to disk).

- Target framework: `net10.0`
- License: Apache-2.0

---

## What it does

- **SMTP network delivery** using `MailKit.Net.Smtp.SmtpClient`
- **Pickup directory delivery** (writes `.eml` messages to disk for testing / later processing)
- **Multiple sender accounts** per host (groups by sender host and sender address)
- Adds **List-Unsubscribe** header when provided
- Uses `Microsoft.Extensions.Logging` (`ILogger<Emailer>`) for error logging
- Returns a structured `EmailerResults` object for successes and failures

---

## Install

NuGet:

```bash
dotnet add package Carrigan.Emailer
```

---

## Configuration

`Carrigan.Emailer` uses a **static** configuration store internally. You must call:

```csharp
Emailer.ValidateConfiguration(configuration);
```

…once at startup **before** sending.

### Required: implement `IEmailConfiguration`

```csharp
using Carrigan.Emailer;

public sealed class MyEmailConfiguration : IEmailConfiguration
{
    public EmailAddress? DefaultAccount { get; set; }

    // Provide the SMTP password for a given sender address.
    public string? GetPasswordForAccount(string accountAddress)
    {
        // IMPORTANT: Do not hardcode real credentials in source.
        // Use environment variables, user-secrets, Key Vault, etc.
        return accountAddress switch
        {
            "no-reply@example.com" => Environment.GetEnvironmentVariable("EMAIL_NO_REPLY_PASSWORD"),
            _ => null
        };
    }

    // Key: email host (EmailAddress.Host), Value: SMTP port
    // Example: ["example.com"] = 587
    public Dictionary<string, int> Hosts { get; set; } = new();

    // Optional: map email host -> SMTP server host
    // Example: ["example.com"] = "smtp.example.com"
    public Dictionary<string, string> HostMappings { get; set; } = new();

    public SecurityEnum? SecurityOption { get; set; } = SecurityEnum.Auto;

    // Used when UsePickupDirectory = true
    public string? PickupDirectory { get; set; }

    public bool UsePickupDirectory { get; set; }

    // Must be non-null; at least one of UsePickupDirectory or UseNetworkDelivery must be enabled.
    public bool? UseNetworkDelivery { get; set; } = true;
}
```

### Example startup configuration

```csharp
using Carrigan.Emailer;

var config = new MyEmailConfiguration
{
    DefaultAccount = new EmailAddress("no-reply@example.com", "My App"),

    // If your email host differs from the SMTP server host, map it here.
    HostMappings =
    {
        ["example.com"] = "smtp.example.com"
    },

    // Ports are keyed by the *email host* (EmailAddress.Host).
    Hosts =
    {
        ["example.com"] = 587
    },

    SecurityOption = SecurityEnum.Tls,

    UseNetworkDelivery = true,
    UsePickupDirectory = false,

    // Only required if UsePickupDirectory = true
    PickupDirectory = "C:\\maildrop"
};

// Throws EmailerConfigurationException (possibly with an AggregateException inner) if invalid.
Emailer.ValidateConfiguration(config);
```

### What validation enforces

`Emailer.ValidateConfiguration(...)` will throw if (among other checks):

- `DefaultAccount` is missing or invalid
- `GetPasswordForAccount(DefaultAccount.Address)` returns null/empty
- `Hosts` does not contain a port entry for `DefaultAccount.Host`
- `SecurityOption` is null/invalid
- `UseNetworkDelivery` is null
- Neither network nor pickup delivery is enabled
- Pickup directory is enabled but `PickupDirectory` is invalid/uncreatable

---

## Sending email (SMTP)

```csharp
using Carrigan.Emailer;
using Microsoft.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger<Emailer> logger = loggerFactory.CreateLogger<Emailer>();

Emailer emailer = new(logger);

// EmailMessage requires at least one body: htmlBody or textBody.
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
    // Network/host/account/email errors are aggregated here.
    throw new AggregateException("One or more emails failed to send.", results.Errors);
}
```

### About `From`

- If `EmailMessage.From` is null, `Carrigan.Emailer` will set it to `configuration.DefaultAccount` before sending.

---

## Pickup directory mode (write .eml files)

Enable pickup in configuration:

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

Where `{recipientFolderName}` is derived from the email address and sanitized for use as a folder name.

> Note: `WriteEmailsToPickupDirectoryAsync(...)` currently returns a new `EmailerResults()` without populating success/failure lists; pickup mode is primarily a “write files” operation.

---

## List-Unsubscribe header

If `EmailMessage.UnsubscribeOptions` contains one or more values, the library adds:

`List-Unsubscribe: <value1>, <value2>, ...`

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

## Identity-style helper

For apps that use the common “send HTML email” pattern:

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

`SendEmailsAsync(...)` returns `EmailerResults`:

- `SuccessfulEmailIds`
- `PermanentlyFailedEmailIds`
- `PermanentlyFailedRecipients`
- `OtherErrors`, `HostErrors`, `AccountErrors`, `EmailErrors`
- `Errors` (all error lists concatenated)
- `EmailIdsToDelete` (successful + permanently failed IDs)

---

## Security notes

- Do not commit real SMTP credentials.
- Prefer environment variables, user secrets, or a secret manager.
- Be mindful of provider rules (app passwords, OAuth, etc.).

---

## Dependencies

This package references:

- `MailKit` (SMTP)
- `Microsoft.Extensions.Logging.Abstractions`
- `Carrigan.Core`

---

## License

Apache License 2.0.

See `LICENSE`.
