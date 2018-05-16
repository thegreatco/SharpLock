using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpLock.InMemory.Tests
{
    [TestClass]
    public class RaceConditionTests
    {
        private ISharpLockLogger _sharpLockLogger;
        private IList<LockBase> _col;

        [TestInitialize]
        public async Task Setup()
        {
            _sharpLockLogger = new LoggingShim();

            _col = new List<LockBase>();
        }

        [TestMethod]
        public async Task AcquireManyBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, string>(dataStore)).ToList();
            _sharpLockLogger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, TimeSpan.FromSeconds(1))));
            
            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");
            
            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));
            
            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));
            
            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");
            
            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManySingularSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, string>(dataStore, y => y.SingularInnerLock)).ToList();
            _sharpLockLogger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.SingularInnerLock, TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyEnumerableSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, string>(dataStore, y => y.EnumerableLockables)).ToList();
            _sharpLockLogger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First(), TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyListSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, string>(dataStore, y => y.ListOfLockables)).ToList();
            _sharpLockLogger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ListOfLockables[0], TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyArraySubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, string>(dataStore, y => y.ArrayOfLockables)).ToList();
            _sharpLockLogger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables[1], TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }
    }
}