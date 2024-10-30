namespace OvdiienkoTB.Models;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    //public string PasswordHash { get; set; }
    public string Password { get; set; }
    public DateTime BirthDate { get; set; }
    
    public ICollection<int> WalletIds { get; set; }
}