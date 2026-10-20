using Claims.Domain;
using static Claims.Domain.Tests.ClaimTestData;

namespace Claims.Domain.Tests;

public class ClaimTransitionTests
{
    // ---- StartReview: Submitted -> UnderReview ----

    [Fact]
    public void StartReview_moves_a_submitted_claim_under_review()
    {
        var claim = Submitted();

        claim.StartReview();

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    [Theory]
    [InlineData(ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    [InlineData(ClaimStatus.PaymentFailed)]
    public void StartReview_is_illegal_unless_submitted(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.StartReview());

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.UnderReview, ex.To);
        Assert.Equal(from, claim.Status);
    }

    // ---- Approve: UnderReview -> Approved ----

    [Fact]
    public void Approve_moves_an_under_review_claim_to_approved_and_records_the_decision()
    {
        var claim = UnderReview();
        claim.ClearDomainEvents();

        claim.Approve("admin-1", Now);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.NotNull(claim.Decision);
        Assert.Equal(DecisionOutcome.Approved, claim.Decision!.Outcome);
        Assert.Equal("admin-1", claim.Decision.DecidedBy);
        Assert.Equal(Now, claim.Decision.DecidedAt);
    }

    [Fact]
    public void Approve_raises_ClaimApproved()
    {
        var claim = UnderReview();
        claim.ClearDomainEvents();

        claim.Approve("admin-1", Now);

        var raised = Assert.IsType<ClaimApproved>(Assert.Single(claim.DomainEvents));
        Assert.Equal(claim.Id, raised.ClaimId);
        Assert.Equal(claim.Amount, raised.Amount);
        Assert.Equal(Now, raised.OccurredAt);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    [InlineData(ClaimStatus.PaymentFailed)]
    public void Approve_is_illegal_unless_under_review(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.Approve("admin-1", Now));

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.Approved, ex.To);
        Assert.Equal(from, claim.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_requires_who_decided(string? decidedBy)
    {
        var claim = UnderReview();

        Assert.Throws<ArgumentException>(() => claim.Approve(decidedBy!, Now));
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    // ---- Reject: UnderReview -> Rejected ----

    [Fact]
    public void Reject_moves_an_under_review_claim_to_rejected_and_records_the_decision()
    {
        var claim = UnderReview();
        claim.ClearDomainEvents();

        claim.Reject("Not covered", "admin-1", Now);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.NotNull(claim.Decision);
        Assert.Equal(DecisionOutcome.Rejected, claim.Decision!.Outcome);
        Assert.Equal("Not covered", claim.Decision.Reason);
        Assert.Equal("admin-1", claim.Decision.DecidedBy);
        Assert.Equal(Now, claim.Decision.DecidedAt);
    }

    [Fact]
    public void Reject_raises_ClaimRejected()
    {
        var claim = UnderReview();
        claim.ClearDomainEvents();

        claim.Reject("Not covered", "admin-1", Now);

        var raised = Assert.IsType<ClaimRejected>(Assert.Single(claim.DomainEvents));
        Assert.Equal(claim.Id, raised.ClaimId);
        Assert.Equal("Not covered", raised.Reason);
        Assert.Equal(Now, raised.OccurredAt);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    [InlineData(ClaimStatus.PaymentFailed)]
    public void Reject_is_illegal_unless_under_review(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.Reject("reason", "admin-1", Now));

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.Rejected, ex.To);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Reject_requires_a_reason(string? reason)
    {
        var claim = UnderReview();

        Assert.Throws<ArgumentException>(() => claim.Reject(reason!, "admin-1", Now));
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Reject_requires_who_decided(string? decidedBy)
    {
        var claim = UnderReview();

        Assert.Throws<ArgumentException>(() => claim.Reject("reason", decidedBy!, Now));
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    // ---- MarkPaid: Approved -> Paid ----

    [Fact]
    public void MarkPaid_moves_an_approved_claim_to_paid()
    {
        var claim = Approved();

        claim.MarkPaid();

        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    [InlineData(ClaimStatus.PaymentFailed)]
    public void MarkPaid_is_illegal_unless_approved(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.MarkPaid());

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.Paid, ex.To);
    }

    // ---- MarkPaymentFailed: Approved -> PaymentFailed ----

    [Fact]
    public void MarkPaymentFailed_moves_an_approved_claim_to_payment_failed()
    {
        var claim = Approved();

        claim.MarkPaymentFailed();

        Assert.Equal(ClaimStatus.PaymentFailed, claim.Status);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    [InlineData(ClaimStatus.PaymentFailed)]
    public void MarkPaymentFailed_is_illegal_unless_approved(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.MarkPaymentFailed());

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.PaymentFailed, ex.To);
    }

    // ---- RetryPayment: PaymentFailed -> Approved ----

    [Fact]
    public void RetryPayment_moves_a_failed_claim_back_to_approved_and_raises_ClaimApproved()
    {
        var claim = PaymentFailed();
        claim.ClearDomainEvents();

        claim.RetryPayment(Now);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        var raised = Assert.IsType<ClaimApproved>(Assert.Single(claim.DomainEvents));
        Assert.Equal(claim.Id, raised.ClaimId);
        Assert.Equal(claim.Amount, raised.Amount);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Paid)]
    public void RetryPayment_is_illegal_unless_payment_failed(ClaimStatus from)
    {
        var claim = InStatus(from);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.RetryPayment(Now));

        Assert.Equal(from, ex.From);
        Assert.Equal(ClaimStatus.Approved, ex.To);
    }

    // ---- Terminal states and events ----

    [Fact]
    public void Rejected_and_paid_claims_accept_no_further_transitions()
    {
        foreach (var claim in new[] { Rejected(), Paid() })
        {
            Assert.Throws<InvalidClaimTransitionException>(() => claim.StartReview());
            Assert.Throws<InvalidClaimTransitionException>(() => claim.Approve("admin-1", Now));
            Assert.Throws<InvalidClaimTransitionException>(() => claim.Reject("r", "admin-1", Now));
            Assert.Throws<InvalidClaimTransitionException>(() => claim.MarkPaid());
            Assert.Throws<InvalidClaimTransitionException>(() => claim.MarkPaymentFailed());
            Assert.Throws<InvalidClaimTransitionException>(() => claim.RetryPayment(Now));
        }
    }

    [Fact]
    public void ClearDomainEvents_removes_raised_events()
    {
        var claim = Submitted();
        Assert.NotEmpty(claim.DomainEvents);

        claim.ClearDomainEvents();

        Assert.Empty(claim.DomainEvents);
    }

    [Fact]
    public void Full_happy_path_submit_review_approve_pay()
    {
        var claim = Submitted();

        claim.StartReview();
        claim.Approve("admin-1", Now);
        claim.MarkPaid();

        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }

    [Fact]
    public void Payment_can_fail_and_be_retried_then_paid()
    {
        var claim = Approved();

        claim.MarkPaymentFailed();
        claim.RetryPayment(Now);
        claim.MarkPaid();

        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }
}
