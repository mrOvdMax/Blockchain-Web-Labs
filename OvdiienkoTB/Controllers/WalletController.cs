using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Data;
using OvdiienkoTB.Models;
using OvdiienkoTB.Validation;

namespace OvdiienkoTB.Controllers;
[Route ("api/[controller]")]
[ApiController]
public class WalletController : ControllerBase
{
    private readonly BlockchainDbContext _context;

    public WalletController(BlockchainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Wallet>>> GetWallets()
    {
        return Ok(await _context.Wallets.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Wallet>> GetWallet(int id)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet is null)
            return NotFound();
        
        return Ok(wallet);
    }

    [HttpGet("userwallets/{id}")]
    public async Task<ActionResult<IEnumerable<Wallet>>> GetWalletByUserId(int id)
    {
        var wallets = await _context.Wallets.Where(w => w.UserId == id).ToListAsync();
        if (wallets is null)
            return NotFound();
        return Ok(wallets);
    }

    [HttpPost("{id}")]
    public async Task<ActionResult<Wallet>> PostWallet(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound();
        
        var wallet = new Wallet()
        {
            UserId = id
        };
        
        var validation = ValidateEntities.ValidateWallet(wallet);
        if (validation.Count > 0)
            return BadRequest(new BlockchainException(string.Join(',', validation)));

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return Ok(wallet);
    }

    [HttpPut]
    public async Task<ActionResult<Wallet>> PutWallet([FromBody] Wallet newWallet)
    {
        var validation = ValidateEntities.ValidateWallet(newWallet);
        if (validation.Count > 0)
            return BadRequest(new BlockchainException(string.Join(',', validation)));
        
        _context.Wallets.Update(newWallet);
        await _context.SaveChangesAsync();
        return Ok(newWallet);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Wallet>> DeleteWallet(int id)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if(wallet is null)
            return NotFound();
        
        var user = await _context.Users.FindAsync(wallet.UserId);
        if(user is null)
            return NotFound();
        
        user.WalletIds.Remove(wallet.Id);

        _context.Wallets.Remove(wallet);
        await _context.SaveChangesAsync();
        return Ok();
    }
}