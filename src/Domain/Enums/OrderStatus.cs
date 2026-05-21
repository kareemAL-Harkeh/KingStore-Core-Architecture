namespace KingStore.Domain.Entities;

public enum OrderStatus
{
    Pending,
    AwaitingVerification,
    Paid,
    Rejected,
    Shipped,
    Delivered,
    PendingVerification,
    Created
    

}