using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Clover.Proxy;
using System.Reflection.Emit;

namespace Sample
{
    internal class Helper
    {
        public static T Create<T>(Func<T> func)
        {
            // var it = System.Reflection.Emit.DynamicMethod.GetCurrentMethod();
            return func();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            ProxyService service = new ProxyService();
            service.BeforeCall = (p) =>
            {
                Console.WriteLine("Before Call : " + p.Arguments);
            };
            service.AfterCall = (p) =>
            {
                Console.WriteLine("After Call : " + p.ReturnValue);
            };

            var item = service.Create<TestWrapper>();

            // method
            var r1 = item.GetAll(128, "Test String");
            Console.WriteLine();

            // property  可以通过配置设置某个方法，或者某类方法需要调用BeforeCall & AfterCall
            var r2 = item.Name;
            Console.WriteLine();



            var item2 = service.Create<TestWrapper2>();
            var r3 = item2.Test("test string"); //run method in remote domain //未完全完成

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

    [Serializable]
    public class TestEntity
    {
    }

    //[Proxy(DisableAutoProxy = true)]
    public class TestWrapper
    {
        public TestWrapper()
        {
        }
        public TestWrapper(int i, string name)
        {
        }
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
    public class TestWrapper2 :MarshalByRefObject
    {
        public virtual TestEntity Test(string s)
        {
            Console.WriteLine("Calling 2 in " + AppDomain.CurrentDomain.FriendlyName);
            return new TestEntity();
        }
    }
}