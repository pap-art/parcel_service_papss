namespace ParcelService.Exceptions
{
    [Serializable]
    public class LockerNotFoundException : Exception
    {
        private string v1;
        private string v2;

        public LockerNotFoundException()
        {
        }

        public LockerNotFoundException(string? message) : base(message)
        {
        }

        public LockerNotFoundException(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public LockerNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}