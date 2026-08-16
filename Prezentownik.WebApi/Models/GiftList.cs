namespace Prezentownik.WebApi.Models;

public class GiftList
{
    public Guid Id { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string OwnerId { get; init; }
    public AppUser? Owner { get; init; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    // Navigation property to contained gift items
    public ICollection<Item> Items { get; } = [];

    private GiftList() {}

    public static GiftList CreateNew(string name, string? description, string ownerId)
    {
        return new()
        {
            Name = name,
            Description = description,
            OwnerId = ownerId,
        };
    }
}
