namespace Prezentownik.WebApi.Models;

public class GiftClaim
{
    public required Guid Id { get; init; }

    // Item navigation properties
    public Guid ItemId { get; init; }
    public Item Item { get; init; } = null!;

    public required int QuantityClaimed { get; init; }

    public required string? ClaimerName { get; init; }

    public required Guid RevocationToken { get; init; }

    public static GiftClaim CreateNew(int quantityClaimed, string? claimerName)
    {
        return new GiftClaim
        {
            Id = Guid.CreateVersion7(),
            QuantityClaimed = quantityClaimed,
            ClaimerName = claimerName,
            RevocationToken = Guid.CreateVersion7(),
        };
    }
}
