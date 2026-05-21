namespace KingStore.Domain.Entities;

public class Shoe
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public decimal Price { get; private set; }
    public string Category { get; private set; } = null!;
    public Gender Gender { get; private set; }
    public decimal FootLength { get; private set; }
    public decimal Size { get; private set; }
    public int Stock { get; private set; }
    public bool IsAvailable => Stock > 0;
    public DateTime CreatedAt { get; private set; }

    // --- نظام الصور ---
    // قائمة الصور الخاصة بهذا الحذاء
    private readonly List<ShoeImage> _images = new();
    public IReadOnlyCollection<ShoeImage> Images => _images.AsReadOnly();

    private Shoe() { }

    public Shoe(string name, string brand, string description, decimal price, string category, Gender gender, decimal footLength, decimal size, int stock = 1)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;

        ValidateDetails(name, brand, description, price, category, footLength, size, stock);

        Name = name;
        Brand = brand;
        Description = description;
        Price = price;
        Category = category;
        Gender = gender;
        FootLength = footLength;
        Size = size;
        Stock = stock;
    }

    public void UpdateDetails(string name, string brand, string description, decimal price, string category, Gender gender, decimal footLength, decimal size, int stock)
    {
        ValidateDetails(name, brand, description, price, category, footLength, size, stock);

        Name = name;
        Brand = brand;
        Description = description;
        Price = price;
        Category = category;
        Gender = gender;
        FootLength = footLength;
        Size = size;
        Stock = stock;
    }

    // ميثود إضافة صورة
    public void AddImage(string url, string publicId, bool isPrimary = false)
    {
        if (isPrimary)
        {
            foreach (var img in _images)
            {
                // إذا كانت أي صورة تانية برايمري، منلغيها
                typeof(ShoeImage).GetProperty(nameof(ShoeImage.IsPrimary))?.SetValue(img, false);
            }
        }
        else if (!_images.Any())
        {
            isPrimary = true;
        }

        _images.Add(new ShoeImage(url, publicId, isPrimary, this.Id));
    }

    // ميثود حذف صورة
    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return;

        _images.Remove(image);

        if (image.IsPrimary && _images.Any())
        {
            // منخلي أول صورة موجودة تصير هي البرايمري
            typeof(ShoeImage).GetProperty(nameof(ShoeImage.IsPrimary))?.SetValue(_images.First(), true);
        }
    }

    public void DecrementStock(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (quantity > Stock) throw new InvalidOperationException("Not enough stock available.");

        Stock -= quantity;
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        Stock += quantity;
    }

    private void ValidateDetails(string name, string brand, string description, decimal price, string category, decimal footLength, decimal size, int stock)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentNullException(nameof(brand));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));

        if (price <= 0) throw new ArgumentException("Price must be positive", nameof(price));
        if (footLength <= 0) throw new ArgumentException("Foot length must be positive", nameof(footLength));
        if (size <= 0) throw new ArgumentException("Size must be positive", nameof(size));
        if (stock < 0) throw new ArgumentException("Stock cannot be negative", nameof(stock));
    }
}