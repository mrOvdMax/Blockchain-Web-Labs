using Microsoft.AspNetCore.Mvc;
using OvdiienkoTB.Models;
using Transaction = System.Transactions.Transaction;

namespace OvdiienkoTB.Controllers;

public class BlockchainController : ControllerBase
{
    private static readonly Blockchain blockchain = new Blockchain();

    // POST /api/blockchain/transactions/new
    [HttpPost("transactions/new")]
    public IActionResult NewTransaction([FromBody] Transaction transaction)
    {
        if (transaction == null)
        {
            return BadRequest("Invalid transaction data");
        }

        // Add the new transaction
        int index = blockchain.NewTransaction_OMO(transaction.Sender, transaction.Recipient, transaction.Amount);
        return Created("", $"Transaction will be added to Block {index}");
    }

    // GET /api/blockchain/mine
    [HttpGet("mine")]
    public IActionResult Mine()
    {
        try
        {
            // Get the last proof
            var lastBlock = blockchain.LastBlock;
            var lastProof = lastBlock.GetProof_OMO();

            // Perform proof of work
            int proofOfWork = blockchain.ProofOfWork_OMO(lastProof);

            // Reward for finding the proof
            blockchain.NewTransaction_OMO("0", Guid.NewGuid().ToString().Replace("-", ""), 1);

            // Create the new block
            string lastHash = blockchain.GetLastHash_OMO();
            var newBlock = blockchain.NewBlock_OMO();

            return Ok(newBlock);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // GET /api/blockchain/chain
    [HttpGet("chain")]
    public IActionResult GetChain()
    {
        return Ok(blockchain);
    }
}
}