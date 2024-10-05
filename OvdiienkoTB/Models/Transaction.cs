namespace OvdiienkoTB.Models;

public class Transaction
{
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public double Amount { get; set; }

    public Transaction(string sender, string recipient, double amount)
    {
        Sender = sender;
        Recipient = recipient;
        Amount = amount;
    }

    public string GetSender_OMO()
    {
        return this.Sender;
    }

    public string GetRecipient_OMO()
    {
        return this.Recipient;
    }

    public double GetAmount_OMO()
    {
        return this.Amount;
    }

    public override string ToString()
    {
        return $"Sender: {GetSender_OMO()}, Recipient: {GetRecipient_OMO()}, Amount: {GetAmount_OMO()}";
    }
}