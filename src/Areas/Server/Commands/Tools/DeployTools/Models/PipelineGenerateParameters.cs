using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Server.Commands.Tools.Models;

public sealed class PipelineGenerateParameters
{
    public bool UseAZDPipelineConfig { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string GithubEnvironmentName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
}

public static class PipelineGenerateParametersSchema
{
    public static readonly JsonObject Schema = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["useAZDPipelineConfig"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Whether to use azd tool to set up the deployment pipeline. Set to true ONLY if azure.yaml is provided or the context suggests AZD tools."
            },
            ["organizationName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the organization or the user account name of the current Github repository. DO NOT fill this in if you're not sure."
            },
            ["repositoryName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the current Github repository. DO NOT fill this in if you're not sure."
            },
            ["githubEnvironmentName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the environment to which the deployment pipeline will be deployed. DO NOT fill this in if you're not sure."
            },
            ["subscriptionId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The Azure subscription ID where the resources will be deployed. DO NOT fill this in if you're not sure."
            }
        }
    };
}

