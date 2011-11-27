using Clover.Proxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnitTest
{


    /// <summary>
    ///This is a test class for RemoteDomainProxyProviderTest and is intended
    ///to contain all RemoteDomainProxyProviderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CrossDomainSample
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion



        [TestMethod()]
        public void CrossDomainSampleTestWithSerializableClass()
        {
            try
            {
                ProxyService service = new ProxyService();
                var wrapper = service.Create<TestWrapper0>();
                wrapper.Test();
                Assert.Fail("should throw exception");
            }
            catch (Exception e)
            {
            }
        }
        [TestMethod()]
        public void CrossDomainSampleTestWithSerializableClass2()
        {

            ProxyService service = new ProxyService();
            var wrapper = service.Create<TestWrapper1>();
            wrapper.Test();
        }
         [TestMethod()]
        public void CrossDomainSampleTestWithMarshalByRefObject()
        {

            ProxyService service = new ProxyService();
            var wrapper = service.Create<TestWrapper2>();
            wrapper.Test();
        }


    }

    [Proxy(ProxyType = ProxyType.Remote)]
    public class TestWrapper0
    {
        public void Test()
        {
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
        }
    }


    [Serializable]
    [Proxy(ProxyType = ProxyType.Remote)]
    public class TestWrapper1
    {
        public void Test()
        {
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
        }
    }

    [Proxy(ProxyType = ProxyType.Remote)]
    public class TestWrapper2 : MarshalByRefObject
    {
        public void Test()
        {
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
        }
    }
}
