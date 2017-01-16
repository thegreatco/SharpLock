namespace SharpLock
{
    public class DistributedLock<TLockableObject, TId> : DistributedLock<TLockableObject, TLockableObject, TId> where TLockableObject : SharpLockable<TId>
    {
        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(IDataStore<TLockableObject, TLockableObject, TId> store, int staleLockMultiplier = 5)
        : base(store, x => x, staleLockMultiplier)
        {
        }
    }
}