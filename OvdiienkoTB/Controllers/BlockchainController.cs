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
    private readonly JsonBlockOperations _jsonBlockOperations;
    private readonly JsonBlockOperations _mainJsonBlockOperations;
    private readonly JsonNodeOperations _jsonNodeOperations = new();
    private static List<string>? _nodes = new();
    private readonly IConfiguration _configuration;

    public BlockchainController(BlockchainJson blockchain, BlockchainDbContext context, IConfiguration configuration)
    {
        _blockchain = blockchain;
        _context = context;
        _configuration = configuration;

        var port = configuration["ASPNETCORE_URLS"]?.Split(':').Last();
        var localFilePath = configuration[$"Paths:JsonNodeLocalBlockchain{port}"];
        var mainFilePath = configuration["Paths:JsonBlockchainPath"];

        _jsonBlockOperations = new JsonBlockOperations(localFilePath);
        _mainJsonBlockOperations = new JsonBlockOperations(mainFilePath);

        _nodes = configuration.GetSection("BlockchainSettings:Nodes").Get<List<string>>();
    }

    [HttpPost("nodes/register")]
    public IActionResult RegisterNodes([FromBody] List<string>? nodeAddresses)
    {
        if (nodeAddresses is null || nodeAddresses.Count == 0)
        {
            return BadRequest("Invalid node data.");
        }

        foreach (var address in nodeAddresses)
        {
            _jsonNodeOperations.AddNode(new Node { Address = address });
        }

        return Ok(new { message = "Nodes registered successfully", nodes = _jsonNodeOperations.DeserializeNodes() });
    }

    [HttpGet("nodes")]
    public IActionResult GetNodes()
    {
        var nodes = _jsonNodeOperations.DeserializeNodes();
        return Ok(new { nodes });
    }

    [HttpDelete("nodes/{address}")]
    public IActionResult RemoveNode(string address)
    {
        try
        {
            _jsonNodeOperations.RemoveNode(address);
            return Ok(new { message = $"Node {address} removed successfully." });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, new { message = "An error occurred while removing the node." });
        }
    }

    [HttpDelete("nodes")]
    public IActionResult RemoveAllNodes()
    {
        try
        {
            _jsonNodeOperations.RemoveAllNodes();
            return Ok(new { message = "All nodes removed successfully." });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, new { message = "An error occurred while removing all nodes." });
        }
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

        if (newBlock is null)
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

    [HttpPost("resolve")]
    public IActionResult ResolveConflicts()
    {
        var resolved = ResolveConflictsMethod();
        if (resolved)
        {
            return Ok("Blockchain updated after consensus resolution.");
        }

        return Ok("No consensus resolution required.");
    }

    private bool ResolveConflictsMethod()
    {
        if (_nodes == null || !_nodes.Any())
        {
            throw new InvalidOperationException("No nodes are registered for consensus resolution.");
        }

        var maxLength = _mainJsonBlockOperations.GetBlockCount();
        var newChain = _mainJsonBlockOperations.DeserializeBlocks() ?? [];

        foreach (var node in _nodes)
        {
            try
            {
                var nodeFilePath = _configuration[$"Paths:JsonNodeLocalBlockchain{node.Split(':').Last()}"];
                var nodeOperations = new JsonBlockOperations(nodeFilePath);
                var nodeChain = nodeOperations.DeserializeBlocks();

                if (nodeChain is not null && nodeChain.Count > maxLength && ValidChain(nodeChain))
                {
                    maxLength = nodeChain.Count;
                    newChain = nodeChain;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with node {node}: {ex.Message}");
            }
        }

        if (newChain != _mainJsonBlockOperations.DeserializeBlocks())
        {
            _mainJsonBlockOperations.SerializeBlocks(newChain);
            return true;
        }

        return false;
    }

    bool ValidChain(List<Block> chain)
    {
        return chain.Skip(1).All(block => block.PreviousHash.EndsWith("10"));
    }
}

