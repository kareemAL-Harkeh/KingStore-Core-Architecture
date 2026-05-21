using MediatR;


namespace KingStore.Application.Orders.Commands;

public record SubmitPaymentCommand(
    Guid OrderId, 
    Guid UserId, 
    string TxUsd, 
    string TxSyp, 
    Guid ShippingCompanyId, 
    string Branch, 
    decimal CostSyp) : IRequest;