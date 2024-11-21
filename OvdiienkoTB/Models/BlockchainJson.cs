using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OvdiienkoTB.Data;
using OvdiienkoTB.Operations;
using OvdiienkoTB.Validation;

namespace OvdiienkoTB.Models;

public class BlockchainJson : IEnumerable<Block>
{
    private readonly List<Transaction> _currentTransactions = [];
    private const int MaxNonce = 102024; 
    private const int StartingNonce = 1510; 
    private const string Surname = "Ovdiienko";
    private decimal _mineReward = 2005;
    private const int MinNodes = 2;
    private const int MaxNodes = 4;
    private static readonly HashSet<string> _nodes = []; 
    private readonly JsonBlockOperations _jsonBlockOperations = new();
    private readonly JsonTransactionOperations _jsonTransactionOperations; 
    private readonly BlockchainDbContext _context;
    
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BlockchainJson(BlockchainDbContext context)
    {
        _context = context;
        _jsonTransactionOperations = new JsonTransactionOperations(); 

        if (_jsonBlockOperations.GetBlockCount() == 0)
            NewGenesisBlock_OMO(Surname, 0);
        
        LoadCurrentTransactions(); 
    }
    
    // Реєстрація нового вузла з перевіркою
    public void RegisterNode(string nodeAddress)
    {
        if (_nodes.Count is < MaxNodes and > MinNodes )
        {
            _nodes.Add(nodeAddress);
        }
        else
        {
            Console.WriteLine("Cannot register more than 4 nodes.");
        }
    }
    
