namespace SharpLock.Exceptions
{
    public class AcquireDistributedLockException : DistributedLockException
    {
        public AcquireDistributedLockException()
        { }
        public AcquireDistributedLockException( string message ) : base( message ) { }
        public AcquireDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}