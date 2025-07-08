// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Server.Commands;
using AzureMcp.Options;

namespace AzureMcp.Areas.Deploy.Options;

public class RawMcpToolInputOptions : GlobalOptions
{
    [JsonPropertyName(ToolOperations.RawMcpToolInputOptionName)]
    public string? RawMcpToolInput { get; set; }
}