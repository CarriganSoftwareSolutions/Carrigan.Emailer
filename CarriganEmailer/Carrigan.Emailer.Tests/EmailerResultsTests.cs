namespace Carrigan.Emailer.Tests;
//IGNORE SPELLING: EMAILER
public class EmailerResultsTests
{
    [Fact]
    public void Add_Success_AddsSuccessfulId()
    {
        Guid id = Guid.NewGuid();
        EmailerResults results = new();

        results.Add(new EmailStatusRecord(EmailStatusEnum.Success, id));

        Assert.Equal(id, Assert.Single(results.SuccessfulEmailIds));
        Assert.Empty(results.PermanentlyFailedEmailIds);
        Assert.Empty(results.EmailErrors);
        Assert.Equal(id, Assert.Single(results.EmailIdsToDelete));
    }

    [Fact]
    public void Add_PermanentAddressFailure_AddsAddressAndId()
    {
        Guid id = Guid.NewGuid();
        EmailAddress recipient = new("bad@example.com");
        EmailerResults results = new();

        results.Add(new EmailStatusRecord(EmailStatusEnum.PermanentAddressFailure, id, recipient, null));

        Assert.Same(recipient, Assert.Single(results.PermanentlyFailedRecipients));
        Assert.Equal(id, Assert.Single(results.PermanentlyFailedEmailIds));
    }

    [Fact]
    public void Add_PermanentAddressFailureWithoutAddress_StillAddsId()
    {
        EmailerResults results = new();
        Guid id = Guid.NewGuid();

        results.Add(new EmailStatusRecord(EmailStatusEnum.PermanentAddressFailure, id, null, null));

        Assert.Empty(results.PermanentlyFailedRecipients);
        Assert.Equal(id, Assert.Single(results.PermanentlyFailedEmailIds));
    }

    [Fact]
    public void Add_PermanentFormatFailure_AddsIdAndError()
    {
        EmailerResults results = new();
        Guid id = Guid.NewGuid();
        FormatException expected = new("Bad message");

        results.Add(new EmailStatusRecord(EmailStatusEnum.PermanentFormatFailure, id, expected));

        Assert.Equal(id, Assert.Single(results.PermanentlyFailedEmailIds));
        Assert.Same(expected, Assert.Single(results.EmailErrors));
    }

    [Fact]
    public void Add_TransientFailure_AddsErrorButDoesNotDeleteId()
    {
        EmailerResults results = new();
        IOException expected = new("Temporary failure");

        results.Add(new EmailStatusRecord(EmailStatusEnum.TransientFailure, Guid.NewGuid(), expected));

        Assert.Same(expected, Assert.Single(results.EmailErrors));
        Assert.Empty(results.EmailIdsToDelete);
    }

    [Fact]
    public void EmailIdsToDelete_RepeatedIdsAcrossCollections_ReturnsUniqueIds()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        EmailerResults results = new();
        results.PermanentlyFailedEmailIds.AddRange([first, first]);
        results.SuccessfulEmailIds.AddRange([first, second, second]);

        Guid[] expected = [first, second];
        Assert.Equal(expected, results.EmailIdsToDelete.ToArray());
    }

    [Fact]
    public void Errors_AllCategories_AreEnumerated()
    {
        EmailerResults results = new();
        Exception other = new InvalidOperationException("Other");
        Exception host = new IOException("Host");
        Exception account = new FormatException("Account");
        Exception email = new ArgumentException("Email");
        results.OtherErrors.Add(other);
        results.HostErrors.Add(host);
        results.AccountErrors.Add(account);
        results.EmailErrors.Add(email);

        Exception[] expected = [other, host, account, email];
        Assert.Equal(expected, results.Errors.ToArray());
    }
}
