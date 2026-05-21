using MediatR;
using Microsoft.AspNetCore.SignalR;
using KingStore.Application.Orders.Commands;
using KingStore.Application.Common.Interfaces;
using KingStore.Domain.Enums;
using KingStore.Domain.Entities;

namespace KingStore.Application.Orders.Handlers;

// أزلنا الـ Unit لتتوافق مع معايير دوت نت 10 و MediatR المودرن
public class UpdateStatusHandler : IRequestHandler<UpdateStatusCommand>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IShoeRepository _shoeRepo;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStatusHandler(
        IOrderRepository orderRepo, 
        IShoeRepository shoeRepo, 
        IHubContext<NotificationHub> hubContext,
        IUnitOfWork unitOfWork) 
    { 
        _orderRepo = orderRepo; 
        _shoeRepo = shoeRepo; 
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateStatusCommand request, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        // التحديث المباشر لأن request.Status أصبحت Enum (Type-Safe)
        var target = request.Status;

        if (target == OrderStatus.Paid) 
        {
            order.ApprovePayment();
        }
        else if (target == OrderStatus.Rejected) 
        {
            order.RejectOrder();

            // ⚡ تحسين الأداء: جلب كل الـ ShoeIds بـ List واحدة لتجنب الـ Loop Queries
            var shoeIds = order.Items.Select(i => i.ShoeId).ToList();
            
            // جلب جماعي للأحذية بـ ضربة واحدة للداتابيز (تفرض وجود ميثود GetByIds في الريبوزيتوري)
            var shoes = await _shoeRepo.GetByIdsAsync(shoeIds); 

            foreach (var item in order.Items) 
            {
                var shoe = shoes.FirstOrDefault(s => s.Id == item.ShoeId);
                if (shoe != null) 
                { 
                    shoe.Restock(item.Quantity); 
                    await _shoeRepo.UpdateAsync(shoe); // EF Change Tracker
                }
            }
        }
        else if (target == OrderStatus.Shipped) 
        {
            if (string.IsNullOrEmpty(request.Tracking)) 
                throw new ArgumentException("Tracking number is required for shipping.", nameof(request.Tracking));
                
            order.ShipOrder(request.Tracking);
        }

        await _orderRepo.UpdateAsync(order);

        // 🌟 حفظ التغييرات للأوردر والأحذية بـ Atomic Transaction واحدة
        await _unitOfWork.SaveChangesAsync(ct);

        // --- نظام الإشعارات (SignalR) ---
        var message = $"تم تحديث حالة طلبك رقم {order.Id.ToString().Substring(0, 6)} إلى: {target}";
        if (target == OrderStatus.Shipped) message += $" - رقم التتبع: {request.Tracking}";

        await _hubContext.Clients.User(order.UserId.ToString())
            .SendAsync("ReceiveNotification", new {
                OrderId = order.Id,
                Status = target.ToString(),
                Message = message,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken: ct);
    }
}