using OvdiienkoTB.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OvdiienkoTB.Operations;

public class JsonNodeOperations
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonNodeOperations(string filePath)
    {
        _filePath = filePath;
    }

    public JsonNodeOperations()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = configBuilder.Build();
        _filePath = configuration.GetSection("Paths").GetValue<string>("JsonNodesPath");
    }

    public void SerializeNodes(List<Node> nodes)
    {
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, nodes, _options);
            Console.WriteLine("Nodes have been saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving nodes to file: {ex.Message}");
        }
    }

    public List<Node> DeserializeNodes()
    {
        try
        {
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length > 0)
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                return JsonSerializer.Deserialize<List<Node>>(fs, _options) ?? new List<Node>();
            }

            return new List<Node>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing nodes: {ex.Message}");
            return [];
        }
    }

    public void AddNode(Node newNode)
    {
        try
        {
            var nodes = DeserializeNodes();
            if (nodes.Any(n => n.Address == newNode.Address))
            {
                Console.WriteLine("Node already exists.");
                return;
            }

            nodes.Add(newNode);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, nodes, _options);
            Console.WriteLine("Node has been added and saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding node: {ex.Message}");
        }
    }

    public void RemoveNode(string address)
    {
        var nodes = DeserializeNodes();
        var nodeToRemove = nodes.FirstOrDefault(n => n.Address == address);

        if (nodeToRemove != null)
        {
            nodes.Remove(nodeToRemove);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, nodes, _options);
            Console.WriteLine("Node removed successfully.");
        }
        else
        {
            Console.WriteLine("Node not found.");
        }
    }

    public void RemoveAllNodes()
    {
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, new List<Node>(), _options);
            Console.WriteLine("All nodes have been removed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing all nodes: {ex.Message}");
        }
    }
}
