// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Areas.Deploy.Models;

namespace AzureMcp.Areas.Deploy.Commands;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AppTopology))]
[JsonSerializable(typeof(MermaidData))]
[JsonSerializable(typeof(MermaidConfig))]
[JsonSerializable(typeof(AzureRegionCheckParameters))]
[JsonSerializable(typeof(CognitiveServiceProperties))]
[JsonSerializable(typeof(AzureQuotaCheckParameters))]
[JsonSerializable(typeof(AzdAppLogsGetParameters))]
[JsonSerializable(typeof(PlanGetParameters))]
[JsonSerializable(typeof(PipelineGenerateParameters))]
[JsonSerializable(typeof(InfraCodeRulesParameters))]
internal sealed partial class DeployJsonContext : JsonSerializerContext
{
}
