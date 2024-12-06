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
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, blocks, _options);
            Console.WriteLine("Blocks have been saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving blocks to file: {ex.Message}");
        }
    }

    public void SerializeBlock(Block newBlock)
    {
        try
        {
            var blocks = DeserializeBlocks();
            blocks.Add(newBlock);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, blocks, _options);
            Console.WriteLine("Block has been added and saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding block: {ex.Message}");
        }
    }

    public List<Block> DeserializeBlocks()
    {
        try
        {
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length > 0)
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                return JsonSerializer.Deserialize<List<Block>>(fs, _options) ?? new List<Block>();
            }

            return new List<Block>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing blocks: {ex.Message}");
            return []; 
        }
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

    public string GetFilePath()
    {
        return _filePath;
    }

    public decimal GetLastReward()
    {
        var lastBlock = DeserializeBlocks().LastOrDefault();
        if (lastBlock is null)
            throw new NullReferenceException();
        if (lastBlock.Transactions.Count <= 0)
            return 0;
        return lastBlock?.Transactions.First().Amount ?? 0;
    }

    public void RemoveAllBlocks()
    {
        try
        {
            using (var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
            {
                JsonSerializer.Serialize(fs, new List<Block>(), _options);
            }

            Console.WriteLine("All blocks have been removed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing all blocks: {ex.Message}");
        }
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