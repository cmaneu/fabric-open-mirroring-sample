using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Microsoft.EntityFrameworkCore;
using OrderManagementCLI.Model;

namespace OrderManagementCLI;

public class OrderService
{
    private readonly OrderDbContext _context;

    public OrderService(OrderDbContext context)
    {
        _context = context;
    }

    public void CreateBulkOrders(int count)
    {
        var faker = new Faker<Order>()
            .RuleFor(o => o.OrderNumber, f => f.Commerce.Random.AlphaNumeric(5))
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .RuleFor(o => o.OrderDate, f => f.Date.Past())
            .RuleFor(o => o.TotalAmount, f => f.Finance.Amount())
            .RuleFor(o => o.LastUpdatedAt, f => DateTime.Now)
            .RuleFor(o => o.IsDeleted, f => false);

        var orders = faker.Generate(count);

        _context.Orders.AddRange(orders);
        _context.SaveChanges();
    }

    public List<Order> ViewOrders()
    {
        return _context.Orders.ToList();
    }

    public Order GetOrderByOrderNumber(string orderNumber)
    {
        return _context.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
    }

    public void UpdateOrder(string orderNumber, string customerName, DateTime orderDate, decimal totalAmount)
    {
        var order = GetOrderByOrderNumber(orderNumber);
        if (order != null)
        {
            order.CustomerName = customerName;
            order.OrderDate = orderDate;
            order.TotalAmount = totalAmount;
            order.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();
        }
    }

    public void DeleteOrder(string orderNumber)
    {
        var order = GetOrderByOrderNumber(orderNumber);
        if (order != null)
        {
            order.IsDeleted = true;
            order.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();
        }
    }
}
