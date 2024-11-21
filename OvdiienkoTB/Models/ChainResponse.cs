namespace OvdiienkoTB.Models;

public class ChainResponse
{
    public List<Block> Chain { get; set; }  // Список блоків в ланцюгу
    public int Length { get; set; }          // Довжина ланцюга

    public ChainResponse(List<Block> chain, int length)
    {
        Chain = chain;
        Length = length;
    }
}