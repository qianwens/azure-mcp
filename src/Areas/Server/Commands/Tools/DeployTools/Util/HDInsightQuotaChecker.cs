using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;

public class HDInsightQuotaChecker(string subscriptionId) : AzureQuotaChecker(subscriptionId)
{
    public override async Task<List<QuotaInfo>> GetQuotaForLocationAsync(string location)
    {
        try
        {
            var requestUrl = $"https://management.azure.com/subscriptions/{SubscriptionId}/providers/Microsoft.HDInsight/locations/{location}/usages?api-version=2018-06-01-preview";
            using var rawResponse = await GetQuotaByUrlAsync(requestUrl);

            if (rawResponse?.RootElement.TryGetProperty("value", out var valueElement) != true)
            {
                return [];
            }

            var result = new List<QuotaInfo>();
            foreach (var item in valueElement.EnumerateArray())
            {
                var name = string.Empty;
                var limit = 0.0;
                var used = 0.0;
                var unit = string.Empty;

                if (item.TryGetProperty("name", out var nameElement) && nameElement.TryGetProperty("value", out var nameValue))
                {
                    name = nameValue.GetStringSafe();
                }

                if (item.TryGetProperty("limit", out var limitElement))
                {
                    limit = limitElement.GetDouble();
                }

                if (item.TryGetProperty("currentValue", out var usedElement))
                {
                    used = usedElement.GetDouble();
                }

                if (item.TryGetProperty("unit", out var unitElement))
                {
                    unit = unitElement.GetStringSafe();
                }

                result.Add(new QuotaInfo(name, limit, used, unit));
            }

            return result;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error fetching HDInsight quotas: {error.Message}");
            return [];
        }
    }
}
