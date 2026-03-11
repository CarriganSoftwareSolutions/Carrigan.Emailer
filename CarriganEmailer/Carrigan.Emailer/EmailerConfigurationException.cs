// Ignore Spelling: Emailer

using Carrigan.Core.Extensions;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Carrigan.Emailer;

public sealed class EmailerConfigurationException : Exception
{
    // Private constructors prevent consumers from directly instantiating this exception.
    private EmailerConfigurationException(string message)
        : base(message)
    {
    }

    private EmailerConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Helper method that forces an exception to capture the stack trace.
    /// </summary>
    /// <param name="ex">The exception to capture.</param>
    /// <returns>The exception with its stack trace attached.</returns>
    private static Exception CaptureException(Exception ex)
    {
        try
        {
            throw ex;
        }
        catch (Exception captured)
        {
            return captured;
        }
    }


    public static void ThrowIfNull([NotNull]IEmailConfiguration? configuration)
    {
        if (configuration is null)
        {
            throw new EmailerConfigurationException("E-mailer configuration error. Configuration is null.");
        }
    }


    /// <summary>
    /// Validates the given <paramref name="configuration"/> and throws an EmailerConfigurationException
    /// if any configuration errors are detected.
    /// </summary>
    /// <param name="configuration">The email configuration to validate.</param>
    public static void ThrowIfConfigurationError(IEmailConfiguration? configuration)
    {
        EmailDomainSmtpLookup? domainSmtpLookup = null;
        if (configuration is null)
        {
            throw new EmailerConfigurationException("E-mailer configuration error. Configuration is null.");
        }

        List<Exception> exceptions = [];

        try
        {
            domainSmtpLookup = new(configuration.EmailDomainSmtpEndpoints);
        }
        catch(AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }
        catch(Exception ex)
        {
            exceptions.Add(ex);
        }

        // Validate DefaultAccount and related properties.
        if (configuration.DefaultAccount is null)
        {
            exceptions.Add(CaptureException(new NullReferenceException($"{nameof(configuration.DefaultAccount)} is null. You must provide a default email account.")));
        }
        else if (configuration.DefaultAccount.Address is null)
        {
            exceptions.Add(CaptureException(new NullReferenceException($"{nameof(configuration.DefaultAccount.Address)} is null. You must provide a default email account.")));
        }
        else if (configuration.DefaultAccount.Address.IsNullOrWhiteSpace())
        {
            exceptions.Add(CaptureException(new FormatException($"{nameof(configuration.DefaultAccount.Address)} is empty or white space. You must provide a default email account.")));
        }
        else if (configuration.DefaultAccount.Host.IsNullOrWhiteSpace())
        {
            exceptions.Add(CaptureException(new FormatException($"{nameof(configuration.DefaultAccount.Host)} is null, empty or white space. You must provide a valid default email account.")));
        }
        else
        {
            if (configuration.GetPasswordForAccount(configuration.DefaultAccount.Address).IsNullOrWhiteSpace())
            {
                exceptions.Add(CaptureException(new FormatException("You must provide a password for the default email account.")));
            }
            if (configuration.EmailDomainSmtpEndpoints
                    .Where(domain => domain.EmailDomain.Equals(configuration.DefaultAccount.Host, StringComparison.OrdinalIgnoreCase))
                    .None())
            {
                exceptions.Add(CaptureException(new FormatException("You must provide an SMTP endpoint for the default email account.")));
            }
        }

        // Validate pickup directory configuration.
        if (configuration.UsePickupDirectory)
        {
            if (configuration.PickupDirectory is null)
            {
                exceptions.Add(CaptureException(
                    new NullReferenceException($"{nameof(configuration.PickupDirectory)} is null. You must provide an email pickup directory.")));
            }
            else if (configuration.PickupDirectory.IsNullOrWhiteSpace())
            {
                exceptions.Add(CaptureException(
                    new NullReferenceException($"{nameof(configuration.PickupDirectory)} is empty or white space. You must provide an email pickup directory.")));
            }
            else if (configuration.PickupDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                exceptions.Add(CaptureException(
                    new NullReferenceException($"{nameof(configuration.PickupDirectory)} contains invalid characters. You must provide a valid email pickup directory.")));
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(configuration.PickupDirectory);
                }
                catch (Exception ex)
                {
                    exceptions.Add(CaptureException(
                        new AggregateException(
                            $"{nameof(configuration.PickupDirectory)} does not exist and cannot be created. You must provide a valid email pickup directory.",
                            ex)));
                }
            }
        }

        // Validate security option.
        if (configuration.SecurityOption is null)
        {
            exceptions.Add(CaptureException(
                new FormatException($"{nameof(configuration.SecurityOption)} is null. Please ensure you provided a properly formatted value.")));
        }
        else
        {
            switch (configuration.SecurityOption)
            {
                case SecurityEnum.Auto:
                case SecurityEnum.Ssl:
                case SecurityEnum.Tls:
                case SecurityEnum.None:
                    break;
                default:
                    exceptions.Add(CaptureException(new InvalidEnumArgumentException($"Invalid security option")));
                    break;
            }
        }

        // Validate network delivery settings.
        if (configuration.UseNetworkDelivery is null)
        {
            exceptions.Add(CaptureException(
                new FormatException($"{nameof(configuration.UseNetworkDelivery)} is null. Please set a valid value.")));
        }
        else if (configuration.UseNetworkDelivery == false && configuration.UsePickupDirectory == false)
        {
            exceptions.Add(CaptureException(
                new FormatException($"No delivery method enabled. Please enable either {nameof(configuration.UseNetworkDelivery)} or {nameof(configuration.UsePickupDirectory)}.")));
        }

        // Throw the custom exception based on the number of errors discovered.
        if (exceptions.Count == 1)
        {
            throw new EmailerConfigurationException("E-mailer configuration error. " + exceptions.First().Message);
        }
        else if (exceptions.Count > 1)
        {
            throw new EmailerConfigurationException(
                "The e-mailer contains multiple configuration errors. See inner exceptions for more details.",
                new AggregateException(exceptions));
        }
    }
}