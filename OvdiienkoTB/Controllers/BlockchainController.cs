using Microsoft.AspNetCore.Mvc;
using OvdiienkoTB.Models;
using OvdiienkoTB.Operations;

namespace OvdiienkoTB.Controllers;
[ApiController]
[Route("api/blockchain")]
public class BlockchainController : ControllerBase
{
    //private static readonly Blockchain Blockchain = new Blockchain();
    private static readonly BlockchainJson Blockchain = new BlockchainJson();
    private readonly JsonBlockOperations _jsonBlockOperations = new JsonBlockOperations();

    [HttpPost("transactions/new")]
    public IActionResult NewTransaction([FromBody] Transaction? transaction)
    {
        if (transaction == null)
        {
            return BadRequest("Invalid transaction data");
        }

        var index = Blockchain.NewTransaction_OMO(transaction.Sender, transaction.Recipient, transaction.Amount);
        return Created("", new
        {
            message = "Transaction created successfully",
            transaction = new
            {
                sender = transaction.Sender,
                recipient = transaction.Recipient,
                amount = transaction.Amount,
            },
            //transactionId = index
        });
    }

    [HttpGet("transactions/mempool")]
    public IActionResult GetTransactionsMempool()
    {
        try
        {
            return Ok(Blockchain.GetTransactions());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpGet("mine")]
    public IActionResult Mine()
    {
        try
        {
            var newBlock = Blockchain.NewBlock_OMO();

            return Ok(new
            {
                message = "New block mined successfully",
                block = new
                {
                    index = newBlock.Index,
                    timestamp = newBlock.Timestamp,
                    transactions = newBlock.GetTransactions_OMO().Select(tx => new
                    {
                        sender = tx.Sender,
                        recipient = tx.Recipient,
                        amount = tx.Amount
                    }).ToList(),
                    previousHash = newBlock.GetPreviousHash_OMO(),
                    nonce = newBlock.GetNonce_OMO()
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("blockchain")]
    public IActionResult GetChain()
    {
        if (Blockchain == null)
        {
            return BadRequest(new { message = "Blockchain is not initialized" });
        }
        
        return Ok(new
        {
            message = "Blockchain retrieved successfully",
            length = Blockchain.Count(),
            chain = Blockchain.Where(block => block != null)
                .Select(block => new
            {
                index = block.Index,
                timestamp = block.Timestamp,
                transactions = block.GetTransactions_OMO()?.Select(tx => new
                {
                    sender = tx.Sender,
                    recipient = tx.Recipient,
                    amount = tx.Amount
                }).ToList(),
                previousHash = block.GetPreviousHash_OMO(),
                nonce = block.GetNonce_OMO()
            }).ToList()
        });
    }

    [HttpGet("blockchain/get/{index}")]
    public IActionResult GetBlock([FromRoute] int index)
    {
        try
        {
            return Ok(_jsonBlockOperations.DeserializeBlockByIndex(index));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpDelete("blockchain/deleteall")]
    public IActionResult DeleteAllBlocks()
    {
        try
        {
            _jsonBlockOperations.RemoveAllBlocks();
            return Ok(new {message = "Blockchain deleted successfully"});
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    [HttpDelete("blockchain/deletelast")]
    public IActionResult DeleteLastBlock()
    {
        try
        {
            _jsonBlockOperations.RemoveLastBlock();
            return Ok(new {message = "Last block deleted successfully"});
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
