using Carrigan.Core.Interfaces;
using System.Net.Mail;

namespace Carrigan.Emailer;

public class EmailAddress : IEmpty
{
    public static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();
    public EmailAddress(string address, string? displayName = null)
    {
            Address = address;
            DisplayName = displayName ?? string.Empty;
        try
        {
            Host = (new MailAddress(Address, DisplayName)).Host;
        }
        catch
        {
            Host = string.Empty;
            _isEmpty = true;
        }
    }

    public readonly string Address;
    public readonly string DisplayName;
    public readonly string Host;
    protected readonly bool _isEmpty;
    public bool IsEmpty() =>
        _isEmpty;

    public bool IsNotEmpty() =>
        _isEmpty == false;

    public string GetFolderName()
    {
        if (IsEmpty())
            return "_";
        else
            return new string([.. Address?
                .ToLower()?
                .Select(character => _invalidPathChars.Contains(character) ? '_' : character) ?? ['_']]);
    }
}
