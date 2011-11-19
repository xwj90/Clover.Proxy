﻿using System;
using System.Collections.Generic;
using Clover.Proxy;
using Clover.Proxy.OldDesign;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //new design
            //simple
            var service = new ProxyService();
            service.BeforeCall += (p) => { Console.WriteLine("Before Call"); };
            service.AfterCall += () => { Console.WriteLine("After Call"); };
            var item = service.Create<TestWrapper>();
            item.GetAll(1, "213");

            service.AfterCall = () => { Console.WriteLine("After Call2"); };
            var item2 = service.Create<TestWrapper2>();
            item2.Test("213");
        }
    }


    public class TestEntity
    {
    }


    public class TestWrapper
    {
         
        public virtual List<TestEntity> GetAll(int i, string s)
        {
            Console.WriteLine("Calling in " + AppDomain.CurrentDomain.FriendlyName);
            return new List<TestEntity> { new TestEntity(), new TestEntity() };
        }
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