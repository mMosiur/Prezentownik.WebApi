namespace Prezentownik.WebApi.Models;

public class GiftClaim
{
    public Guid Id { get; init; }

    // Item navigation properties
    public Guid ItemId { get; init; }
    public Item Item { get; init; } = null!;

    public required int QuantityClaimed { get; init; }

    public string? ClaimerId { get; set; }
    public AppUser? Claimer { get; init; }

    public required string? ClaimerName { get; init; }

    public required Guid RevocationToken { get; init; }

    public void AssignToUser(string userId)
    {
        if (ClaimerId is not null && ClaimerId != userId)
        {
            throw new InvalidOperationException("Claim is already assigned to another user.");
        }

        ClaimerId = userId;
    }

    public static GiftClaim CreateNew(int quantityClaimed, string? claimerName, string? claimerId = null)
    {
        return new()
        {
            QuantityClaimed = quantityClaimed,
            ClaimerName = claimerName,
            ClaimerId = claimerId,
            RevocationToken = Guid.CreateVersion7()
        };
    }
}
