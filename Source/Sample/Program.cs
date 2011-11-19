using System;
using System.Collections.Generic;
using Clover.Proxy;
using Clover.Proxy.OldDesign;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //  List<TestEntity> data = BaseWrapper<TestWrapper>.Proxy.GetAll(1, "a");
            // Console.WriteLine("Result:" + data.Count);


            //new design
            //simple
            var service = new ProxyService();
            service.BeforeCall += (p) => { Console.WriteLine("Before Call"); };
            service.AfterCall += () => { Console.WriteLine("After Call"); };
            var item = service.Create<TestWrapper>();
            item.GetAll(1, "213");
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
            Console.WriteLine("Calling in " + AppDomain.CurrentDomain.FriendlyName);
            return new List<TestEntity> { new TestEntity(), new TestEntity() };
        }
    }
}