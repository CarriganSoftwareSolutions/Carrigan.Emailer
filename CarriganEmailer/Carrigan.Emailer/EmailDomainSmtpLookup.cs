using System.Collections.ObjectModel;
using Carrigan.Core.Extensions;

namespace Carrigan.Emailer;

internal sealed class EmailDomainSmtpLookup
{
    private readonly ReadOnlyDictionary<string, EmailDomainSmtpEndpoint> DomainSmtpLookup;

    internal EmailDomainSmtpLookup(IEnumerable<EmailDomainSmtpEndpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        List<Exception> exceptions = [];

        if (ContainsDuplicateDomains(endpoints))
        {
            exceptions.Add(new ArgumentException("The collection contains duplicate email domains.", nameof(endpoints)));
        }

        Dictionary<string, EmailDomainSmtpEndpoint> dictionary = new(StringComparer.OrdinalIgnoreCase);

        foreach (EmailDomainSmtpEndpoint endpoint in endpoints)
        {
            if (endpoint is null)
            {
                exceptions.Add(new ArgumentNullException(nameof(endpoints), "The collection contains a null endpoint."));
            }
            else
            {
                if (endpoint.EmailDomain.IsNullOrWhiteSpace())
                {
                    exceptions.Add(new ArgumentException("EmailDomain cannot be null, empty, or whitespace.", nameof(endpoints)));
                }

                
                if (endpoint.SmtpServer.IsNullOrWhiteSpace())
                {
                    exceptions.Add(new ArgumentException("SmtpServer cannot be null, empty, or whitespace.", nameof(endpoints)));
                }

                if (endpoint.SmtpPort <= 0)
                {
                    exceptions.Add(new ArgumentOutOfRangeException(nameof(endpoints), "SmtpPort must be greater than zero."));
                }

                if (endpoint.EmailDomain.IsNotNullOrWhiteSpace()
                    && endpoint.SmtpServer.IsNotNullOrWhiteSpace()
                    && endpoint.SmtpPort > 0
                    && dictionary.DoesNotContainKey(endpoint.EmailDomain))
                {
                    dictionary.Add(endpoint.EmailDomain, endpoint);
                }
            }
        }

        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException("One or more errors occurred while building the email domain SMTP lookup.", exceptions);
        }

        DomainSmtpLookup = new ReadOnlyDictionary<string, EmailDomainSmtpEndpoint>(dictionary);
    }

    /// <summary>
    /// Returns true if the given domain is not present in the lookup.
    /// </summary>
    internal bool DoesNotContainDomain(string emailDomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailDomain);

        return DomainSmtpLookup.DoesNotContainKey(emailDomain);
    }

    /// <summary>
    /// Attempts to look up the SMTP endpoint for the given email domain.
    /// </summary>
    internal bool TryLookup(string emailDomain, out EmailDomainSmtpEndpoint? endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailDomain);

        bool found = DomainSmtpLookup.TryGetValue(emailDomain, out EmailDomainSmtpEndpoint? value);
        endpoint = value;
        return found;
    }

    /// <summary>
    /// Looks up the SMTP endpoint for the given email domain.
    /// Returns null if no match exists.
    /// </summary>
    internal EmailDomainSmtpEndpoint? Lookup(string emailDomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailDomain);

        return DomainSmtpLookup.TryGetValue(emailDomain, out EmailDomainSmtpEndpoint? endpoint) ? endpoint : null;
    }

    /// <summary>
    /// Returns true if the provided collection contains duplicate email domains,
    /// using a case-insensitive comparison.
    /// </summary>
    internal static bool ContainsDuplicateDomains(IEnumerable<EmailDomainSmtpEndpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        HashSet<string> seenDomains = new(StringComparer.OrdinalIgnoreCase);

        foreach (EmailDomainSmtpEndpoint endpoint in endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoint);

            if (endpoint.EmailDomain.IsNotNullOrWhiteSpace())
            {
                if (seenDomains.DoesNotContain(endpoint.EmailDomain))
                {
                    seenDomains.Add(endpoint.EmailDomain);
                }
                else
                {
                    return true;
                }
            }
        }

        return false;
    }
}