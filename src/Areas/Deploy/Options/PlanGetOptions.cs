// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.Deploy.Options;

public sealed class PlanGetOptions
{
    public string WorkspaceFolder { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
}
