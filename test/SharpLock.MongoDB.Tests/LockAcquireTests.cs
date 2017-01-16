using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using Serilog.Events;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SharpLock.MongoDB.Tests
{
    public class LockAcquireTests
    {
        [Fact]
        public async Task AcquireOneBaseClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Error)
				.MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();
            var cts = new CancellationTokenSource();
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>("lockables");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);
            
            Assert.True(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.True(lockBase.Id == lck.LockedObject.Id, "Locked Object is not the expected object.");

            Assert.True(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.True(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.True(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.True(lck.Disposed, "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireOneSingularSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Error)
                .MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();
            var cts = new CancellationTokenSource();
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>("lockables");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);
            
            Assert.True(await lck.AcquireLockAsync(lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.True(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.True(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.True(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.True(lck.Disposed, "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireOneEnumerableSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Error)
                .MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();
            var cts = new CancellationTokenSource();
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>("lockables");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.IEnumerableLockables);
            
            Assert.True(await lck.AcquireLockAsync(lockBase.IEnumerableLockables.First()), "Failed to acquire lock.");

            Assert.True(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.True(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.True(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.True(lck.Disposed, "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireOneListSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Error)
                .MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();
            var cts = new CancellationTokenSource();
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>("lockables");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ListOfLockables);
            
            Assert.True(await lck.AcquireLockAsync(lockBase.ListOfLockables.First()), "Failed to acquire lock.");

            Assert.True(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.True(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.True(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.True(lck.Disposed, "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireOneArraySubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Error)
                .MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();
            var cts = new CancellationTokenSource();
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>("lockables");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ArrayOfLockables);
            
            Assert.True(await lck.AcquireLockAsync(lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

            Assert.True(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.True(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.True(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            lck.Dispose();
            Assert.True(lck.Disposed, "Failed to mark object as disposed");
        }
    }
}