namespace SharpLock.Exceptions
{
    public class DistributedLockException : System.Exception
    {
        public DistributedLockException( string message ) : base( message ) { }
    }
}