using System;

namespace SharpLock
{
    public interface ISharpLockable<T> : ISharpLockableBase<T>
    {
        /// <summary>
        /// The timestamp of the last time the lock was refreshed.
        /// </summary>
        DateTime? UpdateLock { get; set; }
        /// <summary>
        /// The unique Id of the instance of the lock.
        /// </summary>
        Guid? LockId { get; set; }
    }
}
