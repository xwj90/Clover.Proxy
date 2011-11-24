using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Clover.Proxy;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
           
            ////new design
            ////simple
            //var service = new ProxyService();
            //service.BeforeCall += (p) =>
            //{
            //    Console.WriteLine("Before Call");
            //};
            //service.AfterCall += (p) =>
            //{
            //    Console.WriteLine("After Call"); if (p.ProxiedMethod.Name.IndexOf("Name") != -1) p.ReturnValue = 100;
            //};
            //var item = service.Create<TestWrapper2>();
            //ComplexClass cc = service.Create<ComplexClass>();
            //cc.TestInt = 5;
            //cc.TestString = "swc";
            //cc.TestDateTime = DateTime.Now;
            //cc.TestIntArray = new int[1];
            //cc.TestStringArray = new string[1];
            //cc.TestIntList = new List<int>();
            //cc.TestStringList = new List<string>();
            //cc.TestNestClass = new ComplexClass.NestClass();
            //cc.TestNestClass.A = 5;

          
          
            //item.Test("111111");

            //var concurentDictionary = new ConcurrentDictionary<int, int>();
            ////int v = 0;

            //item.Test("111111");
            ////   {
            ////       var key = 1;
            ////       var returnValue = concurentDictionary.GetOrAdd(key, (p) =>
            ////           {
            ////               return Interlocked.Increment(ref v);
            ////           });
            ////       Console.WriteLine(returnValue);
            ////   });

        }
    }
    public class ComplexClass
    {
        public class NestClass
        {
            public virtual int A { get; set; }
        }
         

        public virtual int TestInt { get; set; }
        public virtual string TestString { get; set; }
        public virtual DateTime TestDateTime { get; set; }
        public virtual int[] TestIntArray { get; set; }
        public virtual string[] TestStringArray { get; set; }
        public virtual List<int> TestIntList { get; set; }
        public virtual List<string> TestStringList { get; set; }
        public virtual NestClass TestNestClass { get; set; }
         
        public ComplexClass()
        {
            TestIntArray = new int[10];
            TestStringArray = new string[10];
            TestIntList = new List<int>();
            TestStringList = new List<string>();
            TestNestClass = new NestClass();
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

    [Proxy(ProxyType = ProxyType.Remote)]
    [Serializable]
    public class TestWrapper2
    {
        public virtual TestEntity Test(string s)
        {
            Console.WriteLine("Calling 2 in " + AppDomain.CurrentDomain.FriendlyName);
            return new TestEntity();
        }
    }
}