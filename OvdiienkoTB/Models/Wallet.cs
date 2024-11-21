using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace OvdiienkoTB.Models;

public class Wallet : BaseEntity
{
    public int UserId { get; set; }
    public string PublicKey { get; set; }
    private string PrivateKey { get; set; }
    public decimal Amount { get; set; }
    
    public Wallet(decimal amount = 0) : base()
    {
        Amount = amount;
        using var rsa = new RSACryptoServiceProvider(2048);
        rsa.PersistKeyInCsp = false;
        PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
    }
    
    public string SignData(string data)
    {
        var privateKeyBytes = Convert.FromBase64String(PrivateKey);
    
        using var rsa = RSA.Create();
    
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        var dataToSign = Encoding.UTF8.GetBytes(data);
        var signatureBytes = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    
        return Convert.ToBase64String(signatureBytes);
    }


    public static bool VerifySignature(string data, string signature, string publicKey)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.ImportFromPem(publicKey);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return rsa.VerifyHash(hash, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public void AdjustBalance(decimal amount) => Amount += amount;
}