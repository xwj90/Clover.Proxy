using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clover.Proxy;
namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            //  var s = __arglist(1, 2, 3);

            var data = BaseWrapper<TestWrapper>.Proxy.GetAll(1, "a");
            Console.WriteLine("Result:" + data.Count);

            // Console.WriteLine(__arglist);
        }
    }

    [Serializable]
    public class TestEntity
    {
    }
    [Serializable]
    public class TestWrapper
    {
        public virtual List<TestEntity> GetAll(int i, string s)
        {
            Console.WriteLine("Calling");
            return new List<TestEntity>() { new TestEntity(), new TestEntity() };
        }
    }
}
