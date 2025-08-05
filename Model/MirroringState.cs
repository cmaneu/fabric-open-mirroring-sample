namespace OrderManagementCLI.Model;

public class MirroringState
{
    public string TableName { get; set; }
    public string DestinationTableName {get;set;}
    public DateTime LastMirroredAt { get; set; }
    public long LastFileNumber {get;set;}
    public string? OneLakePath {get;set;}
    public string? OneLakeBearerToken {get;set;}
}