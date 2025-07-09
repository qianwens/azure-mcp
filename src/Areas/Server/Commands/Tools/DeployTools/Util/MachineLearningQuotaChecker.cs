using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.MachineLearning;
using Azure.ResourceManager.MachineLearning.Models;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public class MachineLearningQuotaChecker(string subscriptionId) : AzureQuotaChecker(subscriptionId)
{
    public override async Task<List<QuotaInfo>> GetQuotaForLocationAsync(string location)
    {
        try
        {
            var subscription = ResourceClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{SubscriptionId}"));
            var usages = subscription.GetMachineLearningUsagesAsync(location);
            var result = new List<QuotaInfo>();

            await foreach (MachineLearningUsage item in usages)
            {
                result.Add(new QuotaInfo(
                    Name: item.Name?.Value ?? string.Empty,
                    Limit: (double)(item.Limit ?? 0),
                    Used: (double)(item.CurrentValue ?? 0),
                    Unit: item.Unit.ToString()
                ));
            }

            return result;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching Machine Learning Services quotas: {error.Message}");
            return [];
        }
    }
}
