# Microsoft Fabric Open Mirroring Sample

A comprehensive .NET sample application demonstrating Microsoft Fabric's Open Mirroring feature. This application showcases how to mirror data from a local SQL database to Microsoft Fabric OneLake using incremental Parquet file uploads.

## 🎯 What is Open Mirroring?

Microsoft Fabric Open Mirroring allows you to synchronize data from external systems to Fabric OneLake in real-time or near real-time. Unlike traditional ETL processes, Open Mirroring enables:

- **Incremental data sync**: Only changed/new records are transferred
- **Real-time analytics**: Data becomes available in Fabric immediately after upload
- **Cost-effective**: Reduces data transfer costs by syncing only deltas
- **Flexible formats**: Supports Parquet and other formats optimized for analytics

## 🏗️ Application Architecture

This sample application consists of:

```
OrderManagementApp/
├── Models/
│   ├── Order.cs              # Order entity with change tracking
│   └── MirroringState.cs     # Tracks mirroring state and metadata
├── Services/
│   ├── OrderService.cs       # Business logic for order management
│   ├── MirroringService.cs   # Handles data mirroring to OneLake
│   └── OrderDbContext.cs     # Entity Framework database context
├── Views/                    # Terminal.Gui-based user interface
└── Program.cs               # CLI commands and application entry point
```

### Key Features
- **Local SQLite Database**: Stores orders with change tracking
- **CLI Interface**: Command-line tools for bulk operations and mirroring
- **GUI Interface**: Terminal-based user interface for order management
- **Incremental Sync**: Tracks and syncs only modified records
- **Parquet Export**: Generates optimized Parquet files for analytics
- **OneLake Integration**: Direct upload to Fabric OneLake landing zones

## 🚀 Getting Started

### Prerequisites

