using MediatR;
using KingStore.Application.DTOs;
using KingStore.Application.Orders.Commands;
using KingStore.Domain.Entities;

namespace KingStore.Application.Orders.Handlers;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IShoeRepository _shoeRepo;
    private readonly IUnitOfWork _unitOfWork; 

    public CreateOrderHandler(IOrderRepository orderRepo, IShoeRepository shoeRepo, IUnitOfWork unitOfWork)
    {
        _orderRepo = orderRepo;
        _shoeRepo = shoeRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. التحقق الأولي
        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("Order must contain at least one item.");

        var order = new Order(request.UserId);

        // 2. فحص التوفر وحجز الكميات (جلب البيانات وتعديلها بالذاكرة)
        foreach (var item in request.Items)
        {
            var shoe = await _shoeRepo.GetByIdAsync(item.ShoeId);
            
            if (shoe == null || !shoe.IsAvailable || shoe.Stock < item.Quantity)
                throw new InvalidOperationException($"Shoe '{shoe?.Name ?? "Unknown"}' is unavailable or out of stock.");

            // تعديل الحالة بالذاكرة وتتبعها بواسطة EF Core Change Tracker
            shoe.DecrementStock(item.Quantity);
            
            // إضافة العنصر للأوردر
            order.AddShoe(shoe.Id, shoe.Price, item.Quantity);

            // إعلام الريكوزيتوري بالتعديل (بدون حفظ فوري بالداتا بيز)
            await _shoeRepo.UpdateAsync(shoe);
        }

        // 3. إضافة الطلب للـ Tracker
        await _orderRepo.AddAsync(order);

        // 4.  الـ Atomic Commit (كل شيء ينحفظ بـ Transaction واحدة)
        await _unitOfWork.SaveChangesAsync(ct);

        // 5. إرجاع الـ DTO الكامل بنظافة
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            TotalAmountUsd = order.TotalAmountUsd,
            ShippingCostSyp = order.ShippingCostSyp,
            PaymentTransactionIdUsd = order.PaymentTransactionIdUsd,
            PaymentTransactionIdSyp = order.PaymentTransactionIdSyp,
            ShippingBranch = order.ShippingBranch?.ToString() ?? "N/A",
            ShippingDays = order.ShippingCompany?.ExpectedDays ?? "N/A",
            ShippingGovernorate = order.ShippingCompany?.Governorate ?? "N/A",
            ShippingCompanyName = order.ShippingCompany?.Name ?? "General Shipping",
            TrackingNumber = order.TrackingNumber,
            ShippedDate = order.ShippedDate,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ShoeId = i.ShoeId,
                PriceAtPurchase = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}