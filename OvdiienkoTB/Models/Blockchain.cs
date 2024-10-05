using System.Collections;
using System.Text;
using OvdiienkoTB.Operations;

namespace OvdiienkoTB.Models;

public class Blockchain : IEnumerable<Block>
{
    private readonly List<Block> _chain = [];
    private readonly List<Transaction> _currentTransactions = [];
    private const int MaxNonce = 102024; 
    private const int StartingNonce = 1510; 
    private const string Surname = "Ovdiienko";
    private double _mineReward = 2005;


    public Blockchain()
    {
        NewGenesisBlock_OMO(Surname);
    }

    public int NewTransaction_OMO(string sender, string recipient, double amount)
    {
        this._currentTransactions.Add(new Transaction(sender, recipient, amount));
        return this._chain.Count;
    }
    
    public Block NewGenesisBlock_OMO(string previousHash)
    {
        var transactions = _currentTransactions.ToList();
        
        var newBlock = new Block(this._chain.Count, transactions, previousHash);
        
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
        
        this._currentTransactions.Clear();
        this._chain.Add(newBlock);

        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
        
        return newBlock;
    }
    
    public Block NewBlock_OMO()
    {
        var transactions = _currentTransactions.ToList();
        
        var newBlock = new Block(this._chain.Count, transactions, GetLastHash_OMO());
        
        AddCoinbaseTransaction_OMO(newBlock, "Ovdiienko", CalculateCoinbaseTransactionReward_OMO());
        
        var (finalNonce, finalHash) = ProofOfWork_OMO(newBlock);
        
        this._currentTransactions.Clear();
        this._chain.Add(newBlock);

        Console.WriteLine($"New block with index {newBlock.Index} added with Nonce: {finalNonce} and Hash: {finalHash}");
        Console.WriteLine();
        
        return newBlock;
    }

    private double CalculateCoinbaseTransactionReward_OMO()
    {
        _mineReward = (_chain.Count % 2 == 0 && _chain.Count > 0 ? _mineReward / 11 : _mineReward);
        return _mineReward;
    }

    public void AddCoinbaseTransaction_OMO(Block block, string recipient, double amount)
    {
        block.AddCoinbaseTransaction_OMO(new Transaction("0", recipient, amount));
    }
    
    public string GetLastHash_OMO()
    {
        return Hash_OMO(_chain[^1]);
    }
    
    public Block GetLastBlock_OMO()
    {
        return _chain[^1];
    }

    private static string Hash_OMO(Block block)
    {
        StringBuilder hashingInputBuilder = new StringBuilder();
        
        hashingInputBuilder.Append(block.Index)
            .Append(block.Timestamp)
            .Append(block.Nonce)
            .Append(block.PreviousHash);
        
        var hashingInput = hashingInputBuilder.ToString();

        return HashOperation.GetSha256Hash_OMO(hashingInput);
    }
    
    private static string Hash_OMO(int index, long timestamp, int nonce, string previousHash)
    {
        var hashingInputBuilder = new StringBuilder();
        
        hashingInputBuilder.Append(index)
            .Append(timestamp)
            .Append(nonce)
            .Append(previousHash);
        
        var hashingInput = hashingInputBuilder.ToString();

        return HashOperation.GetSha256Hash_OMO(hashingInput);
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
        
        Console.WriteLine($"{_chain.Count} PoW iteration count: {counter}");
        return (nonce, finalHash); 
    }

    public IEnumerator<Block> GetEnumerator() => _chain.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}