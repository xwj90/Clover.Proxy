//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sample.Local {
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using Clover.Proxy.OldDesign;
    using Clover.Proxy;
    
    
    [Serializable()]
    public class Internal_Local_TestWrapper : Sample.TestWrapper {
        
        private Clover.Proxy.ProxyProviderBase _proxyProviderBase;
        
        public Internal_Local_TestWrapper(Clover.Proxy.ProxyProviderBase _proxyProviderBase) {
this._proxyProviderBase=_proxyProviderBase;
        }
        
        public override List<global::Sample.TestEntity> GetAll(int i, string s) {
_proxyProviderBase.ExecuteBeforeCall(null);
var temp_returnData_1024=
            base.GetAll(i, s);
_proxyProviderBase.AfterCall();
return temp_returnData_1024;
        }
    }
}
