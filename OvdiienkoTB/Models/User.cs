namespace OvdiienkoTB.Models;

public class User
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public List<Wallet> Wallets { get; set; }

    public User(string userId, string email, string passwordHash)
    {
        UserId = userId;
        Email = email;
        PasswordHash = passwordHash;
        Wallets = new List<Wallet>();
    }

    public void AddWallet(Wallet wallet)
    {
        Wallets.Add(wallet);
    }
}