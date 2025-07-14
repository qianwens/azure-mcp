// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Areas.Deploy.Services.Util;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Helpers;
using AzureMcp.Options;
using AzureMcp.Services.Azure;
using AzureMcp.Services.Azure.Subscription;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Services;

public class DeployService() : BaseAzureService, IDeployService
{

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public async Task<string> GetAzdResourceLogsAsync(
         string workspaceFolder,
         string azdEnvName,
         string subscriptionId,
         int? limit = null)
    {
        TokenCredential credential = await GetCredential();
        string result = await AzdResourceLogService.GetAzdResourceLogsAsync(
            credential,
            workspaceFolder,
            azdEnvName,
            subscriptionId,
            limit);
        return result;
    }

    public async Task<string> GetAzureQuotaAsync(
        List<string> resourceTypes,
        string subscriptionId,
        string location)
    {
        TokenCredential credential = await GetCredential();
        var quotaByResourceTypes = await AzureQuotaService.GetAzureQuotaAsync(
            credential,
            resourceTypes,
            subscriptionId,
            location
            );

        var toolResult = $"The quota info for the specified resource types in subscription: {subscriptionId} and location: {location} are:\n\n" +
            string.Join("\n", quotaByResourceTypes.Select(kvp =>
            {
                var resourceType = kvp.Key;
                var quotas = kvp.Value;

                if (quotas.Count == 0)
                {
                    return $"- {resourceType}: this resource type has no quota limitation";
                }

                return string.Join("\n", quotas.Select(quota =>
                    $"- {resourceType}: {quota.Name}\n" +
                    $"  - Current: {quota.Used}, Limit: {quota.Limit}{(string.IsNullOrEmpty(quota.Unit) ? "" : $" {quota.Unit}")}"));
            }));
        return toolResult;
    }

    public async Task<string> GetAvailableRegionsForResourceTypesAsync(
        List<string> resourceTypes,
        string subscriptionId,
        CognitiveServiceProperties? cognitiveServiceProperties = null)
    {
        ArmClient armClient = await CreateArmClientAsync();
        var availableRegions = await AzureRegionService.GetAvailableRegionsForResourceTypesAsync(armClient, resourceTypes, subscriptionId, cognitiveServiceProperties);
        var allRegions = availableRegions.Values
            .Where(regions => regions.Count > 0)
            .SelectMany(regions => regions)
            .Distinct()
            .ToList();
        var toolResult = $"If you are deploying an app, you MUST choose a region which exists in the following available region list for all resource types (because regions not listed are not available). DO NOT only choose a common region by yourself! Call the tool `azure_quota-check` to check if the selected region REALLY has enough quota for all resources.\n\n";

        var commonValidRegions = availableRegions.Values
            .Aggregate((current, next) => current.Intersect(next).ToList());
        string regionList = commonValidRegions.Count > 0 ? string.Join(", ", commonValidRegions) : "None";
        toolResult += $"Regions available for all resource types: {regionList}";
        return toolResult;
    }
}
