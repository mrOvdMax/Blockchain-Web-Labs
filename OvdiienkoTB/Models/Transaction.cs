namespace OvdiienkoTB.Models;

public class Transaction
{
    public int? SenderId { get; set; }
    public int RecipientId { get; set; }
    public decimal Amount { get; set; }
    public string? Signature { get; set; }

    public Transaction()
    {
        
    }

    public Transaction(int sender, int recipient, decimal amount)
    {
        SenderId = sender;
        RecipientId = recipient;
        Amount = amount;
    }

    public int GetSenderId_OMO()
    {
        return SenderId.Value;
    }

    public int GetRecipientId_OMO()
    {
        return this.RecipientId;
    }

    public decimal GetAmount_OMO()
    {
        return this.Amount;
    }
    
    public string GetData()
    {
        return $"{SenderId}:{RecipientId}:{Amount}";
    }

    public void SignTransaction(Wallet senderWallet)
    {
        Signature = senderWallet.SignData(GetData());
    }

    public bool VerifySignature(Wallet senderWallet)
    {
        return Wallet.VerifySignature(GetData(), Signature, senderWallet.PublicKey);
    }

    public override string ToString()
    {
        return $"Sender: {GetSenderId_OMO()}, Recipient: {GetRecipientId_OMO()}, Amount: {GetAmount_OMO()}";
    }
}