// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;

namespace AzureMcp.Areas.Deploy.Commands;

public static class EncodeMermaid
{
    public static string GetEncodedMermaidChart(string graph)
    {
        var data = new MermaidData
        {
            Code = graph,
            Mermaid = new MermaidConfig { Theme = "default" }
        };

        string jsonString = JsonSerializer.Serialize(data, DeployJsonContext.Default.MermaidData);

        byte[] encodedData = Encoding.UTF8.GetBytes(jsonString);

        byte[] compressedGraph = CompressData(encodedData);

        string base64CompressedGraph = Convert.ToBase64String(compressedGraph);

        return base64CompressedGraph;
    }

    private static byte[] CompressData(byte[] data)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var deflateStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                deflateStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
    }
}

