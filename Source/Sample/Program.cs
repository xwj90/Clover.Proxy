using System;
using System.Collections.Generic;
using Clover.Proxy;
using Clover.Proxy.OldDesign;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //new design
            //simple
            var service = new ProxyService();
            service.BeforeCall += (p) =>
            {
                Console.WriteLine("Before Call");
            };
            service.AfterCall += (p) =>
            {
                Console.WriteLine("After Call"); if (p.ProxiedMethod.Name.IndexOf("Name") != -1) p.ReturnValue = 100;
            };
            var item = service.Create<TestWrapper>();
            //item.GetAll(1, "213");
            item.Name = 5;
            //item.Name1 = null;


            var concurentDictionary = new ConcurrentDictionary<int, int>();
            int v = 0;

            Parallel.For(0, 1000, (i) =>
               {
                   var key = 1;
                   var returnValue = concurentDictionary.GetOrAdd(key, (p) =>
                       {
                           return Interlocked.Increment(ref v);
                       });
                   Console.WriteLine(returnValue);
               });

        }
    }


    public class TestEntity
    {
    }

    //[Proxy(DisableAutoProxy = true)]
    public class TestWrapper
    {

        public virtual List<TestEntity> GetAll(int arguments, string invocation)
        {
            Console.WriteLine("Calling in " + AppDomain.CurrentDomain.FriendlyName);
            return new List<TestEntity> { new TestEntity(), new TestEntity() };
        }
        public virtual List<TestEntity> GetAll(int arguments, string invocation, string t3)
        {
            Console.WriteLine("Calling in " + AppDomain.CurrentDomain.FriendlyName);
            return new List<TestEntity> { new TestEntity(), new TestEntity() };
        }
        public virtual int Name { get; set; }

        public virtual string[] Name1 { get; set; }
    }
    public class TestWrapper2
    {
        public virtual TestEntity Test(string s)
        {
            Console.WriteLine("Calling 2 in " + AppDomain.CurrentDomain.FriendlyName);
            return new TestEntity();
        }
    }
}