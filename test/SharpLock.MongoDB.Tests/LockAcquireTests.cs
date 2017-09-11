using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SharpLock.MongoDB.Tests
{
    [TestClass]
    public class LockAcquireTests
    {
        private ILogger _logger;
        private IMongoCollection<LockBase> _col;

        [TestInitialize]
        public async Task Setup()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();
            _logger = new LoggingShim(Log.Logger);

            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            _col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await _col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.IsTrue(lockBase.Id == lck.LockedObject.Id, "Locked Object is not the expected object.");

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
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

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
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.EnumerableLockables);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First()), "Failed to acquire lock.");

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
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ListOfLockables);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase, lockBase.ListOfLockables.First()), "Failed to acquire lock.");

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
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ArrayOfLockables);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

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
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(5));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore, 2);

            // Acquire the lock
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObject.Id, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            await Task.Delay(5000);

            // Don't bother releasing it, attempt to re-acquire.
            lck = new DistributedLock<LockBase, ObjectId>(dataStore, 2);
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObject.Id, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            lck.Dispose();
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }
    }
}