namespace OvdiienkoTB.Models;

public class Block
{
    public int Index { get; }
    public long Timestamp { get; }
    private List<Transaction> Transactions { get; }
    public int Nonce { get; private set; }
    public string PreviousHash { get; }

    public Block(int index, List<Transaction> transactions, int proof, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow.Ticks;
        Transactions = transactions;
        Nonce = proof;
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

    public int GetIndex_OMO()
    {
        return this.Index;
    }

    public long GetTimestamp_OMO()
    {
        return this.Timestamp;
    }

    public List<Transaction> GetTransactions_OMO()
    {
        return this.Transactions;
    }

    public int GetNonce_OMO()
    {
        return this.Nonce;
    }

    public string GetPreviousHash_OMO()
    {
        return this.PreviousHash;
    }

    public override string ToString()
    {
        return $"Block with index: {GetIndex_OMO()}\nTimestamp: {GetTimestamp_OMO()}, Nonce: {GetNonce_OMO()}, Previous: {GetPreviousHash_OMO()}";
    }
}