    // Перевірка консенсусу серед всіх вузлів
    public bool ResolveConflicts()
    {
        int maxLength = _jsonBlockOperations.GetBlockCount();
        List<Block> newChain = this.GetBlockchain();

        foreach (var node in _nodes)
        {
            try
            {
                // Виконуємо запит до інших вузлів для отримання їхнього ланцюга
                var response = GetChainFromNode(node); 
                if (response.Length > maxLength && ValidChain(response.Chain))
                {
                    maxLength = response.Length;
                    newChain = response.Chain;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with node {node}: {ex.Message}");
            }
        }

        if (newChain != this.GetBlockchain())
        {
            // Оновлюємо ланцюг
            _jsonBlockOperations.SerializeBlocks(newChain);
            Console.WriteLine("Blockchain updated after consensus resolution.");
            return true;
        }

        Console.WriteLine("No consensus resolution required.");
        return false;
    }

    // Метод для отримання ланцюга з вузла
    private ChainResponse GetChainFromNode(string node)
    {
        try
        {
            // Формуємо URL для запиту
            var url = $"{node}/chain";
        
            // Відправляємо GET запит для отримання ланцюга
            var request = WebRequest.Create(url);
            request.Method = "GET";
        
            // Отримуємо відповідь
            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                // Читаємо відповідь
                var jsonResponse = reader.ReadToEnd();

                // Десеріалізуємо JSON в об'єкт ChainResponse
                var chainResponse = JsonSerializer.Deserialize<ChainResponse>(jsonResponse, _options);

                return chainResponse ?? new ChainResponse(new List<Block>(), 0);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while fetching chain from node {node}: {ex.Message}");
            return new ChainResponse(new List<Block>(), 0); // Повертаємо порожній ланцюг у разі помилки
        }
    }


    // Метод перевірки ланцюга
    public bool ValidChain(List<Block> chain)
    {
        for (int i = 1; i < chain.Count; i++)
        {
            Block lastBlock = chain[i - 1];
            Block currentBlock = chain[i];
            if (!currentBlock.PreviousHash.Equals(Hash_OMO(lastBlock)))
            {
                Console.WriteLine("Hashes don't match.");
                return false;
            }
            if (!IsProofValid(currentBlock));
            {
                Console.WriteLine("Proof is not valid.");
                return false;
            }
        }
        return true;
    }
    
    private bool IsProofValid(Block block)
    {
        var (nonce, finalHash) = ProofOfWork_OMO(block);

        return finalHash.EndsWith("10");
    }


    public Transaction NewCurrencyTransaction_OMO(int senderWalletId, int recipientWalletId, decimal amount)
    {
        Transaction transaction;
        
        if (senderWalletId == 0)
        {
            transaction = new Transaction(senderWalletId, recipientWalletId, amount);
            
        }
        else
        {
            var senderWallet = _context.Wallets.FirstOrDefault(w => w.Id == senderWalletId);
            var recipientWallet = _context.Wallets.FirstOrDefault(w => w.Id == recipientWalletId);

            if (senderWallet is null || recipientWallet is null)
                throw new BlockchainException("Sender or recipient wallet not found.");
        
            if (senderWalletId == recipientWalletId)
                throw new BlockchainException("Recipient wallet id cannot be the same.");

            if (senderWallet.Amount < amount)
                throw new BlockchainException("Insufficient funds.");

        
        
            transaction = new Transaction(senderWalletId, recipientWalletId, amount);
        }
        
       
        //transaction.SignTransaction(senderWallet);
        _currentTransactions.Add(transaction);
        SaveCurrentTransactions(); 
        
        _context.SaveChanges(); 

        return transaction;
    }

    public Block NewGenesisBlock_OMO(string previousHash, int minerId)
    {
        var transactions = _currentTransactions.ToList();
        
        var newBlock = new Block(this._jsonBlockOperations.GetBlockCount(), transactions, previousHash);
        
        var totalFees = _currentTransactions.Sum(t => t.Amount * 0.02m);
        
        AddCoinbaseTransaction_OMO(newBlock, minerId, CalculateCoinbaseTransactionReward_OMO() + totalFees);

        
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
        
        foreach (var transaction in _currentTransactions)
        {
            var recipientWallet = _context.Wallets.FirstOrDefault(w => w.Id == transaction.RecipientId);

            if (transaction.SenderId == 0 && recipientWallet is not null)
            {
                recipientWallet.AdjustBalance(transaction.Amount); 
                continue;
            }
            
            var senderWallet = _context.Wallets.FirstOrDefault(w => w.Id == transaction.SenderId);

            if (senderWallet is null || recipientWallet is null) continue;
            senderWallet.AdjustBalance(-transaction.Amount);
            recipientWallet.AdjustBalance(transaction.Amount * 0.98m);
        }
        
        _context.SaveChanges();
    
        _currentTransactions.Clear();
        SaveCurrentTransactions(); 
        
        _jsonBlockOperations.SerializeBlock(newBlock);

        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
        
        return newBlock;
    }
    
    public static async Task SendTransactionToNodesAsync(Transaction transaction)
    {
        foreach (var node in _nodes)
        {
            using var client = new HttpClient();
            await client.PostAsJsonAsync(node + "/transactions/new", transaction);
        }
    }
    
    // Отримання ланцюга від інших нод
    public async Task<ChainResponse?> GetChainFromNodeAsync(string nodeUrl)
    {
        using var client = new HttpClient();
        try
        {
            var response = await client.GetStringAsync(nodeUrl + "/blockchain");
            return JsonSerializer.Deserialize<ChainResponse>(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while connecting to node {nodeUrl}: {ex.Message}");
            return null;
        }
    }

    public Block NewBlock_OMO(int minerId)
    {
        if (_jsonBlockOperations.GetBlockCount() == 0)
        {
            return NewGenesisBlock_OMO(Surname, minerId);
        }
    
        var transactions = _currentTransactions.ToList();
    
        var newBlock = new Block(this._jsonBlockOperations.GetBlockCount(), transactions, GetLastHash_OMO());
    
        var totalFees = _currentTransactions.Sum(t => t.Amount * 0.02m);
    
        AddCoinbaseTransaction_OMO(newBlock, minerId, CalculateCoinbaseTransactionReward_OMO() + totalFees);
    
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
    
        foreach (var transaction in _currentTransactions)
        {
            var recipientWallet = _context.Wallets.FirstOrDefault(w => w.Id == transaction.RecipientId);

            if (transaction.SenderId == 0 && recipientWallet is not null)
            {
                recipientWallet.AdjustBalance(transaction.Amount); 
                continue;
            }
            
            var senderWallet = _context.Wallets.FirstOrDefault(w => w.Id == transaction.SenderId);

            if (senderWallet is null || recipientWallet is null) continue;
            senderWallet.AdjustBalance(-transaction.Amount);
            recipientWallet.AdjustBalance(transaction.Amount * 0.98m);
        }
    
        _context.SaveChanges();
    
        _currentTransactions.Clear();
        SaveCurrentTransactions(); 

        _jsonBlockOperations.SerializeBlock(newBlock);
    
        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
    
        return newBlock;
    }

    private decimal CalculateCoinbaseTransactionReward_OMO()
    {
        if (_jsonBlockOperations.GetBlockCount() != 0)
        {
            _mineReward = _jsonBlockOperations.GetLastReward();
            return _mineReward = (_jsonBlockOperations.GetBlockCount() % 2 == 0 && _jsonBlockOperations.GetBlockCount() > 0 ? _mineReward / 11 : _mineReward);
        }
    
        return _mineReward;
    }

    public void AddCoinbaseTransaction_OMO(Block block, int recipient, decimal amount)
    {
        block.AddCoinbaseTransaction_OMO(NewCurrencyTransaction_OMO(0, recipient, amount));
    }
    
    public string GetLastHash_OMO()
    {
        return Hash_OMO(_jsonBlockOperations.DeserializeLastBlock());
    }
    
    public Block GetLastBlock_OMO()
    {
        return _jsonBlockOperations.DeserializeLastBlock();
    }

    public List<Block> GetBlockchain()
    {
        return _jsonBlockOperations.DeserializeBlocks();
    }

    public List<Transaction> GetTransactions()
    {
        return _jsonTransactionOperations.DeserializeTransactions();
    }

    private static string Hash_OMO(Block block)
    {
        var hashingInputBuilder = new StringBuilder();
        
        hashingInputBuilder.Append(block.Index)
            .Append(block.Timestamp)
            .Append(block.Nonce)
            .Append(block.PreviousHash);
        
        var hashingInput = hashingInputBuilder.ToString();

        return HashOperations.GetSha256Hash_OMO(hashingInput);
    }
    
    private static string Hash_OMO(int index, long timestamp, int nonce, string previousHash)
    {
        var hashingInputBuilder = new StringBuilder();
        
        hashingInputBuilder.Append(index)
            .Append(timestamp)
            .Append(nonce)
            .Append(previousHash);
        
        var hashingInput = hashingInputBuilder.ToString();

        return HashOperations.GetSha256Hash_OMO(hashingInput);
    }
 
    public (int, string) ProofOfWork_OMO(Block block)
    {
        var isEvenSurname = Surname.Length % 2 == 0;

        var nonce = StartingNonce;
        var counter = 0;
        string finalHash;

        do
        {
            nonce = isEvenSurname ? nonce + 1 : new Random().Next(StartingNonce, MaxNonce);
            finalHash = Hash_OMO(block.GetIndex_OMO(), block.GetTimestamp_OMO(), nonce, block.GetPreviousHash_OMO());  
            counter++;

            if (nonce > MaxNonce)
            {
                throw new ArgumentException("Reached the maximum nonce value");
            }
        }
        while (!finalHash.EndsWith("10")); 

        block.SetNonce_OMO(nonce);  
        
        Console.WriteLine($"{_jsonBlockOperations.GetBlockCount()} PoW iteration count: {counter}");
        return (nonce, finalHash); 
    }

    public IEnumerator<Block> GetEnumerator() => _jsonBlockOperations.DeserializeBlocks().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private void SaveCurrentTransactions()
    {
        _jsonTransactionOperations.SerializeTransactions(_currentTransactions);
    }

    private void LoadCurrentTransactions()
    {
        var loadedTransactions = _jsonTransactionOperations.DeserializeTransactions();
        _currentTransactions.AddRange(loadedTransactions);
    }
}
