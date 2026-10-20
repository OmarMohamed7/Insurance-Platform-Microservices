namespace Claims.Domain;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
