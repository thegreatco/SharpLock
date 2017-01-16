using MongoDB.Bson;
using System.Collections;

namespace SharpLock.MongoDB.Tests
{
    public class InnerLock : SharpLockable<ObjectId>
    {
        public InnerLock()
        {
            Id = ObjectId.GenerateNewId();
            SomeVal = "abcd1234";
        }
        public string SomeVal { get; set;}
    }
}