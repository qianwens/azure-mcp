using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Server.Commands.Tools.Models;

public sealed class AzureQuotaCheckParameters
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public List<string> ResourceTypes { get; set; } = [];
}

public static class AzureQuotaCheckParametersSchema
{
    public static readonly JsonObject Schema = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["subscriptionId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The Azure subscription ID where the resources will be deployed. Retrieve the subscription ID from the context, or by prompting the user to provide it."
            },
            ["region"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The valid Azure region where the resources will be deployed. E.g. 'eastus', 'westus', 'westeurope', etc."
            },
            ["resourceTypes"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "The valid Azure resource types that are going to be deployed. E.g. 'Microsoft.App/containerApps', 'Microsoft.Web/sites', 'Microsoft.CognitiveServices/accounts', etc.",
                ["items"] = new JsonObject
                {
                    ["type"] = "string"
                }
            }
        },
        ["required"] = new JsonArray("subscriptionId", "region", "resourceTypes")
    };
}
