namespace ParcelService.Exceptions
{
    [Serializable]

    public class InvalidSecurityCodeException : Exception
    {
        public InvalidSecurityCodeException(string message) : base(message) { }
    }
    
}