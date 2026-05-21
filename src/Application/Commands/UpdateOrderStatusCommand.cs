using MediatR;
using KingStore.Domain.Enums;

namespace KingStore.Application.Orders.Commands;

public record UpdateStatusCommand(
    Guid OrderId, 
    OrderStatus Status, 
    string? Tracking) : IRequest;