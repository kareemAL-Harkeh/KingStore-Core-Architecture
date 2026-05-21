using Xunit;
using FluentAssertions;
using KingStore.Domain.Entities;
using KingStore.Domain.Enums; 

namespace KingStore.UnitTests.Domain.Entities;

public class ShoeTests
{
    // --- 1. اختبار إنشاء الحذاء والتحقق من البيانات (Constructor & Validation) ---
    
    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WhenDataIsValid()
    {
        // Act
        var shoe = new Shoe("Air Max", "Nike", "Running shoe", 150, "Sports", Gender.Men, 28, 43, 10);

        // Assert
        shoe.Id.Should().NotBeEmpty();
        shoe.IsAvailable.Should().BeTrue();
        
        // 🔥 حل مشكلة الـ Flaky Test باستخدام BeCloseTo مع نسبة سماحية دقيقة
        shoe.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_ShouldThrowArgumentException_WhenPriceIsZeroOrNegative(decimal invalidPrice)
    {
        // Act
        Action act = () => new Shoe("Name", "Brand", "Desc", invalidPrice, "Cat", Gender.Men, 27, 42, 10);

        // Assert & Verify Parameter Name
        act.Should().Throw<ArgumentException>()
           .WithParameterName("price");
    }

    // --- 2. اختبار إدارة المخزون (Inventory Management) ---
    
    [Fact]
    public void DecrementStock_ShouldDecreaseStock_WhenQuantityIsValid()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 10);

        // Act
        shoe.DecrementStock(3);

        // Assert
        shoe.Stock.Should().Be(7);
    }

    [Fact]
    public void DecrementStock_ShouldThrowInvalidOperationException_WhenQuantityExceedsStock()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 5);

        // Act
        Action act = () => shoe.DecrementStock(6);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Not enough stock*");
    }

    // --- 3. اختبار نظام الصور (Image System Logic) ---
    
    [Fact]
    public void AddImage_ShouldSetAsPrimary_WhenItIsTheFirstImageAdded()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 10);

        // Act - نمرر false لنختبر أن الدومين يجبرها تصبح true لأنها الأولى
        shoe.AddImage("url1", "id1", isPrimary: false); 

        // Assert
        shoe.Images.Should().HaveCount(1);
        shoe.Images.First().IsPrimary.Should().BeTrue(); 
    }

    [Fact]
    public void AddImage_ShouldSwitchPrimaryImage_WhenNewNewImageIsSetAsPrimary()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 10);
        shoe.AddImage("url1", "id1", isPrimary: true);

        // Act - إضافة صورة ثانية لتسحب البساط وتكون هي الأساسية
        shoe.AddImage("url2", "id2", isPrimary: true); 

        // Assert
        shoe.Images.First(i => i.PublicId == "id1").IsPrimary.Should().BeFalse();
        shoe.Images.First(i => i.PublicId == "id2").IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemoveImage_ShouldSetNextImageAsPrimary_IfDeletedImageWasThePrimaryOne()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 10);
        shoe.AddImage("url1", "id1", isPrimary: true);
        shoe.AddImage("url2", "id2", isPrimary: false);
        var primaryId = shoe.Images.First(i => i.PublicId == "id1").Id;

        // Act
        shoe.RemoveImage(primaryId);

        // Assert
        shoe.Images.Should().HaveCount(1);
        shoe.Images.First().IsPrimary.Should().BeTrue(); // الصورة المتبقية تمت ترقيتها تلقائياً لأساسية
    }

    [Fact]
    public void RemoveImage_ShouldThrowKeyNotFoundException_WhenImageIdDoesNotExist()
    {
        // Arrange
        var shoe = new Shoe("N", "B", "D", 100, "C", Gender.Men, 27, 42, 10);
        shoe.AddImage("url1", "id1", isPrimary: true);
        var fakeImageId = Guid.NewGuid();

        // Act
        Action act = () => shoe.RemoveImage(fakeImageId);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }
}