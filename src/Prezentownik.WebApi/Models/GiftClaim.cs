namespace Prezentownik.WebApi.Models;

public class GiftClaim
{
    public Guid Id { get; init; }

    // Item navigation properties
    public Guid ItemId { get; init; }
    public Item Item { get; init; } = null!;

    public required int QuantityClaimed { get; init; }

    public string? ClaimantId { get; set; }
    public AppUser? Claimant { get; init; }

    public string? ClaimantName { get; init; }

    public Guid? RevocationToken { get; set; }

    public void AssignToUser(string userId)
    {
        if (ClaimantId is not null && ClaimantId != userId)
        {
            throw new InvalidOperationException("Claim is already assigned to another user.");
        }

        ClaimantId = userId;
        RevocationToken = null;
    }

    public static GiftClaim CreateNewForUnauthenticated(int quantityClaimed, string? claimantName)
    {
        return new()
        {
            QuantityClaimed = quantityClaimed,
            ClaimantName = claimantName,
            RevocationToken = Guid.CreateVersion7()
        };
    }

    public static GiftClaim CreateNewForUser(int quantityClaimed, string? claimantName, string claimantId)
    {
        return new()
        {
            QuantityClaimed = quantityClaimed,
            ClaimantName = claimantName,
            ClaimantId = claimantId,
        };
    }
}
