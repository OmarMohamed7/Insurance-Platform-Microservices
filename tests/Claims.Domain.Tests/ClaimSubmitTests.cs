using Claims.Domain;
using static Claims.Domain.Tests.ClaimTestData;

namespace Claims.Domain.Tests;

public class ClaimSubmitTests
{
    [Fact]
    public void Submit_with_valid_input_creates_a_submitted_claim()
    {
        var claim = Claim.Submit("POL-001", "claimant-1", "Rear-ended", Amount(250m), Now);

        Assert.NotEqual(Guid.Empty, claim.Id);
        Assert.Equal("POL-001", claim.PolicyNumber);
        Assert.Equal("claimant-1", claim.ClaimantId);
        Assert.Equal("Rear-ended", claim.Description);
        Assert.Equal(Amount(250m), claim.Amount);
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(Now, claim.SubmittedAt);
        Assert.Null(claim.Decision);
    }

    [Fact]
    public void Submit_trims_text_fields()
    {
        var claim = Claim.Submit("  POL-001 ", " claimant-1 ", "  Rear-ended  ", Amount(), Now);

        Assert.Equal("POL-001", claim.PolicyNumber);
        Assert.Equal("claimant-1", claim.ClaimantId);
        Assert.Equal("Rear-ended", claim.Description);
    }

    [Fact]
    public void Submit_raises_ClaimSubmitted()
    {
        var claim = Submitted();

        var raised = Assert.IsType<ClaimSubmitted>(Assert.Single(claim.DomainEvents));
        Assert.Equal(claim.Id, raised.ClaimId);
        Assert.Equal(claim.PolicyNumber, raised.PolicyNumber);
        Assert.Equal(claim.ClaimantId, raised.ClaimantId);
        Assert.Equal(claim.Amount, raised.Amount);
        Assert.Equal(Now, raised.OccurredAt);
    }

    [Fact]
    public void Submit_generates_a_different_id_each_time()
    {
        Assert.NotEqual(Submitted().Id, Submitted().Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_rejects_missing_policy_number(string? policyNumber)
    {
        Assert.Throws<ArgumentException>(() =>
            Claim.Submit(policyNumber!, "claimant-1", "desc", Amount(), Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_rejects_missing_claimant_id(string? claimantId)
    {
        Assert.Throws<ArgumentException>(() =>
            Claim.Submit("POL-001", claimantId!, "desc", Amount(), Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_rejects_missing_description(string? description)
    {
        Assert.Throws<ArgumentException>(() =>
            Claim.Submit("POL-001", "claimant-1", description!, Amount(), Now));
    }

    [Fact]
    public void Submit_accepts_description_at_the_max_length()
    {
        var claim = Claim.Submit("POL-001", "claimant-1", new string('x', Claim.MaxDescriptionLength), Amount(), Now);

        Assert.Equal(Claim.MaxDescriptionLength, claim.Description.Length);
    }

    [Fact]
    public void Submit_rejects_description_over_the_max_length()
    {
        var tooLong = new string('x', Claim.MaxDescriptionLength + 1);

        Assert.Throws<ArgumentException>(() =>
            Claim.Submit("POL-001", "claimant-1", tooLong, Amount(), Now));
    }

    [Fact]
    public void Submit_rejects_null_amount()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Claim.Submit("POL-001", "claimant-1", "desc", null!, Now));
    }

    [Fact]
    public void Submit_rejects_zero_amount()
    {
        Assert.Throws<ArgumentException>(() =>
            Claim.Submit("POL-001", "claimant-1", "desc", Amount(0m), Now));
    }
}
