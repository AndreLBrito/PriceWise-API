using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class Product : BaseEntity
{
    private Product(
        Guid id,
        Guid userId,
        string name,
        string? description,
        string? brand,
        string? category,
        string productUrl,
        string? imageUrl,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Description = description;
        Brand = brand;
        Category = category;
        ProductUrl = productUrl;
        ImageUrl = imageUrl;
        IsActive = isActive;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private Product(
        Guid userId,
        string name,
        string? description,
        string? brand,
        string? category,
        string productUrl,
        string? imageUrl)
    {
        UserId = userId;
        Name = name;
        Description = description;
        Brand = brand;
        Category = category;
        ProductUrl = productUrl;
        ImageUrl = imageUrl;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string? Brand { get; private set; }

    public string? Category { get; private set; }

    public string ProductUrl { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(
        Guid userId,
        string name,
        string? description,
        string? brand,
        string? category,
        string productUrl,
        string? imageUrl)
    {
        return new Product(
            userId,
            name.Trim(),
            Normalize(description),
            Normalize(brand),
            Normalize(category),
            productUrl.Trim(),
            Normalize(imageUrl));
    }

    public static Product Restore(
        Guid id,
        Guid userId,
        string name,
        string? description,
        string? brand,
        string? category,
        string productUrl,
        string? imageUrl,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new Product(
            id,
            userId,
            name,
            description,
            brand,
            category,
            productUrl,
            imageUrl,
            isActive,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Update(
        string name,
        string? description,
        string? brand,
        string? category,
        string productUrl,
        string? imageUrl)
    {
        Name = name.Trim();
        Description = Normalize(description);
        Brand = Normalize(brand);
        Category = Normalize(category);
        ProductUrl = productUrl.Trim();
        ImageUrl = Normalize(imageUrl);
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
