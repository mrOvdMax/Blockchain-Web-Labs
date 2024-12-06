using System.Text;
using System.Text.Json.Serialization;
using OvdiienkoTB.Operations;

namespace OvdiienkoTB.Models;

public class Block
{
    public int Index { get; }
    public long Timestamp { get; }
    
    [JsonInclude] 
    public List<Transaction> Transactions { get; private set; } 
    
    public int Nonce { get; private set; }
    public string PreviousHash { get; }

    [JsonConstructor]
    public Block(int index, long timestamp, List<Transaction> transactions, int nonce, string previousHash)
    {
        Index = index;
        Timestamp = timestamp;
        Transactions = transactions;
        Nonce = nonce;
        PreviousHash = previousHash;
    }

    public Block(int index, List<Transaction> transactions, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow.Ticks;
        Transactions = transactions;
        PreviousHash = previousHash;
    }

    public void SetNonce_OMO(int nonce)
    {
        Nonce = nonce;
    }

    public int GetIndex_OMO() => this.Index;
    public long GetTimestamp_OMO() => this.Timestamp;
    public List<Transaction> GetTransactions_OMO() => this.Transactions;
    public int GetNonce_OMO() => this.Nonce;
    public string GetPreviousHash_OMO() => this.PreviousHash;

    public string GetHash_OMO()
    {
        var hashingInputBuilder = new StringBuilder();

        hashingInputBuilder.Append(Index)
            .Append(Timestamp)
            .Append(Nonce)
            .Append(PreviousHash);

        var hashingInput = hashingInputBuilder.ToString();

        return HashOperations.GetSha256Hash_OMO(hashingInput);
    }

    public void AddCoinbaseTransaction_OMO(Transaction transaction)
    {
        this.Transactions.Insert(0, transaction);
    }

    public override string ToString()
    {
        return $"Block with index: {GetIndex_OMO()}\nTimestamp: {GetTimestamp_OMO()}, Nonce: {GetNonce_OMO()}, Previous: {GetPreviousHash_OMO()}";
    }
}