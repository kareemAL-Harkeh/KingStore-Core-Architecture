namespace KingStore.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    
    // المبالغ
    public decimal TotalAmountUsd { get; private set; }
    public decimal ShippingCostSyp { get; private set; } 
    
    // أرقام العمليات
    public string? PaymentTransactionIdUsd { get; private set; } 
    public string? PaymentTransactionIdSyp { get; private set; } 
    
    // الربط مع شركة الشحن
    public Guid? ShippingCompanyId { get; private set; } 
    
    public ShippingCompany ShippingCompany { get; private set; } = null!; 
    public string? ShippingBranch { get; private set; } 

    // تتبع الشحن
    public string? TrackingNumber { get; private set; }
    public DateTime? ShippedDate { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        Id = Guid.NewGuid();
        UserId = userId;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }

    public void AddShoe(Guid shoeId, decimal price, int quantity)
    {
        if (shoeId == Guid.Empty) throw new ArgumentException("Shoe ID is required", nameof(shoeId));
        if (price <= 0) throw new ArgumentException("Price must be positive", nameof(price));
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        _items.Add(new OrderItem(shoeId, price, quantity));
        CalculateTotal();
    }

    private void CalculateTotal()
    {
        TotalAmountUsd = _items.Sum(x => x.Price * x.Quantity);
    }

    public void SubmitDualPayment(
        string transactionIdUsd, 
        string transactionIdSyp, 
        Guid shippingCompanyId,  
        string branchName,       
        decimal confirmedCostSyp) 
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Rejected)
            throw new InvalidOperationException("Payment can only be submitted for pending or rejected orders.");

        if (string.IsNullOrWhiteSpace(transactionIdUsd))
            throw new ArgumentException("USD Transaction ID is required", nameof(transactionIdUsd));

        if (string.IsNullOrWhiteSpace(transactionIdSyp))
            throw new ArgumentException("SYP Transaction ID is required", nameof(transactionIdSyp));

        if (shippingCompanyId == Guid.Empty)
            throw new ArgumentException("Shipping company must be selected", nameof(shippingCompanyId));

        // تثبيت الأسعار والبيانات لمنع التلاعب أو التغير المستقبلي
        PaymentTransactionIdUsd = transactionIdUsd;
        PaymentTransactionIdSyp = transactionIdSyp;
        ShippingCompanyId = shippingCompanyId;
        ShippingBranch = branchName;
        ShippingCostSyp = confirmedCostSyp; 
        
        Status = OrderStatus.AwaitingVerification;
    }

    public void ApprovePayment()
    {
        if (Status != OrderStatus.AwaitingVerification)
            throw new InvalidOperationException("Order must be awaiting verification to be approved.");

        Status = OrderStatus.Paid;
    }

    public void RejectOrder()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot reject a shipped order.");

        Status = OrderStatus.Rejected;
        PaymentTransactionIdUsd = null;
        PaymentTransactionIdSyp = null;
    }

    public void ShipOrder(string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException("Only paid orders can be shipped.");

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number is required to ship an order.", nameof(trackingNumber));

        TrackingNumber = trackingNumber;
        ShippedDate = DateTime.UtcNow;
        Status = OrderStatus.Shipped;
    }
}