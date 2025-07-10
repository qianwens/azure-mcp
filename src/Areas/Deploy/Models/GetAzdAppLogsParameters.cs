using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;

public sealed class GetAzdAppLogsParameters
{
    public string WorkspaceFolder { get; set; } = string.Empty;
    public string AzdEnvName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? Limit { get; set; }
}

public static class GetAzdAppLogsParametersSchema
{
    public static readonly JsonObject Schema = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["workspaceFolder"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The full path of the workspace folder."
            },
            ["azdEnvName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the environment created by azd (AZURE_ENV_NAME) during `azd init` or `azd up`. If not provided in context, try to find it in the .azure directory in the workspace or use 'azd env list'."
            },
            ["subscriptionId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The Azure subscription to which azd deploys (AZURE_SUBSCRIPTION_ID) which usually provided during `azd up`. If not provided in context, try to find it in the .azure/**/env file."
            },
            ["startTime"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "date",
                ["description"] = "The start time from which this tool will retrieve the logs. Use this when the logs of a specific time range needs checking. For example, older logs or only recent logs are required."
            },
            ["endTime"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "date",
                ["description"] = "The end time to which this tool will retrieve the logs. Use this when the logs of a specific time range needs checking. For example, older logs or only recent logs are required."
            },
            ["limit"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "The maximum row number of logs to retrieve. Use this to get a specific number of logs or to avoid the retrieved logs from reaching token limit. Default is 200."
            }
        },
        ["required"] = new JsonArray
        (
            "workspaceFolder",
            "azdEnvName",
            "subscriptionId"
        )
    };

}
