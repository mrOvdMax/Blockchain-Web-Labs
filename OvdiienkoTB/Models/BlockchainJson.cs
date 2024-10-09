using System.Collections;
using System.Text;
using OvdiienkoTB.Operations;

namespace OvdiienkoTB.Models;

public class BlockchainJson : IEnumerable<Block>
{
    private readonly List<Transaction> _currentTransactions = [];
    private const int MaxNonce = 102024; 
    private const int StartingNonce = 1510; 
    private const string Surname = "Ovdiienko";
    private double _mineReward = 2005;
    private readonly JsonBlockOperations _jsonBlockOperations = new JsonBlockOperations();


    public BlockchainJson()
    {
        if(_jsonBlockOperations.GetBlockCount() == 0)
            NewGenesisBlock_OMO(Surname);
    }

    public int NewTransaction_OMO(string sender, string recipient, double amount)
    {
        this._currentTransactions.Add(new Transaction(sender, recipient, amount));
        return this._jsonBlockOperations.GetBlockCount();
    }
    
    public Block NewGenesisBlock_OMO(string previousHash)
    {
        var transactions = _currentTransactions.ToList();
        
        var newBlock = new Block(this._jsonBlockOperations.GetBlockCount(), transactions, previousHash);
        
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
        
        this._currentTransactions.Clear();
        _jsonBlockOperations.SerializeBlock(newBlock);

        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
        
        return newBlock;
    }
    
    public Block NewBlock_OMO()
    {
        if (_jsonBlockOperations.GetBlockCount() == 0)
        {
            return NewGenesisBlock_OMO(Surname);
        }
        
        var transactions = _currentTransactions.ToList();
        
        var newBlock = new Block(this._jsonBlockOperations.GetBlockCount(), transactions, GetLastHash_OMO());
        
        AddCoinbaseTransaction_OMO(newBlock, "Ovdiienko", CalculateCoinbaseTransactionReward_OMO());
        
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
        
        this._currentTransactions.Clear();
        _jsonBlockOperations.SerializeBlock(newBlock);
        
        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
        
        return newBlock;
    }

    private double CalculateCoinbaseTransactionReward_OMO()
    {
        _mineReward = (_jsonBlockOperations.GetBlockCount() % 2 == 0 && _jsonBlockOperations.GetBlockCount() > 0 ? _mineReward / 11 : _mineReward);
        return _mineReward;
    }

    public void AddCoinbaseTransaction_OMO(Block block, string recipient, double amount)
    {
        block.AddCoinbaseTransaction_OMO(new Transaction("0", recipient, amount));
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
        return _currentTransactions;
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
                throw new ArgumentException("Досягнуто максимального значення Nonce");
            }
        }
        while (!finalHash.EndsWith("10")); 

        block.SetNonce_OMO(nonce);  
        
        Console.WriteLine($"{_jsonBlockOperations.GetBlockCount()} PoW iteration count: {counter}");
        return (nonce, finalHash); 
    }

    public IEnumerator<Block> GetEnumerator() => _jsonBlockOperations.DeserializeBlocks().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}