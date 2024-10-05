using Microsoft.AspNetCore.Mvc;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Controllers;
[ApiController]
[Route("api/blockchain")]
public class BlockchainController : ControllerBase
{
    private static readonly Blockchain Blockchain = new Blockchain();

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
            transactionId = index
        });
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

    [HttpGet("chain")]
    public IActionResult GetChain()
    {
        return Ok(new
        {
            message = "Blockchain retrieved successfully",
            length = Blockchain.Count(),
            chain = Blockchain.Select(block => new
            {
                index = block.Index,
                timestamp = block.Timestamp,
                transactions = block.GetTransactions_OMO().Select(tx => new
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
}
