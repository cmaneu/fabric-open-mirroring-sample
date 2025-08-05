using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Storage.Files.DataLake;
using OrderManagementCLI.Model;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using ParquetSharp.RowOriented;

namespace OrderManagementCLI;

public class MirroringService
{
    private readonly OrderDbContext _context;

    public MirroringService(OrderDbContext context)
    {
        _context = context;
    }

    public async Task GenerateParquetFilesAsync()
    {
        var state = _context.States.FirstOrDefault(s => s.TableName == "Orders")
            ?? new MirroringState { TableName = "Orders", LastFileNumber = 0, LastMirroredAt = DateTime.MinValue, OneLakeBearerToken = "", OneLakePath = "" };

        // Get only orders modified since last mirroring
        var orders = _context.Orders
            .Where(o => o.LastUpdatedAt > state.LastMirroredAt)
            .ToList();

        // Skip if no new/updated records
        if (!orders.Any())
        {
            return;
        }

        var fileNumber = state.LastFileNumber + 1;
        var fileName = $"{fileNumber:D20}.parquet";

        var outputDir = Path.Combine("data", "parquet", "orders");
        Directory.CreateDirectory(outputDir);

        // Create metadata file
        var orderEntityType = _context.Model.FindEntityType(typeof(Order));
        var primaryKeys = orderEntityType.FindPrimaryKey().Properties
                                    .Select(p => p.Name)
                                    .ToArray();

        // Create metadata file
        var metadataPath = Path.Combine(outputDir, "_metadata.json");
        if(!File.Exists(metadataPath))
        {
            var metadata = new { keyColumns = primaryKeys };
            var jsonContent = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(metadataPath, jsonContent);

            if(!string.IsNullOrEmpty(state.OneLakePath) && !string.IsNullOrEmpty(state.OneLakeBearerToken))
            {
                // OneLakePath is in the format https://domain.com/workspaceId/mirroredDatabaseId/Files/LandingZone/tableName
                // Extract workspaceId, mirroredDatabaseId and tableName
                var pathParts = state.OneLakePath.Split('/');
                var workspaceId = pathParts[3];
                var mirroredDatabaseId = pathParts[4];
                await WriteFileToOneLake(workspaceId, mirroredDatabaseId, state.DestinationTableName, state.OneLakeBearerToken, metadataPath, "_metadata.json");
                Console.WriteLine("Metadata file saved to OneLake landing zone");
            }
        }

        var outputPath = Path.Combine(outputDir, fileName);

        // await WriteParquetFileLegacy(orders, outputPath);
        await WriteParquetFileWithParquetSharp(orders, outputPath);
        Console.WriteLine("File saved at " + outputPath);

        if(!string.IsNullOrEmpty(state.OneLakePath) && !string.IsNullOrEmpty(state.OneLakeBearerToken))
        {
            // OneLakePath is in the format https://domain.com/workspaceId/mirroredDatabaseId/Files/LandingZone/tableName
            // Extract workspaceId, mirroredDatabaseId and tableName
            var pathParts = state.OneLakePath.Split('/');
            var workspaceId = pathParts[3];
            var mirroredDatabaseId = pathParts[4];
            await WriteFileToOneLake(workspaceId, mirroredDatabaseId, state.DestinationTableName, state.OneLakeBearerToken, outputPath, fileName);
        }


        state.LastFileNumber = fileNumber;
        state.LastMirroredAt = DateTime.Now;

        if (_context.States.Any(s => s.TableName == "Orders"))
        {
            _context.States.Update(state);
        }
        else
        {
            _context.States.Add(state);
        }

        await _context.SaveChangesAsync();

        Console.WriteLine("Mirroring done at " + state.LastMirroredAt);
        Console.WriteLine("Total records: " + orders.Count);

        static async Task WriteParquetFileLegacy(List<Order> orders, string outputPath)
        {
            // Create schema first
            var schema = new ParquetSchema(
                new DataField<int>("__rowMarker__"),
                new DataField<int>("OrderId"),
                new DataField<string>("OrderNumber"),
                new DataField<string>("CustomerName"),
                new DataField<DateTime>("OrderDate"),
                new DecimalDataField("TotalAmount", precision: 29, scale: 0, forceByteArrayEncoding: true)
            );

            // Then create columns using schema fields
            var columns = new List<DataColumn>
        {
            new DataColumn(schema.DataFields[0], orders.Select(o => o.IsDeleted ? 2 : 0).ToArray()),
            new DataColumn(schema.DataFields[1], orders.Select(o => o.OrderId).ToArray()),
            new DataColumn(schema.DataFields[2], orders.Select(o => o.OrderNumber).ToArray()),
            new DataColumn(schema.DataFields[3], orders.Select(o => o.CustomerName).ToArray()),
            new DataColumn(schema.DataFields[4], orders.Select(o => o.OrderDate).ToArray()),
            new DataColumn(schema.DataFields[5], orders.Select(o => o.TotalAmount).ToArray())
        };

            using (var fileStream = File.Create(outputPath))
            {
                using (var parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream))
                {
                    parquetWriter.CompressionMethod = CompressionMethod.None;
                    // parquetWriter.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;

                    using (var groupWriter = parquetWriter.CreateRowGroup())
                    {
                        foreach (var column in columns)
                        {
                            await groupWriter.WriteColumnAsync(column);
                        }
                    }
                }
            }
        }

