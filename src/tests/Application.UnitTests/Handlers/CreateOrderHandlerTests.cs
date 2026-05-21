using Moq;
using Xunit;
using FluentAssertions;
using KingStore.Application.Orders.Commands;
using KingStore.Application.Orders.Handlers;
using KingStore.Application.Common.Interfaces;
using KingStore.Domain.Entities;
using KingStore.Domain.Enums;

namespace KingStore.UnitTests.Application.Orders.Handlers;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IShoeRepository> _shoeRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        // 1. عمل Mocking لكل الخدمات المشبوكة بالهاندلر
        _orderRepoMock = new Mock<IOrderRepository>();
        _shoeRepoMock = new Mock<IShoeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // 2. حقن الموكس جوات الهاندلر (Constructor Injection)
        _handler = new CreateOrderHandler(
            _orderRepoMock.Object, 
            _shoeRepoMock.Object, 
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderAndCommit_WhenItemsAreInStock()
    {
        // Arrange (تجهيز البيانات الوهمية وسلوك الـ Mocks)
        var userId = Guid.NewGuid();
        var shoeId = Guid.NewGuid();
        var fakeShoe = new Shoe("Air Jordan", "Nike", "Desc", 100, "Sneakers", Gender.Men, 27, 42, 10);
        
        var command = new CreateOrderCommand(userId, new List<OrderItemDto> 
        { 
            new OrderItemDto(shoeId, 2)
        });

        _shoeRepoMock.Setup(repo => repo.GetByIdAsync(shoeId))
            .ReturnsAsync(fakeShoe);

        // Act (تنفيذ الهاندلر الفعلي)
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert (التحقق من النتائج والـ Behavior)
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        fakeShoe.Stock.Should().Be(8); // المخزون لازم ينزل لـ 8 أوتوماتيكياً

        _orderRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var shoeId = Guid.NewGuid();
        var lowStockShoe = new Shoe("Air Jordan", "Nike", "Desc", 100, "Sneakers", Gender.Men, 27, 42, 1); // المخزون 1 فقط
        
        var command = new CreateOrderCommand(userId, new List<OrderItemDto> 
        { 
            new OrderItemDto(shoeId, 5) // الزبون عم يطلب 5 قطع!
        });

        _shoeRepoMock.Setup(repo => repo.GetByIdAsync(shoeId))
            .ReturnsAsync(lowStockShoe);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not enough stock*");

        // التأكد من أن السيستم حمى نفسه ولم يقم بأي عملية حفظ
        _orderRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}