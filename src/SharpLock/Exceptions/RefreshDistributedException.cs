namespace SharpLock.Exceptions
{
    public class RefreshDistributedLockException : DistributedLockException
    {
        public RefreshDistributedLockException()
        { }
        public RefreshDistributedLockException( string message ) : base( message ) { }
        public RefreshDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}