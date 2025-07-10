// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;

public sealed class PlanGetParameters
{
    public string WorkspaceFolder { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
}

public static class PlanGetParametersSchema
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
            ["projectName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the project to generate the deployment plan for. If not provided, will be inferred from the workspace."
            }
        },
        ["required"] = new JsonArray("workspaceFolder", "projectName")
    };
}
