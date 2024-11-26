namespace OvdiienkoTB.Models;

public class ChainResponse
{
    public List<Block> Chain { get; set; }
    public int Length { get; set; }

    public ChainResponse(List<Block> chain, int length)
    {
        Chain = chain;
        Length = length;
    }
}