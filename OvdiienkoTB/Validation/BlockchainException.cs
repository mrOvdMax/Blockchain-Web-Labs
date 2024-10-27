namespace OvdiienkoTB.Validation;

public class BlockchainException : Exception
{
    public BlockchainException()
    {
        
    }

    public BlockchainException(string message) : base(message)
    {
        
    }

    public BlockchainException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}