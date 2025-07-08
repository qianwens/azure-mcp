using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using Azure.ResourceManager.CognitiveServices.Models;
using Azure.ResourceManager.Resources;
using AzureMcp.Services.Azure.Authentication;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public record CognitiveServiceProperties(
    string? DeploymentSkuName = null,
    string? ModelVersion = null,
    string? ModelName = null
);

public interface IRegionChecker
{
    Task<List<string>> GetAvailableRegionsAsync(string resourceType);
}

public abstract class AzureRegionChecker : IRegionChecker
{
    protected readonly string SubscriptionId;
    protected readonly ArmClient ResourceClient;
    private static readonly HttpClient HttpClient = new();

    protected AzureRegionChecker(string subscriptionId)
    {
        SubscriptionId = subscriptionId;
        var credential = new CustomChainedCredential();
        ResourceClient = new ArmClient(credential, subscriptionId);
        Console.WriteLine($"AzureRegionChecker initialized for subscription: {subscriptionId}");
    }

    protected async Task<JsonDocument?> GetAzureManagementResultByUrlAsync(string requestApi)
    {
        try
        {
            var credential = new CustomChainedCredential();
            var token = await credential.GetTokenAsync(new TokenRequestContext(["https://management.azure.com/.default"]), CancellationToken.None);
            var requestUrl = $"https://management.azure.com{requestApi}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP error! status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching region status directly: {error.Message}");
            return null;
        }
    }

    public abstract Task<List<string>> GetAvailableRegionsAsync(string resourceType);
}

public class DefaultRegionChecker(string subscriptionId) : AzureRegionChecker(subscriptionId)
{
    public override async Task<List<string>> GetAvailableRegionsAsync(string resourceType)
    {
        try
        {
            var parts = resourceType.Split('/');
            var providerNamespace = parts[0];
            var resourceTypeName = parts[1];

            var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
            var provider = await subscription.GetResourceProviderAsync(providerNamespace);

            if (provider?.Value?.Data?.ResourceTypes == null)
            {
                return [];
            }

            var resourceTypeInfo = provider.Value.Data.ResourceTypes
                .FirstOrDefault(rt => rt.ResourceType.Equals(resourceTypeName, StringComparison.OrdinalIgnoreCase));

            if (resourceTypeInfo?.Locations == null)
            {
                return [];
            }

            return resourceTypeInfo.Locations
                .Select(location => location.Replace(" ", "").ToLowerInvariant())
                .ToList();
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching regions for resource type {resourceType}: {error.Message}");
            return [];
        }
    }
}

public class CognitiveServicesRegionChecker : AzureRegionChecker
{
    private readonly string? _skuName;
    private readonly string? _apiVersion;
    private readonly string? _modelName;

    public CognitiveServicesRegionChecker(string subscriptionId, string? skuName = null, string? apiVersion = null, string? modelName = null)
        : base(subscriptionId)
    {
        _skuName = skuName;
        _apiVersion = apiVersion;
        _modelName = modelName;
    }

    public override async Task<List<string>> GetAvailableRegionsAsync(string resourceType)
    {
        var parts = resourceType.Split('/');
        var providerNamespace = parts[0];
        var resourceTypeName = parts[1];

        var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
        var provider = await subscription.GetResourceProviderAsync(providerNamespace);

        List<string> regions = provider?.Value?.Data?.ResourceTypes?
            .FirstOrDefault(rt => rt.ResourceType.Equals(resourceTypeName, StringComparison.OrdinalIgnoreCase))
            ?.Locations?
            .Select(location => location.Replace(" ", "").ToLowerInvariant())
            .ToList() ?? new List<string>();

        var availableRegions = new List<string>();

        foreach (var region in regions)
        {
            try
            {
                var quotas = subscription.GetModels(region);

                bool hasMatchingModel = false;

                foreach(CognitiveServicesModel modelElement in quotas)
                {
                    var nameMatch = string.IsNullOrEmpty(_modelName) ||
                        (modelElement.Model?.Name == _modelName);

                    var versionMatch = string.IsNullOrEmpty(_apiVersion) ||
                        (modelElement.Model?.Version == _apiVersion);


                    var skuMatch = string.IsNullOrEmpty(_skuName) ||
                        (modelElement.Model?.Skus?.Any(sku => sku.Name == _skuName) ?? false);

                    if (nameMatch && versionMatch && skuMatch)
                    {
                        hasMatchingModel = true;
                        break;
                    }
                }

                if (hasMatchingModel)
                {
                    availableRegions.Add(region);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error checking cognitive services models for region {region}: {error.Message}");
            }
        }

        return availableRegions;
    }
}

public class PostgreSqlRegionChecker(string subscriptionId) : AzureRegionChecker(subscriptionId)
{
    public override async Task<List<string>> GetAvailableRegionsAsync(string resourceType)
    {
        var parts = resourceType.Split('/');
        var providerNamespace = parts[0];
        var resourceTypeName = parts[1];

        var subscription = ResourceClient.GetSubscriptionResource(ResourceClient.GetDefaultSubscription().Id);
        var provider = await subscription.GetResourceProviderAsync(providerNamespace);
        var regions = provider?.Value?.Data?.ResourceTypes?
            .FirstOrDefault(rt => rt.ResourceType.Equals(resourceTypeName, StringComparison.OrdinalIgnoreCase))
            ?.Locations?
            .Select(location => location.Replace(" ", "").ToLowerInvariant())
            .ToList() ?? [];

        var availableRegions = new List<string>();
        const string apiVersion = "2024-11-01-preview";

        foreach (var region in regions)
        {
            try
            {
                var url = $"/subscriptions/{SubscriptionId}/providers/Microsoft.DBforPostgreSQL/locations/{region}/capabilities?api-version={apiVersion}";
                using var rawResponse = await GetAzureManagementResultByUrlAsync(url);

                Console.WriteLine($"Checking region {region} for PostgreSQL capabilities: {rawResponse?.RootElement}");

                if (rawResponse?.RootElement.TryGetProperty("value", out var valueElement) == true &&
                    valueElement.EnumerateArray().Any() &&
                    (!valueElement[0].TryGetProperty("reason", out _)))
                {
                    availableRegions.Add(region);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error checking PostgreSQL capabilities for region {region}: {error.Message}");
            }
        }

        return availableRegions;
    }
}

public static class RegionCheckerFactory
{
    public static IRegionChecker CreateRegionChecker(
        string subscriptionId,
        string resourceType,
        CognitiveServiceProperties? properties = null)
    {
        var provider = resourceType.Split('/')[0].ToLowerInvariant();

        return provider switch
        {
            "microsoft.cognitiveservices" => new CognitiveServicesRegionChecker(
                subscriptionId,
                properties?.DeploymentSkuName,
                properties?.ModelVersion,
                properties?.ModelName),
            "microsoft.dbforpostgresql" => new PostgreSqlRegionChecker(subscriptionId),
            _ => new DefaultRegionChecker(subscriptionId)
        };
    }
}

public static class AzureRegionService
{
    public static async Task<Dictionary<string, List<string>>> GetAvailableRegionsForResourceTypesAsync(
        List<string> resourceTypes,
        string subscriptionId,
        CognitiveServiceProperties? cognitiveServiceProperties = null)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var resourceType in resourceTypes)
        {
            var checker = RegionCheckerFactory.CreateRegionChecker(subscriptionId, resourceType, cognitiveServiceProperties);
            result[resourceType] = await checker.GetAvailableRegionsAsync(resourceType);
        }

        return result;
    }
}
