using Claims.Domain;

namespace Claims.Domain.Tests;

internal static class ClaimTestData
{
    public static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public static Money Amount(decimal amount = 500m) => Money.Create(amount, "EGP");

    public static Claim Submitted() =>
        Claim.Submit("POL-001", "claimant-1", "Rear-ended at a junction", Amount(), Now);

    public static Claim UnderReview()
    {
        var claim = Submitted();
        claim.StartReview();
        return claim;
    }

    public static Claim Approved()
    {
        var claim = UnderReview();
        claim.Approve("admin-1", Now);
        return claim;
    }

    public static Claim Rejected()
    {
        var claim = UnderReview();
        claim.Reject("Not covered", "admin-1", Now);
        return claim;
    }

    public static Claim Paid()
    {
        var claim = Approved();
        claim.MarkPaid();
        return claim;
    }

    public static Claim PaymentFailed()
    {
        var claim = Approved();
        claim.MarkPaymentFailed();
        return claim;
    }

    public static Claim InStatus(ClaimStatus status) => status switch
    {
        ClaimStatus.Submitted => Submitted(),
        ClaimStatus.UnderReview => UnderReview(),
        ClaimStatus.Approved => Approved(),
        ClaimStatus.Rejected => Rejected(),
        ClaimStatus.Paid => Paid(),
        ClaimStatus.PaymentFailed => PaymentFailed(),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
