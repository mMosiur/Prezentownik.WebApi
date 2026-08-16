using System.Diagnostics.CodeAnalysis;
using Prezentownik.WebApi.Models.Enums;

namespace Prezentownik.WebApi.Models;

public class Item
{
    public Guid Id { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required ItemType Type { get; set; }

    public required int? TargetQuantity { get; set; }

    public List<GiftClaim> Claims { get; init; } = [];

    public int TotalClaimsQuantity => Claims.Sum(c => c.QuantityClaimed);

    public int OrderNumber { get; set; }

    public Guid GiftListId { get; init; }
    public GiftList GiftList { get; init; } = null!;

    // For EF Core
    private Item() { }

    [SetsRequiredMembers]
    private Item(string name, string? description, int orderNumber, ItemType type, int? targetQuantity)
    {
        Name = name;
        Description = description;
        Type = type;
        TargetQuantity = targetQuantity;
        OrderNumber = orderNumber;
    }

    public static Item CreateSingular(string name, string? description, int order)
        => new(name, description, order,
            type: ItemType.Singular,
            targetQuantity: 1);

    public static Item CreateLimited(string name, string? description, int order, int targetQuantity)
        => new(
            name, description, order,
            type: ItemType.Limited,
            targetQuantity: targetQuantity);

    public static Item CreateLimitless(string name, string? description, int order)
        => new(
            name, description, order,
            type: ItemType.Limitless,
            targetQuantity: null);

    public void AddClaim(int quantity, string? claimerName)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity to claim must be greater than zero.");
        }

        var newTotalQuantity = TotalClaimsQuantity + quantity;

        if (TargetQuantity.HasValue && newTotalQuantity > TargetQuantity.Value)
        {
            throw new InvalidOperationException("Total quantity claimed exceeds target quantity.");
        }

        Claims.Add(GiftClaim.CreateNew(quantity, claimerName));
    }

    public void RemoveClaim(Guid revocationToken)
    {
        var claim = Claims.FirstOrDefault(c => c.RevocationToken == revocationToken);
        if (claim is null)
        {
            throw new InvalidOperationException("Claim not found or invalid revocation token.");
        }

        Claims.Remove(claim);
    }

    public static Item CreateFromRequest(string name, string? description, int orderNumber, ItemType type, int? targetQuantity)
    {
        return type switch
        {
            ItemType.Singular => CreateSingular(name, description, orderNumber),
            ItemType.Limited => CreateLimited(name, description, orderNumber, targetQuantity ?? 1),
            ItemType.Limitless => CreateLimitless(name, description, orderNumber),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public void UpdateFromRequest(string name, string? description, int orderNumber, ItemType type, int? targetQuantity)
    {
        Name = name;
        Description = description;
        OrderNumber = orderNumber;
        Type = type;
        TargetQuantity = type switch
        {
            ItemType.Singular => 1,
            ItemType.Limited => targetQuantity ?? 1,
            ItemType.Limitless => null,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
