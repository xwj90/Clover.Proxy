using System;

namespace Clover.Proxy.OldDesign
{
    public static class EventMonitor
    {
        public static void BeforeCall(object[] args)
        {
            Console.WriteLine("Before Call");
        }

        public static object AfterCall()
        {
            Console.WriteLine("After Call");
            return null;
        }
    }
}