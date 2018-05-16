using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLock.Exceptions;

namespace SharpLock.InMemory.Tests
{
    [TestClass]
    public class LockAcquireTests
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
        public async Task AcquireOneBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");

            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneSingularSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.SingularInnerLock);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneEnumerableSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.EnumerableLockables);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneListSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.ListOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ListOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneArraySubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.ArrayOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAfterLossAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore, 2);

            // Acquire the lock
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            await Task.Delay(5000);

            // Don't bother releasing it, attempt to re-acquire.
            lck = new DistributedLock<LockBase, string>(dataStore, 2);
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneArraySubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.ArrayOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneListSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.ListOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ListOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneEnumerableSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.EnumerableLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");

            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneSingularSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task ToStringBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task RefreshAlreadyReleasedBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.IsFalse(await lck.RefreshLockAsync(), "await lck.RefreshLockAsync()");

            await Assert.ThrowsExceptionAsync<RefreshDistributedLockException>(async () => await lck.RefreshLockAsync(true), "async () => await lck.RefreshLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task GetObjectBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsTrue(await lck.GetObjectAsync() == null, "await lck.GetObjectAsync() == null");

            await Assert.ThrowsExceptionAsync<DistributedLockException>(() => lck.GetObjectAsync(true), "() => lck.GetObjectAsync(true)");

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task DisposeBaseClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock) != null");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task ToStringSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task RefreshAlreadyReleasedSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.IsFalse(await lck.RefreshLockAsync(), "await lck.RefreshLockAsync()");

            await Assert.ThrowsExceptionAsync<RefreshDistributedLockException>(async () => await lck.RefreshLockAsync(true), "async () => await lck.RefreshLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task GetObjectSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase, InnerLock>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, string>(dataStore, x => x.SingularInnerLock);

            Assert.IsTrue(await lck.GetObjectAsync() == null, "await lck.GetObjectAsync() == null");

            await Assert.ThrowsExceptionAsync<DistributedLockException>(() => lck.GetObjectAsync(true), "() => lck.GetObjectAsync(true)");

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task DisposeSubClassAsync()
        {
            var lockBase = new LockBase();
            _col.Add(lockBase);
            var dataStore = new InMemoryDataStore<LockBase>(_col, _sharpLockLogger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, string>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }
    }
}