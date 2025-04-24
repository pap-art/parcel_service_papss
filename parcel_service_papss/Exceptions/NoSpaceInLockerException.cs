namespace ParcelService.Exceptions
{
    [Serializable]
    public class NoSpaceInLockerException : Exception
    {
        private string v1;
        private string v2;

        public NoSpaceInLockerException()
        {
        }

        public NoSpaceInLockerException(string? message) : base(message)
        {
        }

        public NoSpaceInLockerException(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public NoSpaceInLockerException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}