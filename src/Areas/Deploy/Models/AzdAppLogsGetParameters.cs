using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;

public sealed class AzdAppLogsGetParameters
{
    public string WorkspaceFolder { get; set; } = string.Empty;
    public string AzdEnvName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? Limit { get; set; }
}

