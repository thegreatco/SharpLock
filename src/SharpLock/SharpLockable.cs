using System;

namespace SharpLock
{
    public class SharpLockable<T> : SharpLockableBase<T>
    {
        /// <summary>
        /// The timestamp of the last time the lock was refreshed.
        /// </summary>
        public long? UpdateLock;
        /// <summary>
        /// The unique Id of the instance of the lock.
        /// </summary>
        public Guid? LockId;
    }
}
