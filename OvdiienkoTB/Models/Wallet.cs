using System.Security.Cryptography;

namespace OvdiienkoTB.Models;

public class Wallet : BaseEntity
{
    public int UserId { get; set; }
    public string PublicKey { get; set; }
    private string PrivateKey { get; set; }
    public decimal Amount { get; set; }
    public User User { get; set; }

    public Wallet() : base()
    {
        
    }
    
    public Wallet( User user, decimal amount = 0) : base()
    {
        Amount = amount;
        User = user;
        UserId = user.Id;
        
        using var rsa = new RSACryptoServiceProvider(2048);
        rsa.PersistKeyInCsp = false;
        PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
    }
}