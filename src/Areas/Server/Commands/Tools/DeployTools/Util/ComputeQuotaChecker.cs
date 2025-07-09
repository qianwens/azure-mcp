using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public class ComputeQuotaChecker(string subscriptionId) : AzureQuotaChecker(subscriptionId)
{
    public override async Task<List<QuotaInfo>> GetQuotaForLocationAsync(string location)
    {
        try
        {
            var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
            var usages = subscription.GetUsagesAsync(location);
            var result = new List<QuotaInfo>();

            await foreach (ComputeUsage item in usages)
            {
                result.Add(new QuotaInfo(
                    Name: item.Name?.LocalizedValue ?? item.Name?.Value ?? string.Empty,
                    Limit: (double)item.Limit,
                    Used: (double)item.CurrentValue,
                    Unit: item.Unit.ToString().ToString()
                ));
            }

            return result;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching compute quotas: {error.Message}");
            return [];
        }
    }
}
