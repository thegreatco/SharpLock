using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLock.InMemory
{
    public class InMemoryDataStore<TBaseObject, TLockableObject> : IDataStore<TBaseObject, TLockableObject, string>
        where TLockableObject : ISharpLockable<string> where TBaseObject : class, ISharpLockableBase<string>
    {
        private readonly ISharpLockLogger _sharpLockLogger;
        private readonly IEnumerable<TBaseObject> _col;
        private readonly TimeSpan _lockTime;

        public InMemoryDataStore(IEnumerable<TBaseObject> rawStore, ISharpLockLogger sharpLockLogger, TimeSpan lockTime)
        {
            _col = rawStore;
            _sharpLockLogger = sharpLockLogger;
            _lockTime = lockTime;
        }

        public ISharpLockLogger GetLogger() => _sharpLockLogger;
        public TimeSpan GetLockTime() => _lockTime;

        public Task<TBaseObject> AcquireLockAsync(string baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseObjId == null) throw new ArgumentNullException(nameof(baseObjId), "Base Object Id cannot be null");
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Lockable Object cannot be null");
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1);
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x => x.Id == baseObjId && compiledSelector.Invoke(x).Id == obj.Id);
            if (baseObject == null) return Task.FromResult<TBaseObject>(null);
            var lockObject = compiledSelector.Invoke(baseObject);
            if (lockObject == null) return Task.FromResult<TBaseObject>(null);
            
            lock (lockObject)
            {
                if (lockObject.LockId != null)
                    if (lockObject.UpdateLock > staleLockTime)
                        return Task.FromResult<TBaseObject>(null);
                lockObject.LockId = Guid.NewGuid();
                lockObject.UpdateLock = lockTime;
                return Task.FromResult(baseObject);
            }
        }

        public Task<TBaseObject> AcquireLockAsync(string baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseObjId == null) throw new ArgumentNullException(nameof(baseObjId), "Base Object Id cannot be null");
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Lockable Object cannot be null");
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1);
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x =>
                x.Id == baseObjId && compiledSelector.Invoke(x).FirstOrDefault(y => y.Id == obj.Id) != null);
            
            if (baseObject == null) return Task.FromResult<TBaseObject>(null);
            var lockObjectEnumerable = compiledSelector.Invoke(baseObject);
            if (lockObjectEnumerable == null) return Task.FromResult<TBaseObject>(null);
            var lockObject = lockObjectEnumerable.FirstOrDefault(x => x.Id == obj.Id);
            if (lockObject == null) return Task.FromResult<TBaseObject>(null);

            lock (lockObject)
            {
                if (lockObject.LockId != null)
                    if (lockObject.UpdateLock > staleLockTime)
                        return Task.FromResult<TBaseObject>(null);
                lockObject.LockId = Guid.NewGuid();
                lockObject.UpdateLock = lockTime;
                return Task.FromResult(baseObject);
            }
        }

        public Task<bool> RefreshLockAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x => x.Id == baseObjId && compiledSelector.Invoke(x).Id == lockedObjecstring);
            if (baseObject == null) return Task.FromResult(false);
            var lockObject = compiledSelector.Invoke(baseObject);
            if (lockObject == null) return Task.FromResult(false);

            lock (lockObject)
            {
                if (lockObject.LockId != lockedObjectLockId)
                    return Task.FromResult(false);
                lockObject.UpdateLock = lockTime;
                return Task.FromResult(true);
            }
        }

        public Task<bool> RefreshLockAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x =>
                x.Id == baseObjId && compiledSelector.Invoke(x).FirstOrDefault(y => y.Id == lockedObjecstring) != null);

            if (baseObject == null) return Task.FromResult(false);
            var lockObjectEnumerable = compiledSelector.Invoke(baseObject);
            if (lockObjectEnumerable == null) return Task.FromResult(false);
            var lockObject = lockObjectEnumerable.FirstOrDefault(x => x.Id == lockedObjecstring);
            if (lockObject == null) return Task.FromResult(false);

            lock (lockObject)
            {
                if (lockObject.LockId != lockedObjectLockId)
                    return Task.FromResult(false);
                lockObject.UpdateLock = lockTime;
                return Task.FromResult(true);
            }
        }

        public Task<bool> ReleaseLockAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x => x.Id == baseObjId && compiledSelector.Invoke(x).Id == lockedObjecstring);
            if (baseObject == null) return Task.FromResult(true);
            var lockObject = compiledSelector.Invoke(baseObject);
            if (lockObject == null) return Task.FromResult(true);

            lock (lockObject)
            {
                if (lockObject.LockId != lockedObjectLockId)
                    return Task.FromResult(true);
                
                lockObject.LockId = null;
                lockObject.UpdateLock = null;
                return Task.FromResult(true);
            }
        }

        public Task<bool> ReleaseLockAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x =>
                x.Id == baseObjId && compiledSelector.Invoke(x).FirstOrDefault(y => y.Id == lockedObjecstring) != null);

            if (baseObject == null) return Task.FromResult(true);
            var lockObjectEnumerable = compiledSelector.Invoke(baseObject);
            if (lockObjectEnumerable == null) return Task.FromResult(true);
            var lockObject = lockObjectEnumerable.FirstOrDefault(x => x.Id == lockedObjecstring);
            if (lockObject == null) return Task.FromResult(true);

            lock (lockObject)
            {
                if (lockObject.LockId != lockedObjectLockId)
                    return Task.FromResult(true);
                lockObject.UpdateLock = null;
                lockObject.LockId = null;
                return Task.FromResult(true);
            }
        }

        public Task<TBaseObject> GetLockedObjectAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x => x.Id == baseObjId && compiledSelector.Invoke(x).Id == lockedObjecstring);
            if (baseObject == null) return Task.FromResult<TBaseObject>(null);
            var lockObject = compiledSelector.Invoke(baseObject);
            if (lockObject == null) return Task.FromResult<TBaseObject>(null);
            if (lockObject.LockId == lockedObjectLockId)
                return Task.FromResult(baseObject);
            return Task.FromResult<TBaseObject>(null);
        }

        public Task<TBaseObject> GetLockedObjectAsync(string baseObjId, string lockedObjecstring, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fieldSelector == null)
                throw new ArgumentNullException(nameof(fieldSelector),
                    "Field Selector for lockable object cannot be null");
            var compiledSelector = fieldSelector.Compile();
            var baseObject = _col.FirstOrDefault(x =>
                x.Id == baseObjId && compiledSelector.Invoke(x).FirstOrDefault(y => y.Id == lockedObjecstring) != null);

            if (baseObject == null) return Task.FromResult<TBaseObject>(null);
            var lockObjectEnumerable = compiledSelector.Invoke(baseObject);
            if (lockObjectEnumerable == null) return Task.FromResult<TBaseObject>(null);
            var lockObject = lockObjectEnumerable.FirstOrDefault(x => x.Id == lockedObjecstring);
            if (lockObject == null) return Task.FromResult<TBaseObject>(null);
            if (lockObject.LockId != lockedObjectLockId)
                return Task.FromResult<TBaseObject>(null);
            return Task.FromResult(baseObject);
        }
    }
}
