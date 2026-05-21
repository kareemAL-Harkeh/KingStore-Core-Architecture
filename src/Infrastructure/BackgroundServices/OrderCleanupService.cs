using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using KingStore.Application.Interfaces; 
using KingStore.Shared.Hubs;

namespace KingStore.Infrastructure.ExternalServices;

public class OrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCleanupService> _logger;

    public OrderCleanupService(IServiceProvider serviceProvider, ILogger<OrderCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Cleanup Background Service is starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Checking for expired orders at: {time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var shoeRepo = scope.ServiceProvider.GetRequiredService<IShoeRepository>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>(); // حقن الـ Unit of Work

                var expirationTime = DateTime.UtcNow.AddMinutes(-15); // مهلة الحجز 15 دقيقة
                var expiredOrders = await orderRepo.GetExpiredPendingOrdersAsync(expirationTime);

                if (!expiredOrders.Any()) continue;

                foreach (var order in expiredOrders)
                {
                    try 
                    {
                        _logger.LogWarning("Cancelling expired order: {Id} due to payment timeout.", order.Id);
                        
                        // 1. إلغاء الطلب وتحديث حالته عبر الـ Domain Boundary
                        order.RejectOrder(); 
                        await orderRepo.UpdateAsync(order);

                        // 2. إرجاع الستوك للأحذية (جلب جماعي أو تتبع عبر الـ Change Tracker لرفع الأداء)
                        var shoeIds = order.Items.Select(i => i.ShoeId).ToList();
                        var shoes = await shoeRepo.GetByIdsAsync(shoeIds);

                        foreach (var item in order.Items)
                        {
                            var shoe = shoes.FirstOrDefault(s => s.Id == item.ShoeId);
                            if (shoe != null) 
                            {
                                shoe.Restock(item.Quantity);
                                await shoeRepo.UpdateAsync(shoe); // تتبع بالذاكرة
                            }
                        }

                        // 3.  الـ Atomic Commit لكل أوردر وأحذيته بضربة واحدة آمنة
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        
                        // 4. إشعار الزبون فوراً عبر السيجنال أر
                        await hubContext.Clients.User(order.UserId.ToString())
                            .SendAsync("ReceiveNotification", new 
                            { 
                                orderId = order.Id,
                                message = "انتهت مهلة الدفع (15 دقيقة)، تم إلغاء حجز الطلب تلقائياً وإعادة المنتجات للمخزن." 
                            }, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, "Error processing atomic cleanup for expired order {Id}", order.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error occurred inside Order Cleanup Worker scope.");
            }
        }

        _logger.LogInformation("Order Cleanup Background Service is stopping.");
    }
}