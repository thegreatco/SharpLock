namespace SharpLock
{
    public class ReleaseDistributedLockException : DistributedLockException
    {
        public ReleaseDistributedLockException() : base() {}
        public ReleaseDistributedLockException( string message ) : base( message ) { }
        public ReleaseDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}