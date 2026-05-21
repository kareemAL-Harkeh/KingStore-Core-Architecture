using MediatR;
using Microsoft.AspNetCore.SignalR;
using KingStore.Application.Orders.Commands;
using KingStore.Application.Common.Interfaces; // حيث تعيش الـ Interfaces النظيفة
using KingStore.Domain.Entities;

namespace KingStore.Application.Orders.Handlers;

public class SubmitPaymentHandler : IRequestHandler<SubmitPaymentCommand>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IUserRepository _userRepo; // استبدلنا UserManager بـ Interface نظيف لحماية المعمارية
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitPaymentHandler(
        IOrderRepository orderRepo, 
        IUserRepository userRepo,
        IHubContext<NotificationHub> hubContext, 
        IUnitOfWork unitOfWork)
    {
        _orderRepo = orderRepo;
        _userRepo = userRepo;
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SubmitPaymentCommand request, CancellationToken ct)
    {
        // 1. جلب الطلب والتأكد من وجوده وصلاحية المستخدم
        var order = await _orderRepo.GetByIdAsync(request.OrderId);
        
        if (order == null) 
            throw new KeyNotFoundException("Order not found.");
            
        if (order.UserId != request.UserId) 
            throw new UnauthorizedAccessException("Unauthorized access to this order.");

        // 2. تحديث بيانات الدفع والشحن عبر الـ Domain Rich Method
        order.SubmitDualPayment(
            request.TxUsd, 
            request.TxSyp, 
            request.ShippingCompanyId, 
            request.Branch,            
            request.CostSyp           
        );
        
        await _orderRepo.UpdateAsync(order);
        
        // حفظ التغييرات بـ Atomic Transaction
        await _unitOfWork.SaveChangesAsync(ct);

        // 3. جلب بيانات الزبون للإشعار عبر الـ Repository المعزول هندسياً
        var user = await _userRepo.GetByIdAsync(request.UserId);
        string customerName = user != null ? $"{user.FirstName} {user.LastName}" : "Customer";

        // 4. إرسال الإشعار الفوري للأدمن عبر SignalR
        await _hubContext.Clients.Group("Admin").SendAsync("ReceivePaymentSubmitted", new {
            orderId = order.Id,
            customerName = customerName,
            txUsd = request.TxUsd,
            txSyp = request.TxSyp,
            branch = request.Branch,
            shippingCost = request.CostSyp, 
            amountUsd = order.TotalAmountUsd,
            message = $"الزبون {customerName} دفع ثمن الطلب واختار شحن {request.Branch} بتكلفة {request.CostSyp:N0} SYP"
        }, ct);
        
    } 
}