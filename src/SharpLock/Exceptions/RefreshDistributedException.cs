namespace SharpLock.Exceptions
{
    public class RefreshDistributedLockException : DistributedLockException
    {
        public RefreshDistributedLockException( string message ) : base( message ) { }
    }
}