        static async Task WriteParquetFileWithParquetSharp(List<Order> orders, string outputPath)
        {

            // Create an array containing all orders, with the first column being an extra column with a value of 0
            var values = orders.Select(o => new object[]
            {
                0,
                o.OrderId,
                o.OrderNumber,
                o.CustomerName,
                o.OrderDate,
                o.TotalAmount,
                o.LastUpdatedAt
            }).ToArray();

            using var decimalType = ParquetSharp.LogicalType.Decimal(precision: 29, scale: 0);
            // use ParquetSharp library to write parquet file
            var columns = new ParquetSharp.Column[]
            {
                new ParquetSharp.Column<int>("__rowMarker__"),
                new ParquetSharp.Column<int>("OrderId"),
                new ParquetSharp.Column<string>("OrderNumber"),
                new ParquetSharp.Column<string>("CustomerName"),
                new ParquetSharp.Column<DateTime>("OrderDate"),
                new ParquetSharp.Column<decimal>("TotalAmount",decimalType),
                new ParquetSharp.Column<DateTime>("LastUpdatedAt")
            };


            using var rowWriter = ParquetFile.CreateRowWriter<(int, int, string, string, DateTime, decimal, DateTime)>(outputPath, columns);

            foreach (var order in orders)
            {
                rowWriter.WriteRow((0, order.OrderId, order.OrderNumber, order.CustomerName, order.OrderDate, order.TotalAmount, order.LastUpdatedAt));
            }

            rowWriter.Close();
        }

    }
    
    public async Task WriteFileToOneLake(string workspaceId, string mirroredDatabaseId, string tableName, string oneLakeBearerToken, string localFile, string targetFileName)
    {
        var serviceClient = new DataLakeServiceClient(
            new Uri("https://onelake.dfs.fabric.microsoft.com"),
            new StaticTokenCredential(oneLakeBearerToken)
        );

        
        var fileSystemClient = serviceClient.GetFileSystemClient(workspaceId);
        
        var directoryClient = fileSystemClient.GetDirectoryClient($"{mirroredDatabaseId}/Files/LandingZone/{tableName}");
        await directoryClient.CreateIfNotExistsAsync();

        var metadataFile = fileSystemClient.GetFileClient($"{mirroredDatabaseId}/Files/LandingZone/{tableName}/{targetFileName}");
        await metadataFile.UploadAsync(localFile,overwrite: true); 
    }

    internal void InitializeMirroring(string sourceTable, string destinationTable, string landingZoneUrl, string bearerToken)
    {
        // Create a state with all these infos

        // bearerToken is base64encoded, but we need to store it as is
        var decodedBearerToken = Encoding.UTF8.GetString(Convert.FromBase64String(bearerToken));

        var state = new MirroringState()
        {
            TableName = sourceTable,
            DestinationTableName = destinationTable,
            LastMirroredAt = DateTime.MinValue,
            LastFileNumber = 0,
            OneLakePath = landingZoneUrl+"/"+destinationTable,
            OneLakeBearerToken = decodedBearerToken
        };

        if (_context.States.Any(s => s.TableName == sourceTable))
        {
            _context.States.Update(state);
        }
        else
        {
            _context.States.Add(state);
        }
        _context.SaveChanges();
    }

    public class StaticTokenCredential : TokenCredential
    {
        private readonly string _token;

        public StaticTokenCredential(string token)
        {
            _token = token;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_token, DateTimeOffset.Now.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }


}