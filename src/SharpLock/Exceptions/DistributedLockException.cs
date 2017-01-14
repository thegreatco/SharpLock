namespace SharpLock
{
    public class DistributedLockException : System.Exception
    {
        public DistributedLockException() { }
        public DistributedLockException( string message ) : base( message ) { }
        public DistributedLockException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}