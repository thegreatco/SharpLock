namespace SharpLock
{
    public class AcquireDistributedLockException : DistributedLockException
    {
        public AcquireDistributedLockException() : base() { }
        public AcquireDistributedLockException( string message ) : base( message ) { }
        public AcquireDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}