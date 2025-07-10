using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;

public sealed class PipelineGenerateParameters
{
    public bool UseAZDPipelineConfig { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string GithubEnvironmentName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
}
