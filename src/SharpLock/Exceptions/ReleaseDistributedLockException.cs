namespace SharpLock.Exceptions
{
    public class ReleaseDistributedLockException : DistributedLockException
    {
        public ReleaseDistributedLockException()
        {}
        public ReleaseDistributedLockException( string message ) : base( message ) { }
        public ReleaseDistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}