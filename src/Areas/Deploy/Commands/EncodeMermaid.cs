using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AzureMcp.Areas.Deploy.Options;

namespace AzureMcp.Areas.Deploy.Commands;

public static class EncodeMermaid
{
    public static string GetEncodedMermaidChart(string graph)
    {
        // Create the data object structure similar to the TypeScript version
        var data = new MermaidData
        {
            Code = graph,
            Mermaid = new MermaidConfig { Theme = "default" }
        };

        // Serialize to JSON using AOT-safe context
        string jsonString = JsonSerializer.Serialize(data, DeployJsonContext.Default.MermaidData);

        // Encode the JSON string to UTF-8 bytes
        byte[] encodedData = Encoding.UTF8.GetBytes(jsonString);

        // Compress the data using deflate
        byte[] compressedGraph = CompressData(encodedData);

        // Convert the compressed data to base64 string
        string base64CompressedGraph = Convert.ToBase64String(compressedGraph);

        return base64CompressedGraph;
    }

    public static string GetEncodedMermaidChartFromTopology(string workspaceFolder, AppTopology appTopology)
    {
        var mermaidChart = GenerateMermaidChart.GenerateChart(workspaceFolder, appTopology);
        return GetEncodedMermaidChart(mermaidChart);
    }

    private static byte[] CompressData(byte[] data)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
            {
                deflateStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
    }
}

