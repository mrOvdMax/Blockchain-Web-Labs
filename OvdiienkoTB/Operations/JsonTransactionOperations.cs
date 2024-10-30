using System.Text.Json;
using System.Text.Json.Serialization;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Operations;

public class JsonTransactionOperations
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonTransactionOperations(string filePath)
    {
        _filePath = filePath;
    }

    public JsonTransactionOperations()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = configBuilder.Build();
        _filePath = configuration.GetSection("Paths").GetValue<string>("JsonTransactionsPath");
    }

    public void SerializeTransactions(List<Transaction> transactions)
    {
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, transactions, _options);
            Console.WriteLine("Transactions have been saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving transactions to file: {ex.Message}");
        }
    }

    public void SerializeTransaction(Transaction newTransaction)
    {
        try
        {
            var transactions = DeserializeTransactions();
            transactions.Add(newTransaction);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, transactions, _options);
            Console.WriteLine("Transaction has been added and saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding transaction: {ex.Message}");
        }
    }

    public List<Transaction> DeserializeTransactions()
    {
        try
        {
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length > 0)
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                return JsonSerializer.Deserialize<List<Transaction>>(fs, _options) ?? new List<Transaction>();
            }

            return new List<Transaction>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing transactions: {ex.Message}");
            return new List<Transaction>(); // Return an empty list in case of error
        }
    }

    public Transaction DeserializeTransactionByIndex(int index)
    {
        var transactions = DeserializeTransactions();

        if (index >= 0 && index < transactions.Count)
        {
            return transactions[index];
        }

        Console.WriteLine("Index out of range.");
        return null;
    }

    public int GetTransactionCount()
    {
        return DeserializeTransactions().Count;
    }

    public void RemoveAllTransactions()
    {
        try
        {
            using (var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
            {
                JsonSerializer.Serialize(fs, new List<Transaction>(), _options);
            }

            Console.WriteLine("All transactions have been removed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing all transactions: {ex.Message}");
        }
    }

    public void RemoveTransactionByIndex(int index)
    {
        var transactions = DeserializeTransactions();

        if (index >= 0 && index < transactions.Count)
        {
            transactions.RemoveAt(index);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, transactions, _options);
            Console.WriteLine($"Transaction at index {index} has been removed.");
        }
        else
        {
            Console.WriteLine("Index out of range. No transaction removed.");
        }
    }

    public void RemoveLastTransaction()
    {
        var transactions = DeserializeTransactions();

        if (transactions.Count > 0)
        {
            transactions.RemoveAt(transactions.Count - 1);

            using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, transactions, _options);
            Console.WriteLine("Last transaction has been removed.");
        }
        else
        {
            Console.WriteLine("No transactions to remove.");
        }
    }
}
