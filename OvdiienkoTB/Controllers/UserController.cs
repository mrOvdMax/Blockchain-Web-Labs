using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Data;
using OvdiienkoTB.Models;
using OvdiienkoTB.Validation;

namespace OvdiienkoTB.Controllers;

[Route ("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly BlockchainDbContext _context;

    public UserController(BlockchainDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return Ok(await _context.Users.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            throw new BlockchainException();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> PostUser([FromBody] User user)
    {
        var validation = ValidateEntities.ValidateUser(user);
        if(validation.Count > 0)
            return BadRequest(new BlockchainException(string.Join(',', validation)));
        
        
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("wallet/{id}")]
    public async Task<ActionResult<Wallet>> PostWallet(int id, [FromBody] Wallet walletData)
    {
        var validation = ValidateEntities.ValidateWallet(walletData);
        if (validation.Count > 0)
            return BadRequest(new BlockchainException(string.Join(',', validation)));

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound();

        var wallet = new Wallet 
        {
            UserId = id,
            Amount = walletData.Amount
        };

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return Ok(wallet);
    }

    [HttpPut]
    public async Task<ActionResult<User>> PutUser([FromBody] User newUser)
    {
        var validation = ValidateEntities.ValidateUser(newUser);
        if(validation.Count > 0)
            return BadRequest(new BlockchainException(string.Join(',', validation)));
        
        _context.Users.Update(newUser);
        await _context.SaveChangesAsync();
        return Ok(newUser);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<User>> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if(user is null)
            return NotFound();
        
        _context.Wallets.RemoveRange(_context.Wallets.Where(w => w.UserId == user.Id));
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok();
    }
}