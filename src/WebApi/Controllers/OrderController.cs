using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KingStore.Application.Orders.Commands;
using KingStore.Application.Orders.Queries;
using KingStore.Application.DTOs;
using KingStore.Domain.Enums; // سحبنا الـ Enums كرمال تحويل الـ Status

namespace KingStore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // 1. إنشاء طلب جديد (يستخدمه الزبون)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try 
        {
            var command = new CreateOrderCommand(userId.Value, request.Items);
            var result = await _mediator.Send(command);
            
            // استخدمنا CreatedAtAction لتعطي دلالة RESTful صحيحة (201 Created)
            return CreatedAtAction(nameof(GetOrderDetails), new { id = result.Id }, result);
        } 
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // 2. جلب تفاصيل طلب محدد (يستخدمه الزبون والأدمن)
    [Authorize]
    [HttpGet("{id:guid}/details", Name = "GetOrderDetails")]
    public async Task<IActionResult> GetOrderDetails(Guid id)
    {
        try 
        {
            var query = new GetOrderDetailsQuery(id, GetCurrentUserId()!.Value, IsAdmin());
            var result = await _mediator.Send(query);
            return Ok(result);
        } 
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // 3. تقديم بيانات الدفع (يستخدمه الزبون)
    [Authorize]
    [HttpPost("{id:guid}/submit-payment")]
    public async Task<IActionResult> SubmitPayment(Guid id, [FromBody] SubmitPaymentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try 
        {
            var command = new SubmitPaymentCommand(
                id, 
                userId.Value, 
                request.TransactionIdUsd, 
                request.TransactionIdSyp, 
                request.ShippingCompanyId,
                request.ShippingBranch, 
                request.ShippingCostSyp);

            await _mediator.Send(command);
            return Ok(new { Message = "Payment submitted for verification." });
        } 
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // 4. جلب الطلبات المنتظرة للتحقق (للأدمن فقط)
    [Authorize(Roles = "Admin")]
    [HttpGet("pending-verification")]
    public async Task<IActionResult> GetPendingVerification()
    {
        var result = await _mediator.Send(new GetPendingOrdersQuery());
        return Ok(result);
    }

    // 5. جلب كامل تاريخ الطلبات (للأدمن فقط)
    [Authorize(Roles = "Admin")]
    [HttpGet("all-history")]
    public async Task<IActionResult> GetAllOrderHistory()
    {
        var result = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(result);
    }

    // 6. جلب طلباتي الخاصة (للزبوون)
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _mediator.Send(new GetUserOrdersQuery(userId.Value));
        return Ok(result);
    }

    // 7. تحديث حالة الطلب / شحن (للأدمن فقط)
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try 
        {
            // ⚡ هنا ترجمنا الـ string الجاي من الـ Request لـ OrderStatus Enum المتوافق مع الكوماند المعدل
            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var orderStatus))
                return BadRequest($"Invalid status value: {request.Status}");

            var command = new UpdateStatusCommand(id, orderStatus, request.TrackingNumber);
            await _mediator.Send(command);
            return NoContent();
        } 
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // 8. إلغاء الطلب (للزبوون - فقط إذا كان قيد الانتظار)
    [Authorize]
    [HttpDelete("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try 
        {
            await _mediator.Send(new CancelOrderCommand(id, userId.Value));
            return NoContent();
        } 
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // --- Helpers ---
    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}