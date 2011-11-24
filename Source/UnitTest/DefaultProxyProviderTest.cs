using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Clover.Proxy;

namespace UnitTest
{
    [TestClass()]
    public class DefaultProxyProviderTest
    {
        [TestInitialize()]
        public void Initialize()
        {

        }
        [TestMethod()]
        public void Can_Proxy_Function_Class()
        {
            ProxyService service = new ProxyService();
            service.BeforeCall += (p) =>
            {
                Assert.AreEqual(2, p.Arguments.Length);
                Assert.AreEqual("aaa", p.Arguments[0]);
                Assert.AreEqual(100, p.Arguments[1]);
                p.Arguments[0] = p.Arguments[0] + "_BeforeCall_";
                p.Arguments[1] = (int)p.Arguments[1] + 100;
            };
            service.AfterCall += (p) =>
            {
                Assert.AreEqual("aaa_BeforeCall_200", p.ReturnValue);
                p.ReturnValue = p.ReturnValue + "_AfterCall";
            };
            var ac = service.Create<InheritClass>();
            var r = ac.Do("aaa", 100);
            Assert.AreEqual("aaa_BeforeCall_200_AfterCall", r);
            r = ac.DoNotProxy("aaa", 100);
            Assert.AreEqual("aaa100", r);
        }

        [TestMethod()]
        public void Can_Proxy_Normal_Class()
        {
            ProxyService service = new ProxyService();
            service.BeforeCall += (p) =>
            {
                if (p.Method.Name == "TestParams")
                {
                    Assert.AreEqual(p.Arguments[0].GetType(), typeof(int[]));
                }
            };
            service.AfterCall += (p) =>
            {
            };
            var ac = service.Create<Normal>();
            var r = ac.WithDuplexName(true, 1, "abc");
            ac.Do(5);
            ac.Do("abc");
            ac.Do(5, "abc");
            ac.Do(new object[] { });
            ac.TestParams(1, 2, 3);
            ac.Name = "Name";
        }
    }


    public class Normal
    {
        public virtual string Name { get; set; }
        public virtual string WithDuplexName(bool _hasInit, int _proxyProviderBase, string arguments)
        {
            return "";
        }
        public virtual void Do(int a)
        { }
        public virtual void Do(string a)
        {
        }
        public virtual string Do(int a, string b)
        {
            return a + b;
        }
        public virtual object[] Do(object[] a)
        {
            return new object[] { };
        }
        public virtual void TestParams(params int[] a)
        {
        }
    }

    public class InheritClass
    {
        public string DoNotProxy(string a, int b)
        {
            return a + b;
        }
        public virtual string Do(string a, int b)
        {
            return a + b;
        }
    }
}
