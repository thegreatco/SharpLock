namespace SharpLock
{
    public class DistributedLock<TLockableObject, TId> : DistributedLock<TLockableObject, TLockableObject, TId> where TLockableObject : SharpLockable<TId>
    {
        public DistributedLock(IDataStore<TLockableObject, TLockableObject> col)
        : base(col, x => x)
        {
        }
    }
}