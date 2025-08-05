using Terminal.Gui;
using System.Collections.Generic;
using OrderManagementCLI;

namespace OrderManagementApp.Views
{
    public class OrdersView : Window
    {

        public OrdersView(OrderService orderService)
        {
            Title = "Order Management - View Orders (Press Esc to quit)";
         var orders = orderService.ViewOrders();

            var tableView = new TableView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

        var dataTable = new System.Data.DataTable();
        dataTable.Columns.Add("#", typeof(int));
        dataTable.Columns.Add("OrderNumber", typeof(string));
        dataTable.Columns.Add("CustomerName", typeof(string));
        dataTable.Columns.Add("OrderDate", typeof(DateTime));
        dataTable.Columns.Add("Last updated at", typeof(DateTime));
        var i = 1;
        foreach (var order in orders)
        {
            dataTable.Rows.Add(i++, order.OrderNumber, order.CustomerName, order.OrderDate, order.LastUpdatedAt);
        }
        tableView.Table = new DataTableSource(dataTable);
        Add(tableView);


        }
    }
}