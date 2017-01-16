using System.Collections.Generic;
using SharpLock;
using MongoDB.Bson;
using SharpLock.MongoDB.Tests;

namespace SharpLock.MongoDB.Tests
{
    public class LockBase : SharpLockable<ObjectId>
    {
        public LockBase()
        {
            Id = ObjectId.GenerateNewId();
            SomeVal = "abcd1234";
            SingularInnerLock = new InnerLock();
            ListOfLockables = new List<InnerLock>() { new InnerLock(), new InnerLock() };
            ArrayOfLockables = new[] { new InnerLock(), new InnerLock() };
            IEnumerableLockables = new List<InnerLock>() { new InnerLock(), new InnerLock() } as IEnumerable<InnerLock>;
        }
        public string SomeVal { get; set; }
        public InnerLock SingularInnerLock { get; set; }
        public IList<InnerLock> ListOfLockables { get; set; }
        public InnerLock[] ArrayOfLockables { get; set; }
        public IEnumerable<InnerLock> IEnumerableLockables { get; set;}
    }

}