namespace SharpLock
{
    public class RefreshDistributedLockException : DistributedLockException
    {
        public RefreshDistributedLockException() : base() { }
        public RefreshDistributedLockException( string message ) : base( message ) { }
        public RefreshDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}