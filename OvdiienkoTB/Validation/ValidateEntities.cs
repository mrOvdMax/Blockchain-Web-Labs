using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;
using OvdiienkoTB.Models;
using static System.Text.RegularExpressions.Regex;

namespace OvdiienkoTB.Validation;

public static class ValidateEntities
{
    public static Collection<string> ValidateUser(User user)
    {
        if (user is null)
            throw new BlockchainException();
        
        var errors = new Collection<string>();
        
        if (string.IsNullOrEmpty(user.Email))
            errors.Add("Email is required");
        if(!IsMatch(user.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            errors.Add("Email is not valid");
        if (string.IsNullOrEmpty(user.Name))
            errors.Add("Name is required");
        if (string.IsNullOrEmpty(user.Surname))
            errors.Add("Surname is required");
        if (string.IsNullOrEmpty(user.Username))
            errors.Add("Username is required");
        if (user.BirthDate > DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-18), DateTimeKind.Utc) ||
            user.BirthDate <= new DateTime(1900,1, 1, 1, 1, 1, DateTimeKind.Utc))
            errors.Add("Birth Date is invalid");
        
        return errors;
    }
    

    public static Collection<string> ValidateWallet(Wallet wallet)
    {
        if (wallet is null)
            throw new BlockchainException();
        
        var errors = new Collection<string>();
        
        if(string.IsNullOrEmpty(wallet.PublicKey))
            errors.Add("Public key is missing");
        if (wallet.Amount != 0)
            errors.Add("Amount is invalid");
        if(wallet.UserId <= 0)
            errors.Add("UserId is required");
        
        return errors;
    }
}