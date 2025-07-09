using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using Azure.ResourceManager.CognitiveServices.Models;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public class CognitiveServicesQuotaChecker(string subscriptionId) : AzureQuotaChecker(subscriptionId)
{
    public override async Task<List<QuotaInfo>> GetQuotaForLocationAsync(string location)
    {
        try
        {
            var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
            var usages = subscription.GetUsagesAsync(location);
            var result = new List<QuotaInfo>();

            await foreach (var item in usages)
            {
                result.Add(new QuotaInfo(
                    Name: item.Name?.LocalizedValue ?? item.Name?.Value ?? string.Empty,
                    Limit: (double)(item.Limit ?? 0),
                    Used: (double)(item.CurrentValue ?? 0),
                    Unit: item.Unit.ToString()
                ));
            }

            return result;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching cognitive services quotas: {error.Message}");
            return [];
        }
    }
}
