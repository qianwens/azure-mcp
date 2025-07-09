using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;
using Azure.Core;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public class ContainerInstanceQuotaChecker(string subscriptionId) : AzureQuotaChecker(subscriptionId)
{
    public override async Task<List<QuotaInfo>> GetQuotaForLocationAsync(string location)
    {
        try
        {
            var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
            var usages = subscription.GetUsagesWithLocationAsync(location);

            var result = new List<QuotaInfo>();
            await foreach (ContainerInstanceUsage item in usages)
            {
                result.Add(new QuotaInfo(
                    Name: item.Name?.LocalizedValue ?? item.Name?.Value ?? string.Empty,
                    Limit: (int)(item.Limit ?? 0),
                    Used: (int)(item.CurrentValue ?? 0),
                    Unit: item.Unit.ToString()
                ));
            }

            return result;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching Container Instance quotas: {error.Message}");
            return [];
        }
    }
}
