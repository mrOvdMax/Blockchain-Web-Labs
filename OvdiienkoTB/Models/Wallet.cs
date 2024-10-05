namespace OvdiienkoTB.Models;

public class Wallet
{
    public string PublicKey { get; set; }
    private string EncryptedPrivateKey { get; set; }
    public User User { get; set; }
    
    public Wallet(string publicKey, string encryptedPrivateKey)
    {
        PublicKey = publicKey;
        EncryptedPrivateKey = encryptedPrivateKey;
    }
}