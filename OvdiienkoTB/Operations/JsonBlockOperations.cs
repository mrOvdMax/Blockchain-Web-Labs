using System.Text.Json;
using System.Text.Json.Serialization;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Operations;

public class JsonBlockOperations
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonBlockOperations(string filePath)
    {
        _filePath = filePath;
    }

    public JsonBlockOperations()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = configBuilder.Build();
        _filePath = configuration.GetSection("Paths").GetValue<string>("JsonBlockchainPath");
    }

    public void SerializeBlocks(List<Block> blocks)
    {
        using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(fs, blocks, _options);
        Console.WriteLine("Blocks have been saved to file.");
    }

    public void SerializeBlock(Block newBlock)
    {
        var blocks = DeserializeBlocks();
        blocks.Add(newBlock); 

        using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(fs, blocks, _options);
        Console.WriteLine("Block has been added and saved to file.");
    }

    public List<Block> DeserializeBlocks()
    {
        if (File.Exists(_filePath) && new FileInfo(_filePath).Length > 0)
        {
            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            return JsonSerializer.Deserialize<List<Block>>(fs, _options) ?? new List<Block>();
        }

        return new List<Block>(); 
    }
    
    public Block DeserializeBlockByIndex(int index)
    {
        var blocks = DeserializeBlocks();

        if (index >= 0 && index < blocks.Count)
        {
            return blocks[index];
        }

        Console.WriteLine("Index out of range.");
        return null;
    }


    public Block DeserializeLastBlock()
    {
        var blocks = DeserializeBlocks();
        return blocks.Count > 0 ? blocks[^1] : null; 
    }
    
    public int GetBlockCount()
    {
        return DeserializeBlocks().Count;
    }
    
    public void RemoveAllBlocks()
    {
        using (var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize(fs, new List<Block>(), _options); // Write an empty list to clear the file
        }

        Console.WriteLine("All blocks have been removed.");
    }

    public void RemoveBlockByIndex(int index)
    {
        var blocks = DeserializeBlocks();

        if (index >= 0 && index < blocks.Count)
        {
            blocks.RemoveAt(index);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, blocks, _options);
            Console.WriteLine($"Block at index {index} has been removed.");
        }
        else
        {
            Console.WriteLine("Index out of range. No block removed.");
        }
    }

    public void RemoveLastBlock()
    {
        var blocks = DeserializeBlocks();

        if (blocks.Count > 0)
        {
            blocks.RemoveAt(blocks.Count - 1);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, blocks, _options);
            Console.WriteLine("Last block has been removed.");
        }
        else
        {
            Console.WriteLine("No blocks to remove.");
        }
    }
}