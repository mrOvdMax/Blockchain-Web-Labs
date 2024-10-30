using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Data;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly BlockchainDbContext _context;

    public AdminController(BlockchainDbContext context)
    {
        _context = context;
    }

    [HttpPut("wallets/{walletId}/{amount}")]
    public async Task<ActionResult<IEnumerable<Wallet>>> UpdateWallet(int walletId, decimal amount)
    {
        var wallet = await _context.Wallets.FindAsync(walletId);
        if(wallet == null)
            return NotFound();
        wallet.Amount = amount;
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        return Ok(wallet);
    }

    [HttpPost("wallets/{userId}/{amount}")]
    public async Task<ActionResult<Wallet>> CreateWallet(int userId, decimal amount)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return NotFound();
        
        var wallet = new Wallet()
        {
            UserId = user.Id,
            Amount = amount
        };

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return Ok(wallet);
    }

    [HttpPut("users")]
    public async Task<ActionResult<Wallet>> AddUserToWallet([FromBody] User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }
}