- **.NET 9 SDK** ([Download here](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Microsoft Fabric tenant** with:
  - A workspace attached to a Fabric capacity (F, P, or trial)
  - A mirrored database configured in your workspace
- **Access permissions** to the target workspace and mirrored database

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/cmaneu/fabric-open-mirroring-sample.git
   cd fabric-open-mirroring-sample
   ```

2. **Build the application**:
   ```bash
   dotnet build
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

## 📋 Usage

### Command Line Interface

The application provides several CLI commands:

#### 1. Create Sample Data
```bash
# Create 10 sample orders (default)
dotnet run create-bulk-orders

# Create custom number of orders
dotnet run create-bulk-orders --count 1000
```

#### 2. Setup Mirroring Configuration
```bash
dotnet run setup-mirroring \
  --source-table Orders \
  --destination-table Orders \
  --landing-zone-url "https://onelake.dfs.fabric.microsoft.com/{workspaceId}/{mirroredDbId}/Files/LandingZone" \
  --bearer-token "YOUR_BASE64_TOKEN"
```

**Parameters:**
- `--source-table`: Source table name in local database
- `--destination-table`: Target table name in Fabric
- `--landing-zone-url`: OneLake landing zone URL for your mirrored database
- `--bearer-token`: Base64-encoded authentication token (see authentication section)

#### 3. Execute Data Mirroring
```bash
# Mirror all changed data since last sync
dotnet run mirror
```

#### 4. Launch GUI Interface
```bash
# View and manage orders in terminal UI
dotnet run view-orders

# Or launch main application window
dotnet run
```

### Complete Demo Workflow

```bash
# 1. Initialize database with sample data
dotnet run create-bulk-orders --count 100

# 2. Configure mirroring (replace with your values)
dotnet run setup-mirroring \
  --source-table Orders \
  --destination-table Orders \
  --landing-zone-url "https://onelake.dfs.fabric.microsoft.com/{workspaceId}/{mirroredDbId}/Files/LandingZone" \
  --bearer-token "YOUR_TOKEN_HERE"

# 3. Perform initial data sync
dotnet run mirror

# 4. Create additional data and sync again
dotnet run create-bulk-orders --count 500
dotnet run mirror

# 5. Launch GUI to view/edit orders
dotnet run view-orders
```

## 🔐 Authentication

### Getting a Bearer Token

> ⚠️ **Warning**: This method is for demo purposes only! Tokens are valid for approximately 1 hour.

1. **Open a Fabric notebook** in your workspace with access to the mirrored database

2. **Execute this Python code** in a notebook cell:
   ```python
   from notebookutils import mssparkutils
   import base64
   
   # Get token for Azure Storage (OneLake)
   token = mssparkutils.credentials.getToken('https://storage.azure.com/.default')
   
   # Encode token in base64 format
   encoded = base64.b64encode(token.encode())
   print(encoded)
   ```

3. **Copy the token** (without the `b'` prefix and `'` suffix)

4. **Use the token** in the `setup-mirroring` command

### Production Authentication
For production scenarios, consider:
- **Service Principal authentication** with proper credentials management
- **Managed Identity** when running in Azure
- **Key Vault integration** for secure token storage

## 📁 Data Format

### Generated Files

The mirroring process creates:

```
data/
└── parquet/
    └── orders/
        ├── _metadata.json           # Table schema and key columns
        ├── 00000000000000000001.parquet  # Initial data batch
        ├── 00000000000000000002.parquet  # Incremental batch 1
        └── 00000000000000000003.parquet  # Incremental batch 2
```

### Parquet Schema

| Column | Type | Description |
|--------|------|-------------|
| `__rowMarker__` | int | Row operation marker (0=insert/update, 2=delete) |
| `OrderId` | int | Primary key |
| `OrderNumber` | string | Unique order identifier |
| `CustomerName` | string | Customer full name |
| `OrderDate` | datetime | When order was placed |
| `TotalAmount` | decimal | Order total amount |
| `LastUpdatedAt` | datetime | Last modification timestamp |

### Metadata Format

```json
{
  "keyColumns": ["OrderId"]
}
```

## 🔧 Configuration

### Database Configuration
- **SQLite database** stored in `data/orders.db`
- **Automatic schema creation** on first run
- **Soft delete support** with `IsDeleted` flag
- **Change tracking** via `LastUpdatedAt` timestamp

### Mirroring State
The application tracks mirroring state in the `States` table:
- **Last mirrored timestamp**: Ensures incremental sync
- **File numbering**: Maintains sequential Parquet file naming
- **OneLake credentials**: Securely stores connection information

## 🐛 Troubleshooting

### Common Issues

**"Authentication failed"**
- Verify your bearer token is valid and not expired
- Ensure you have proper permissions to the workspace and mirrored database
- Check that the OneLake URL format is correct

**"No data to mirror"**
- Verify orders exist in the local database: `dotnet run view-orders`
- Check if mirroring was already performed (only changed data is synced)
- Create new orders: `dotnet run create-bulk-orders`

**"Parquet file creation failed"**
- Ensure the `data/parquet/orders` directory is writable
- Check available disk space
- Verify .NET 9 runtime is properly installed

**"OneLake upload failed"**
- Confirm the landing zone URL format: `https://onelake.dfs.fabric.microsoft.com/{workspaceId}/{mirroredDbId}/Files/LandingZone`
- Verify workspace ID and mirrored database ID are correct
- Check network connectivity to OneLake

### Debug Mode
Enable detailed logging by setting environment variable:
```bash
$env:DOTNET_ENVIRONMENT="Development"
dotnet run
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Related Resources

- [Microsoft Fabric Documentation](https://docs.microsoft.com/fabric/)
- [OneLake Overview](https://docs.microsoft.com/fabric/onelake/)
- [Mirroring in Microsoft Fabric](https://docs.microsoft.com/fabric/database/mirroring/)
- [Parquet Format Specification](https://parquet.apache.org/)

## 📞 Support

For questions and support:
- Create an [issue](https://github.com/cmaneu/fabric-open-mirroring-sample/issues) in this repository
- Check the [Microsoft Fabric community](https://community.fabric.microsoft.com/)
- Review the [troubleshooting section](#-troubleshooting) above
