namespace SharpLock.Exceptions
{
    public class AcquireDistributedLockException : DistributedLockException
    {
        public AcquireDistributedLockException( string message ) : base( message ) { }
    }
}