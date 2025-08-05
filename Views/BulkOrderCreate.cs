using System.ComponentModel;
using OrderManagementCLI;
using Terminal.Gui;

namespace OrderManagementApp.Views
{
    public class BulkOrderCreateView : View
    {
        public event Action OnCancel;
        private readonly TextField quantityField;
        private readonly Button createButton;
        private readonly Button cancelButton;

        private readonly OrderService _orderService;
        public BulkOrderCreateView(OrderService orderService)
        {
            _orderService = orderService;
            Title = "Bulk Order Creation";
            
            var label = new Label()
            {
                Text = "Number of orders to create:",
                X = 1,
                Y = 1
            };

            quantityField = new TextField()
            {
                X = Pos.Right(label) + 1,
                Y = 1,
                Width = 10,
                Text = "10"
            };

            createButton = new Button()
            {
                Text = "Create",
                X = 1,
                Y = 3
            };

            cancelButton = new Button()
            {
                Text = "Cancel",
                X = Pos.Right(createButton) + 2,
                Y = 3
            };

            createButton.Accept += OnCreateClicked;
            cancelButton.Accept += OnCancelClicked;

            Add(label, quantityField, createButton, cancelButton);
            quantityField.SetFocus();
        }

        private void OnCreateClicked(object? sender, HandledEventArgs e)
        {
            if (int.TryParse(quantityField.Text.ToString(), out int quantity))
            {
                _orderService.CreateBulkOrders(quantity);
                MessageBox.Query("Success", $"Creating {quantity} orders done.", "OK");
                OnCancel?.Invoke();
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Please enter a valid number", "OK");
            }
        }

        private void OnCancelClicked(object? sender, HandledEventArgs e)
        {
             OnCancel?.Invoke();
        }
    }
}