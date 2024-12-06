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
    private readonly JsonNodeOperations _jsonNodeOperations = new();
    private static List<string>? _nodes = [];
    private readonly IConfiguration _configuration;

    public BlockchainController(BlockchainJson blockchain, BlockchainDbContext context, IConfiguration configuration)
    {
        _blockchain = blockchain;
        _context = context;
        _configuration = configuration;
        var port = configuration["ASPNETCORE_URLS"]?.Split(':').Last();
        Console.WriteLine($"Port: {port}");
        var localFilePath = configuration[$"Paths:JsonNodeLocalBlockchain{port}"];
        Console.WriteLine($"Local file path: {localFilePath}");
        var mainFilePath = configuration["Paths:JsonBlockchainPath"];
        _jsonBlockOperations = new JsonBlockOperations(localFilePath);
        _nodes = configuration.GetSection("BlockchainSettings:Nodes").Get<List<string>>() ?? [];
        Console.WriteLine("Registered nodes:");
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
        try
        {
            Console.WriteLine("Starting mining process...");
            var localChain = _jsonBlockOperations.DeserializeBlocks() ?? new List<Block>();

            // Перевіряємо, чи є блоки в локальному блокчейні
            if (!localChain.Any())
            {
                Console.WriteLine("Local blockchain is empty. Creating genesis block.");
                var genesisBlock = _blockchain.NewGenesisBlock_OMO("InitialHash", id);
                localChain.Add(genesisBlock);
                _jsonBlockOperations.SerializeBlocks(localChain);
            }

            var newBlock = _blockchain.NewBlock_OMO(id);

            if (newBlock is null)
            {
                return BadRequest("Mining failed. No new block was created.");
            }

            if (!newBlock.GetTransactions_OMO().Any())
            {
                return BadRequest("No transactions to mine.");
            }

            localChain.Add(newBlock);
            Console.WriteLine($"New block added to local chain with index {newBlock.Index}.");

            _jsonBlockOperations.SerializeBlocks(localChain);

            Console.WriteLine(System.IO.File.Exists(_jsonBlockOperations.GetFilePath())
                ? $"Local blockchain file created at {_jsonBlockOperations.GetFilePath()}"
                : "Failed to create the local blockchain file.");

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
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return StatusCode(500, new { message = "An error occurred while mining the block." });
        }
    }


    /*[HttpGet("blockchain")]
    public IActionResult GetChain()
    {
        var mainChain = _mainJsonBlockOperations.DeserializeBlocks();
        if (mainChain == null || !mainChain.Any())
        {
            return NotFound(new { message = "Main blockchain is not initialized or is empty" });
        }

        var chain = mainChain.Select(block => new
        {
            index = block.Index,
            timestamp = block.Timestamp,
            transactions = block.Transactions?.Select(tx => new
            {
                sender = tx.SenderId,
                recipient = tx.RecipientId,
                amount = tx.Amount
            }).ToList(),
            previousHash = block.PreviousHash,
            nonce = block.Nonce
        }).ToList();

        return Ok(new
        {
            message = "Main blockchain retrieved successfully",
            length = chain.Count,
            chain
        });
    }*/


    [HttpGet("chain")]
    public IActionResult GetLocalChain()
    {
        try
        {
            Console.WriteLine("Attempting to retrieve local blockchain...");
            Console.WriteLine($"Using file path: {_jsonBlockOperations.GetFilePath()}");

            var localChain = _jsonBlockOperations.DeserializeBlocks();
            if (localChain is null || !localChain.Any())
            {
                Console.WriteLine("Local blockchain is empty or not found.");
                return NotFound(new { message = "Local blockchain is empty or not found." });
            }

            var chain = localChain.Select(block => new
            {
                index = block.Index,
                timestamp = block.Timestamp,
                transactions = block.Transactions?.Select(tx => new
                {
                    sender = tx.SenderId,
                    recipient = tx.RecipientId,
                    amount = tx.Amount
                }).ToList(),
                previousHash = block.PreviousHash,
                nonce = block.Nonce
            }).ToList();

            Console.WriteLine("Local blockchain retrieved successfully.");
            return Ok(new
            {
                message = "Local blockchain retrieved successfully",
                length = chain.Count,
                chain
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return StatusCode(500, new { message = "An error occurred while retrieving the local blockchain." });
        }
    }


    [HttpGet("get/{index}")]
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

    [HttpDelete("deleteall")]
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

    [HttpDelete("deletelast")]
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
        return Ok(resolved ? "Blockchain updated after consensus resolution." : "No consensus resolution required.");
    }


    private bool ResolveConflictsMethod()
    {
        if (_nodes == null || !_nodes.Any())
        {
            throw new InvalidOperationException("No nodes are registered for consensus resolution.");
        }

        var maxLength = 0;
        var winningChain = new List<Block>();
        var longestChainCount = 0;
        var nodeChains = new Dictionary<string, List<Block>>();
        var allLostBlocks = new List<Block>();

        foreach (var node in _nodes)
        {
            try
            {
                var nodeFilePath = _configuration[$"Paths:JsonNodeLocalBlockchain{node.Split(':').Last()}"];
                var nodeOperations = new JsonBlockOperations(nodeFilePath);
                var nodeChain = nodeOperations.DeserializeBlocks();

                nodeChains[node] = nodeChain;

                if (nodeChain != null && nodeChain.Count > maxLength && ValidChain(nodeChain))
                {
                    maxLength = nodeChain.Count;
                    winningChain = nodeChain;
                    longestChainCount = 1; // Скидаємо лічильник, оскільки знайшли новий найдовший ланцюг
                }
                else if (nodeChain != null && nodeChain.Count == maxLength && ValidChain(nodeChain))
                {
                    longestChainCount++; // Збільшуємо лічильник, якщо знаходимо ланцюг такої ж довжини
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with node {node}: {ex.Message}");
            }
        }

        if (longestChainCount > 1)
        {
            Console.WriteLine("Multiple nodes have the longest chain. Consensus cannot be reached.");
            return false;
        }

        if (maxLength > 0 && longestChainCount == 1)
        {
            foreach (var node in _nodes)
            {
                try
                {
                    var nodeFilePath = _configuration[$"Paths:JsonNodeLocalBlockchain{node.Split(':').Last()}"];
                    var nodeOperations = new JsonBlockOperations(nodeFilePath);
                    var nodeChain = nodeChains[node];

                    if (!nodeChain.SequenceEqual(winningChain))
                    {
                        allLostBlocks.AddRange(nodeChain.Where((t, i) => i >= winningChain.Count || !t.GetHash_OMO().Equals(winningChain[i].GetHash_OMO())));
                        /*for (int i = 0; i < nodeChain.Count; i++)
                        {
                            if (i >= winningChain.Count ||
                                !nodeChain[i].GetHash_OMO().Equals(winningChain[i].GetHash_OMO()))
                            {
                                allLostBlocks.Add(nodeChain[i]);
                            }
                        }*/
                    }

                    nodeOperations.SerializeBlocks(winningChain);
                    Console.WriteLine($"Node {node} updated with the new blockchain.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating node {node}: {ex.Message}");
                }
            }

            var distinctBlocks = allLostBlocks
                .GroupBy(block => block.GetHash_OMO())
                .Select(group => group.First())
                .ToList();

            foreach (var transaction in distinctBlocks.SelectMany(block => block.GetTransactions_OMO()))
            {
                if (transaction.SenderId == 0)
                {
                    UpdateWalletBalance(transaction.RecipientId, -transaction.Amount);
                }
                else
                {
                    UpdateWalletBalance(transaction.SenderId, transaction.Amount);
                    UpdateWalletBalance(transaction.RecipientId, -transaction.Amount);
                }

                _blockchain.AddTransactionToMempool(transaction);
                Console.WriteLine(
                    $"Transaction from sender {transaction.SenderId} to recipient {transaction.RecipientId} for amount {transaction.Amount} returned to mempool.");
            }
            
            /*foreach (var block in distinctBlocks)
            {
                foreach (var transaction in block.GetTransactions_OMO())
                {
                    if (transaction.SenderId == 0)
                    {
                        UpdateWalletBalance(transaction.RecipientId, -transaction.Amount);
                    }
                    else
                    {
                        UpdateWalletBalance(transaction.SenderId, transaction.Amount);
                        UpdateWalletBalance(transaction.RecipientId, -transaction.Amount);
                    }

                    _blockchain.AddTransactionToMempool(transaction);
                    Console.WriteLine(
                        $"Transaction from sender {transaction.SenderId} to recipient {transaction.RecipientId} for amount {transaction.Amount} returned to mempool.");
                }
            }*/

            Console.WriteLine("Blockchain updated after consensus resolution.");
            return true;
        }

        Console.WriteLine("No consensus resolution required.");
        return false;
    }

    private void UpdateWalletBalance(int walletReceipentId, decimal amount)
    {
        var wallet = _context.Wallets.FirstOrDefault(w => w.Id == walletReceipentId);
        if (wallet is not null)
        {
            wallet.Amount += amount;
            _context.SaveChanges();
            Console.WriteLine($"Updated balance for wallet {walletReceipentId}. New balance: {wallet.Amount}");
        }
        else
        {
            Console.WriteLine($"Wallet for wallet {walletReceipentId} not found.");
        }
    }


    private static bool ValidChain(List<Block> chain)
    {
        return chain.Skip(1).All(block => block.PreviousHash.EndsWith("10"));
    }
}