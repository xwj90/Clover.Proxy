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
            var data = BaseWrapper<TestWrapper>.Proxy.GetAll();
            Console.WriteLine(data.Count);
        }
    }

    [Serializable]
    public class TestEntity
    {
    }
    [Serializable]
    public class TestWrapper
    {
        public virtual List<TestEntity> GetAll()
        {
            return new List<TestEntity>() { new TestEntity(), new TestEntity() };
        }
    }
}
