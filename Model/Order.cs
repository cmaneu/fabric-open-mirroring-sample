using System;

namespace OrderManagementCLI.Model;

public class Order
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; }
    public string CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}