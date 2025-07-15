using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Deploy.Models;

public class AzureRegionCheckParameters
{
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [JsonPropertyName("resourceTypes")]
    public string[] ResourceTypes { get; set; } = [];

    [JsonPropertyName("cognitiveServiceProperties")]
    public CognitiveServiceProperties? CognitiveServiceProperties { get; set; }
}

public class CognitiveServiceProperties
{
    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; } = string.Empty;

    [JsonPropertyName("modelVersion")]
    public string? ModelVersion { get; set; } = string.Empty;

    [JsonPropertyName("deploymentSkuName")]
    public string? DeploymentSkuName { get; set; } = string.Empty;
}

public static class AzureRegionCheckParametersSchema
{
    public static readonly JsonObject Schema = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["subscriptionId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The user's Azure subscription ID (in GUID format). If you don't know which subscription the user is interested in, use the 'azure_auth-get_selected_subscriptions' tool to get the current subscription. Do not make up a value, use a placeholder, or pass null."
            },
            ["resourceTypes"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "The valid Azure resource types. E.g. 'Microsoft.App/containerApps', 'Microsoft.Web/sites', 'Microsoft.CognitiveServices/accounts', etc.",
                ["items"] = new JsonObject
                {
                    ["type"] = "string"
                }
            },
            ["cognitiveServiceProperties"] = new JsonObject
            {
                ["type"] = "object",
                ["description"] = "Optional configuration for model-specific checks. This is only needed when Microsoft.CognitiveServices is included in resourceTypes above",
                ["properties"] = new JsonObject
                {
                    ["modelName"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional. The name of the model. E.g. 'gpt-4o', 'gpt-35-turbo', 'o1-mini', 'text-embedding-3-small'."
                    },
                    ["modelVersion"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional. The version of the model to use when checking model availability. E.g. '2023-05-01'."
                    },
                    ["deploymentSkuName"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional. The SKU name of the model deployment. E.g. 'Standard', 'GlobalStandard', 'Premium', etc.",
                        ["enum"] = new JsonArray
                        (
                            "GlobalStandard",
                            "GlobalProvisionedManaged",
                            "GlobalBatch",
                            "DataZoneStandard",
                            "DataZoneProvisionedManaged",
                            "DataZoneBatch",
                            "ProvisionedManaged",
                            "Standard"
                        )
                    }
                }
            }
        },
        ["required"] = new JsonArray("subscriptionId", "resourceTypes")
    };
}
