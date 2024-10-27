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
        ValidateEntities.ValidateUser(user);
        
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("wallet/{id}")]
    public async Task<ActionResult<User>> PutWallet(int id)
    { 
        
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return NotFound();
        
        var wallet = new Wallet(user, 0);
        ValidateEntities.ValidateWallet(wallet);
        
        user.Wallets ??= [];
        user.Wallets.Add(wallet);       
        
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut]
    public async Task<ActionResult<User>> PutUser([FromBody] User newUser)
    {
        _context.Users.Update(newUser);
        await _context.SaveChangesAsync();
        return Ok(newUser);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<User>> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if(user is null)
            throw new BlockchainException();
        _context.Users.Remove(user);
        return Ok();
    }

    /*[HttpDelete("wallet/{id}")]
    public async Task<ActionResult<User>> DeleteWallet(int id)
    {
        
    }*/
}