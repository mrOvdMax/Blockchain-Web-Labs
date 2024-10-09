using System.Security.Cryptography;
using System.Text;

namespace OvdiienkoTB.Operations;

public class HashOperations
{
    public static string GetSha256Hash_OMO(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hashString = new StringBuilder();
        foreach (var b in hashBytes)
        {
            hashString.Append(b.ToString("x2"));
        }

        return hashString.ToString();
    }
}