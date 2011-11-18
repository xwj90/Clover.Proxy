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

        }
    }

    public class TestProvider  
    {     

        public IEnumerable<TestEntity> GetSports()
        {

            return BaseWrapper<TestWrapper>.Proxy.GetAll();

        }
    }
    public class TestEntity
    {
    }
    public class TestWrapper
    {
        public virtual List<TestEntity> GetAll()
        {
            return new List<TestEntity>();
        }
    }
}
