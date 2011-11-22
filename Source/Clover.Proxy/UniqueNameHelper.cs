using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clover.Proxy
{
    class UniqueNameHelper
    {
        private HashSet<string> nameSet = new HashSet<string>();
        private int counter;
        public string ToUniqueName(string oriname)
        {
            var temp = oriname;
            while (true)
            {
                if (!nameSet.Contains(temp))
                {
                    nameSet.Add(temp);
                    return temp;
                }
                counter++;
                temp += counter;
            }
        }
        public void Add(string name)
        {
            nameSet.Add(name);
        }
    }
}
