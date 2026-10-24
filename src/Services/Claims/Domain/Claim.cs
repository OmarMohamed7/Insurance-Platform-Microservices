namespace Claims.Domain;

public sealed class Claim
{
    public const int MaxDescriptionLength = 200;

    private readonly List<IDomainEvent> _events = [];

    public Guid Id { get; private set; }
    public string PolicyNumber { get; private set; } = null!;
    public string ClaimantId { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public ClaimDecision? Decision { get; private set; }
    public ClaimStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;

    private Claim() { }

    public static Claim Submit(string policyNumber, string claimantId, string description, Money amount, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(policyNumber))
            throw new ArgumentException("Policy number is required.", nameof(policyNumber));

        if (string.IsNullOrWhiteSpace(claimantId))
            throw new ArgumentException("Claimant id is required.", nameof(claimantId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.", nameof(description));

        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
            throw new ArgumentException("Claim amount must be greater than zero.", nameof(amount));

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            PolicyNumber = policyNumber.Trim(),
            ClaimantId = claimantId.Trim(),
            Description = description.Trim(),
            Amount = amount,
            Status = ClaimStatus.Submitted,
            SubmittedAt = now
        };

        claim._events.Add(new ClaimSubmitted(claim.Id, claim.PolicyNumber, claim.ClaimantId, claim.Amount, now));
        return claim;
    }

    public void StartReview()
    {
        EnsureStatus(ClaimStatus.Submitted, ClaimStatus.UnderReview);
        Status = ClaimStatus.UnderReview;
        _events.Add(new StartReviewClaimEvent(Id,  DateTime.UtcNow));
    }

    public void Approve(string decidedBy, DateTime now)
    {
        EnsureStatus(ClaimStatus.UnderReview, ClaimStatus.Approved);
        RequireText(decidedBy, nameof(decidedBy));

        Status = ClaimStatus.Approved;
        Decision = ClaimDecision.Approved(null, decidedBy, now);
        _events.Add(new ClaimApproved(Id, ClaimantId, Amount, now));
    }

    public void Reject(string reason, string decidedBy, DateTime now)
    {
        EnsureStatus(ClaimStatus.UnderReview, ClaimStatus.Rejected);
        RequireText(reason, nameof(reason));
        RequireText(decidedBy, nameof(decidedBy));

        Status = ClaimStatus.Rejected;
        Decision = ClaimDecision.Rejected(reason, decidedBy, now);
        _events.Add(new ClaimRejected(Id, ClaimantId, reason, now));
    }

    public void MarkPaid()
    {
        EnsureStatus(ClaimStatus.Approved, ClaimStatus.Paid);
        Status = ClaimStatus.Paid;
    }

    public void MarkPaymentFailed()
    {
        EnsureStatus(ClaimStatus.Approved, ClaimStatus.PaymentFailed);
        Status = ClaimStatus.PaymentFailed;
    }

    public void RetryPayment(DateTime now)
    {
        EnsureStatus(ClaimStatus.PaymentFailed, ClaimStatus.Approved);
        Status = ClaimStatus.Approved;
        _events.Add(new ClaimApproved(Id, ClaimantId, Amount, now));
    }

    public void ClearDomainEvents() => _events.Clear();

    private void EnsureStatus(ClaimStatus required, ClaimStatus target)
    {
        if (Status != required)
            throw new InvalidClaimTransitionException(Status, target);
    }

    private static void RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.", name);
    }
}
