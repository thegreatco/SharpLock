namespace SharpLock.Exceptions
{
    public class ReleaseDistributedLockException : DistributedLockException
    {
        public ReleaseDistributedLockException( string message ) : base( message ) { }
    }
}