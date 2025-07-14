// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Areas.Deploy.Services.Util;
using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Options;

namespace AzureMcp.Areas.Deploy.Services;

public interface IDeployService
{
    Task<string> GetAzdResourceLogsAsync(
        string workspaceFolder,
        string azdEnvName,
        string subscriptionId,
        int? limit = null);

    Task<string> GetAzureQuotaAsync(
        List<string> resourceTypes,
        string subscriptionId,
        string location);


    Task<string> GetAvailableRegionsForResourceTypesAsync(
        List<string> resourceTypes,
        string subscriptionId,
        CognitiveServiceProperties? cognitiveServiceProperties = null);
}
