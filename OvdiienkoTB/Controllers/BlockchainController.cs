using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Data;
using OvdiienkoTB.Models;
using OvdiienkoTB.Operations;

namespace OvdiienkoTB.Controllers;

[ApiController]
[Route("api/blockchain")]
public class BlockchainController : ControllerBase
{
    private readonly BlockchainJson _blockchain;
    private readonly BlockchainDbContext _context;
    private readonly JsonBlockOperations _jsonBlockOperations = new();
    private static List<string>? _nodes = [];

    public BlockchainController(BlockchainJson blockchain, BlockchainDbContext context, IConfiguration configuration)
    {
        _blockchain = blockchain;
        _context = context;
        _nodes = configuration.GetSection("BlockchainSettings:Nodes").Get<List<string>>();
    }
    
    // Add a new node to the network
    [HttpPost("nodes/register")]
    public IActionResult RegisterNodes([FromBody] NodesResponse nodesResponse)
    {
        if (nodesResponse?.Nodes == null || !nodesResponse.Nodes.Any())
        {
            return BadRequest("Invalid nodes data.");
        }

        foreach (var node in nodesResponse.Nodes)
        {
            _nodes.Add(node);
        }

        return Ok(new { message = "Nodes registered successfully", nodes = _nodes });
    }
    
    // Get the list of registered nodes
    [HttpGet("nodes")]
    public IActionResult GetNodes()
    {
        return Ok(new { nodes = _nodes });
    }

    // Resolve conflicts with other nodes' chains
    [HttpGet("nodes/resolve")]
    public async Task<IActionResult> ResolveConflicts()
    {
        bool isChainUpdated = _blockchain.ResolveConflicts(/*_nodes*/);

        if (isChainUpdated)
        {
            return Ok(new ChainResponse(_blockchain.GetBlockchain(), _blockchain.GetBlockchain().Count));
        }

        return Ok(new ChainResponse(_blockchain.GetBlockchain(), _blockchain.GetBlockchain().Count));
    }

    [HttpPost("transactions/new/{senderId}/{recipientId}/{amount}")]
    public async Task<ActionResult<Transaction>> NewTransaction(int senderId, int recipientId, int amount)
    {
        if (amount <= 0)
            return BadRequest("Transaction amount must be greater than zero");

        var index = _blockchain.NewCurrencyTransaction_OMO(senderId, recipientId, amount);
        
        await BlockchainJson.SendTransactionToNodesAsync(new Transaction(senderId, recipientId, amount)); 

        await _context.SaveChangesAsync();
        return Ok();
    }
    
    [HttpPost("transactions/check")]
    public async Task<ActionResult<bool>> CheckTransaction([FromBody] Transaction? transaction)
    {
        if (transaction is null)
            return BadRequest("Invalid transaction data");

        var senderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == transaction.SenderId);
        if (senderWallet is null)
            return NotFound("Sender wallet not found.");

        var isValid = Wallet.VerifySignature(transaction.GetData(), transaction.Signature, senderWallet.PublicKey);

        return Ok(isValid);
    }

    [HttpGet("transactions/mempool")]
    public IActionResult GetTransactionsMempool()
    {
        var mempool = _blockchain.GetTransactions();
        if (mempool is null)
            return NotFound();
        return Ok(mempool);
    }

    [HttpGet("mine/{id}")]
    public IActionResult Mine(int id)
    {
        var newBlock = _blockchain.NewBlock_OMO(id);

        if (newBlock == null)
        {
            return BadRequest("Mining failed. No new block was created.");
        }

        if (!newBlock.GetTransactions_OMO().Any())
            return BadRequest("No transactions to mine.");

        return Ok(new
        {
            message = "New block mined successfully",
            block = new
            {
                index = newBlock.Index,
                timestamp = newBlock.Timestamp,
                transactions = newBlock.GetTransactions_OMO().Select(tx => new
                {
                    sender = tx.SenderId, 
                    recipient = tx.RecipientId, 
                    amount = tx.Amount
                }).ToList(),
                previousHash = newBlock.GetPreviousHash_OMO(),
                nonce = newBlock.GetNonce_OMO()
            }
        });
    }

    [HttpGet("blockchain")]
    public IActionResult GetChain()
    {
        if (_blockchain is null || !_blockchain.Any())
        {
            return NotFound(new { message = "Blockchain is not initialized or is empty" });
        }

        var chain = _blockchain.Select(block => new
        {
            index = block.Index,
            timestamp = block.Timestamp,
            transactions = block.GetTransactions_OMO()?.Select(tx => new
            {
                sender = tx.SenderId,
                recipient = tx.RecipientId,
                amount = tx.Amount
            }).ToList(),
            previousHash = block.GetPreviousHash_OMO(),
            nonce = block.GetNonce_OMO()
        }).ToList();

        return Ok(new
        {
            message = "Blockchain retrieved successfully",
            length = chain.Count,
            chain
        });
    }


    [HttpGet("blockchain/get/{index}")]
    public IActionResult GetBlock([FromRoute] int index)
    {
        try
        {
            var block = _jsonBlockOperations.DeserializeBlockByIndex(index);
            if (block is null)
            {
                return NotFound(new { message = $"Block with index {index} not found." });
            }

            return Ok(block);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, new { message = "An error occurred while retrieving the block." });
        }
    }

    [HttpDelete("blockchain/deleteall")]
    public IActionResult DeleteAllBlocks()
    {
        try
        {
            if (_jsonBlockOperations.GetBlockCount() == 0)
            {
                return NotFound(new { message = "No blocks to delete." });
            }

            _jsonBlockOperations.RemoveAllBlocks();
            return Ok(new { message = "Blockchain deleted successfully" });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, new { message = "An error occurred while deleting the blockchain." });
        }
    }


    [HttpDelete("blockchain/deletelast")]
    public IActionResult DeleteLastBlock()
    {
        try
        {
            if (_jsonBlockOperations.GetBlockCount() == 0)
            {
                return NotFound(new { message = "No blocks to delete." });
            }

            _jsonBlockOperations.RemoveLastBlock();
            return Ok(new { message = "Last block deleted successfully" });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, new { message = "An error occurred while deleting the last block." });
        }
    }
}