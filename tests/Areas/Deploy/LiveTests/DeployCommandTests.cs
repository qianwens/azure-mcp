// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using AzureMcp.Areas.Deploy.Services;
using AzureMcp.Tests.Client;
using AzureMcp.Tests.Client.Helpers;
using ModelContextProtocol.Client;
using Xunit;

namespace AzureMcp.Tests.Areas.Deploy.LiveTests;

[Trait("Area", "Deploy")]
public class DeployCommandTests : CommandTestsBase,
    IClassFixture<LiveTestFixture>
{
    private readonly DeployService _deployService;
    private readonly string _subscriptionId;
    private readonly string _accountName;

    public DeployCommandTests(LiveTestFixture liveTestFixture, ITestOutputHelper output) : base(liveTestFixture, output)
    {
        _deployService = new DeployService();
        _subscriptionId = Settings.SubscriptionId;
        _accountName = Settings.ResourceBaseName;
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_get_plan()
    {
        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-plan-get",
            new()
            {
                { "workspace-folder", "C:/" },
                { "project-name", "django" }
            });
        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_check_azure_regions()
    {
        // arrange
        var parameters = new
        {
            subscriptionId = _subscriptionId,
            resourceTypes = new[] { "Microsoft.Web/sites", "Microsoft.Storage/storageAccounts" }
        };

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-region-check",
            new()
            {
                { "raw-mcp-tool-input", JsonSerializer.Serialize(parameters) }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
        Assert.Contains("eastus", result ?? String.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_check_azure_quota()
    {
        // arrange
        var parameters = new
        {
            subscriptionId = _subscriptionId,
            region = "eastus",
            resourceTypes = new[] { "Microsoft.Web/sites" }
        };

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-quota-check",
            new()
            {
                { "raw-mcp-tool-input", JsonSerializer.Serialize(parameters) }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_get_infrastructure_code_rules()
    {
        // arrange
        var parameters = new
        {
            deploymentTool = "azd",
            iacType = "bicep",
            resourceTypes = new[] { "appservice", "azurestorage" }
        };

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-infra-code-rules-get",
            new()
            {
                { "raw-mcp-tool-input", JsonSerializer.Serialize(parameters) }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
        Assert.Contains("bicep", result ?? String.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_generate_pipeline()
    {
        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-pipeline-generate",
            new()
            {
                { "subscription", _subscriptionId },
                { "use-azd-pipeline-config", true }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_get_azd_app_logs()
    {
        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-azd-app-log-get",
            new()
            {
                { "subscription", _subscriptionId },
                { "workspace-folder", "C:/" },
                { "azd-env-name", "test-env" },
                { "limit", 10 }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_check_regions_with_cognitive_services()
    {
        // arrange
        var parameters = new
        {
            subscriptionId = _subscriptionId,
            resourceTypes = new[] { "Microsoft.CognitiveServices/accounts" },
            cognitiveServiceProperties = new
            {
                modelName = "gpt-4o",
                deploymentSkuName = "Standard"
            }
        };

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-region-check",
            new()
            {
                { "raw-mcp-tool-input", JsonSerializer.Serialize(parameters) }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_get_infrastructure_rules_for_terraform()
    {
        // arrange
        var parameters = new
        {
            deploymentTool = "azcli",
            iacType = "terraform",
            resourceTypes = new[] { "containerapp", "azurecosmosdb" }
        };

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-infra-code-rules-get",
            new()
            {
                { "raw-mcp-tool-input", JsonSerializer.Serialize(parameters) }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
        Assert.Contains("terraform", result ?? String.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_generate_pipeline_with_github_details()
    {
        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-pipeline-generate",
            new()
            {
                { "subscription", _subscriptionId },
                { "use-azd-pipeline-config", false },
                { "organization-name", "test-org" },
                { "repository-name", "test-repo" },
                { "github-environment-name", "production" }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Should_get_azd_app_logs_with_time_range()
    {
        // arrange
        var startTime = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // act
        var result = await CallToolMessageAsync(
            "azmcp-deploy-azd-app-log-get",
            new()
            {
                { "subscription", _subscriptionId },
                { "workspace-folder", "C:/" },
                { "azd-env-name", "test-env" },
                { "start-time", startTime },
                { "end-time", endTime },
                { "limit", 50 }
            });

        // assert
        Assert.NotEmpty(result ?? String.Empty);
    }

    protected async Task<string?> CallToolMessageAsync(string command, Dictionary<string, object?> parameters)
    {
        // Output will be streamed, so if we're not in debug mode, hold the debug output for logging in the failure case
        Action<string> writeOutput = Settings.DebugOutput
            ? s => Output.WriteLine(s)
            : s => FailureOutput.AppendLine(s);

        writeOutput($"request: {JsonSerializer.Serialize(new { command, parameters })}");

        var result = await Client.CallToolAsync(command, parameters);

        var content = McpTestUtilities.GetFirstText(result.Content);
        if (string.IsNullOrWhiteSpace(content))
        {
            Output.WriteLine($"response: {JsonSerializer.Serialize(result)}");
            throw new Exception("No JSON content found in the response.");
        }

        var root = JsonSerializer.Deserialize<JsonElement>(content!);
        if (root.ValueKind != JsonValueKind.Object)
        {
            Output.WriteLine($"response: {JsonSerializer.Serialize(result)}");
            throw new Exception("Invalid JSON response.");
        }

        // Remove the `args` property and log the content
        var trimmed = root.Deserialize<JsonObject>()!;
        trimmed.Remove("args");
        writeOutput($"response content: {trimmed.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}");

        return root.TryGetProperty("message", out var property) ? property.GetString() : null;
    }

}
