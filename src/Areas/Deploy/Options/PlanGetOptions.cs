// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Deploy.Options;

public sealed class PlanGetOptions
{
    [JsonPropertyName("workspaceFolder")]
    public string WorkspaceFolder { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;
}
