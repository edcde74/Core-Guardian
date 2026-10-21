using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
public static class CGDebugReuse
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static object Dump(object o)=>o.GetType().GetFields(F).Where(f=>f.Name!="sources").ToDictionary(f=>f.Name,f=>f.GetValue(o)?.ToString());
    public static object Run()
    {
        var fixtureType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianBodyMaskSharingTests+Fixture")).First(t=>t!=null);
        var fixture=Activator.CreateInstance(fixtureType,true);
        try
        {
            var a=fixtureType.GetMethod("Owner").Invoke(fixture,new object[]{2});var b=fixtureType.GetMethod("Owner").Invoke(fixture,new object[]{1});
            fixtureType.GetMethod("Show").Invoke(fixture,new[]{a});var ar=Get(Get(a,"maskBinding"),"current");
            var ap=Dump(Get(ar,"request"));var apart=Get(ar,"parts");
            fixtureType.GetMethod("Show").Invoke(fixture,new[]{b});var br=Get(Get(b,"maskBinding"),"current");
            var camera=(Camera)Get(fixture,"camera");
            return new{camera.allowDynamicResolution,camera.stereoEnabled,producer=ap,consumer=Dump(Get(br,"request")),valid=Get(ar,"valid"),oldEpoch=Get(ar,"epoch"),newEpoch=Get(br,"epoch"),partsA=apart,partsB=Get(br,"parts")};
        }
        finally{((IDisposable)fixture).Dispose();}
    }
}
