using MediatR;

namespace KingStore.Application.Orders.Commands;

public record CreateOrderItemRequest(Guid ShoeId, int Quantity);
public record CreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items);
public record CreateOrderCommand(Guid UserId, IReadOnlyCollection<CreateOrderItemRequest> Items) : IRequest<OrderDto>;