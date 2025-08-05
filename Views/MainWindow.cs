using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OrderManagementCLI;
using Spectre.Console;
using Terminal.Gui;

namespace OrderManagementApp.Views;

public class MainWindow : Toplevel
{
    readonly StatusBar? _statusBar;
    MenuBar _menubar;
    private View _currentView; 
    private readonly OrderService _orderService;
    private Label _ordersCountLabel;

    public MainWindow(OrderService orderService)
    {
        _orderService = orderService;

        InitializeMenu();



        _statusBar = new()
        {
            Visible = true,
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
            CanFocus = false
        };
        _statusBar.Height = Dim.Auto(DimAutoStyle.Auto, minimumContentDim: Dim.Func(() => _statusBar.Visible ? 1 : 0), maximumContentDim: Dim.Func(() => _statusBar.Visible ? 1 : 0));

        var orders = orderService.ViewOrders();

        _statusBar.Add(new Label() { Text = "Order Management App" });
        _ordersCountLabel = new Label() { Text = $"{orders.Count()} Orders" };
        _statusBar.Add(_ordersCountLabel);
        Add(_statusBar);

        ShowOrdersTableView();

    }

    private void SwitchToView(View newView)
    {
        if (_currentView != null)
        {
            Remove(_currentView);
        }
        _currentView = newView;
        Add(_currentView);
    }

    private void ShowOrdersTableView()
    {
        var tableView = new TableView
        {
            X = 0,
            Y = Pos.Bottom(_menubar),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
            Title = "Orders",
            SuperViewRendersLineCanvas = true
        };

        var dataTable = new System.Data.DataTable();
        dataTable.Columns.Add("#", typeof(int));
        dataTable.Columns.Add("OrderNumber", typeof(string));
        dataTable.Columns.Add("CustomerName", typeof(string));
        dataTable.Columns.Add("OrderDate", typeof(DateTime));
        dataTable.Columns.Add("Last updated at", typeof(DateTime));
        var i = 1;
        var orders = _orderService.ViewOrders();
        foreach (var order in orders)
        {
            dataTable.Rows.Add(i++, order.OrderNumber, order.CustomerName, order.OrderDate, order.LastUpdatedAt);
        }
        tableView.Table = new DataTableSource(dataTable);
        
        SwitchToView(tableView);
        _ordersCountLabel.Text = $"{orders.Count()} Orders";
    }

    private void ShowBulkOrderCreateView()
    {
        var bulkOrderView = new BulkOrderCreateView(_orderService)
        {
            X = 0,
            Y = Pos.Bottom(_menubar),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        // Hello memory leak
        bulkOrderView.OnCancel += () => ShowOrdersTableView();
        SwitchToView(bulkOrderView);
    }

    private void InitializeMenu()
    {
        _menubar = new()
        {
            Menus =
                    [
                        new ("_File", new MenuItem[]
                {
                    new ("_Quit", "Quit Order Management App", () => Application.RequestStop())
                }),
                new ("_Orders", new MenuItem[]
                {
                    new ("_Create Bulk Orders", "Create bulk orders with fake data", () =>
                    {
                        ShowBulkOrderCreateView();
                    }),
                    new ("_Delete an order", "Delete a specific order", () =>
                    {

                    }),
                }),
                new ("_Mirroring", new MenuItem[]{
                    new ("_Initialize", "Initialize mirroring", () =>
                    {

                    }),
                    new ("_Run", "Run mirroring sequence", () =>
                    {

                    }),
                }),
                new ("_Help",new MenuItem []
                {
                    new (
                        "_Documentation",
                        "",
                        () => OpenUrl ("https://cmaneu.github.io/fabric-open-mirroring-sample"),
                        null,
                        null,
                        (KeyCode)Key.F1
                        ),
                    new (
                        "_README",
                        "",
                        () => OpenUrl ("https://github.com/cmaneu/fabric-open-mirroring-sample"),
                        null,
                        null,
                        (KeyCode)Key.F2
                        ),
                    new (
                        "_About...",
                        "About UI Catalog",
                        () => MessageBox.Query (
                                                title: "",
                                                message: GetAboutBoxMessage (),
                                                wrapMessage: false,
                                                buttons: "_Ok"
                                                ),
                        null,
                        null,
                        (KeyCode)Key.A.WithCtrl
                        )
                }
            )
        ]
        };

        Add(_menubar);
    }


    static void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = url,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                })
                {
                    process.Start();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
            throw;
        }
    }


    public static string GetAboutBoxMessage()
    {
        // NOTE: Do not use multiline verbatim strings here.
        // WSL gets all confused.
        StringBuilder msg = new();
        msg.AppendLine("Order Management APp: A comprehensive sample library for");
        msg.AppendLine();

        msg.AppendLine("""

     ______    _          _      __  __ _                     _             
    |  ____|  | |        (_)    |  \/  (_)                   (_)            
    | |__ __ _| |__  _ __ _  ___| \  / |_ _ __ _ __ ___  _ __ _ _ __   __ _ 
    |  __/ _` | '_ \| '__| |/ __| |\/| | | '__| '__/ _ \| '__| | '_ \ / _` |
    | | | (_| | |_) | |  | | (__| |  | | | |  | | | (_) | |  | | | | | (_| |
    |_|  \__,_|_.__/|_|  |_|\___|_|  |_|_|_|  |_|  \___/|_|  |_|_| |_|\__, |
                                                                       __/ |
                                                                      |___/ 

    """);
        msg.AppendLine();
        msg.AppendLine("v0.1");
        msg.AppendLine();
        msg.AppendLine("https://github.com/cmaneu/fabric-open-mirroring-sample");

        return msg.ToString();
    }
}