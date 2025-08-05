using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.EntityFrameworkCore;
using OrderManagementApp.Views;
using OrderManagementCLI.Model;
using Spectre.Console;
using Terminal.Gui;
using Command = System.CommandLine.Command;

namespace OrderManagementCLI;

  class Program
    {

        static OrderService orderService;
        static void Main(string[] args)
        {
            // Initialize app context
            var OrderDbContext = new OrderDbContext();
            OrderDbContext.InitializeAndApplySchema();
            orderService = new OrderService(OrderDbContext);

            var createBulkOrdersCommand = new Command("create-bulk-orders", "Create bulk orders with fake data")
            {
                new Option<int>("--count", () => 10, "Number of bulk orders to create")
            };
            createBulkOrdersCommand.Handler = CommandHandler.Create<int>((count) =>
            {
                try
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .Start($"Creating {count} bulk orders...", ctx =>
                        {
                            orderService.CreateBulkOrders(count);
                        });
                    AnsiConsole.MarkupLine($"[green] {count} bulk orders created successfully.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error creating bulk orders: [/]");
                    AnsiConsole.WriteException(ex);
                }
            });

            var setupMirroringCommand = new Command("setup-mirroring", "Setup mirroring")
            {
                new Option<string>("--source-table", "Source table name"),
                new Option<string>("--destination-table", "Destination table name"),
                new Option<string>("--landing-zone-url", "Landing zone URL"),
                new Option<string>("--bearer-token", "Bearer token")
            };

            setupMirroringCommand.Handler = CommandHandler.Create<string, string, string, string>((sourceTable, destinationTable, landingZoneUrl, bearerToken) =>
            {
                try
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .Start($"Initializing mirroring from {sourceTable} to {destinationTable}...", ctx =>
                        {
                            var mirroringService = new MirroringService(OrderDbContext);
                            mirroringService.InitializeMirroring(sourceTable, destinationTable, landingZoneUrl, bearerToken);
                        });
                    AnsiConsole.MarkupLine($"[green]Mirroring initialized successfully.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error initializing mirroring: [/]");
                    AnsiConsole.WriteException(ex);
                }
            });


            var rootCommand = new RootCommand("Order Management app")
            {
               createBulkOrdersCommand,
               setupMirroringCommand,
                new Command("view-orders", "View orders")
                {
                    Handler = CommandHandler.Create(() =>
                    {
                        Application.Init ();
                        Application.Run (new OrdersView(orderService));

                        // Before the application exits, reset Terminal.Gui for clean shutdown
                        Application.Shutdown ();

                    })
                },
                
                new Command("mirror", "Execute mirroring")
                {
                    Handler = CommandHandler.Create(async () =>
                    {
                        await AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .Start("Mirroring...", async ctx =>
                            {
                                var mirroringService = new MirroringService(OrderDbContext);
                                await mirroringService.GenerateParquetFilesAsync();
                            });
                    })
                }
                
                
            };

            rootCommand.SetHandler(runRootCommand);

            rootCommand.InvokeAsync(args).Wait();
        }

    private static void runRootCommand(InvocationContext context)
    {
        Application.Init();

        Application.Run(new MainWindow(orderService));

        Application.Shutdown();
    }
}