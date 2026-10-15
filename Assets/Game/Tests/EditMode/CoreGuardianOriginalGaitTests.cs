using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianOriginalGaitTests
{
    static Type Walk=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianOriginalWalk")).First(t=>t!=null);
    static Vector2 Foot(float phase,Vector2 direction)=>(Vector2)Walk.GetMethod("FootOffset").Invoke(null,new object[]{phase,14f,1.8f,direction});
    [Test]
    public void BothFeetUseIdenticalTrajectories_HalfACycleApart_WithContinuousContact()
    {
        foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
        {
            for(int i=0;i<100;i++)
            {
                float phase=i/100f;
                Assert.That(Vector2.Distance(Foot(phase,direction),Foot(phase+1,direction)),Is.LessThan(.0001f));
                Assert.That(Vector2.Distance(Foot(phase+.5f,direction),Foot(phase-.5f,direction)),Is.LessThan(.0001f));
            }
            Assert.That(Vector2.Distance(Foot(.5f-.00001f,direction),Foot(.5f+.00001f,direction)),Is.LessThan(.002f));
            Assert.That(Vector2.Distance(Foot(1-.00001f,direction),Foot(.00001f,direction)),Is.LessThan(.002f));
        }
    }
    [Test]
    public void PlantedFootCancelsRootTravel_AndLiftOccursOnlyDuringSwing()
    {
        foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
        {
            var start=Foot(.1f,direction);
            var later=Foot(.2f,direction);
            Assert.That(Vector2.Distance(later-start,-direction*1.4f),Is.LessThan(.0001f),"A planted foot must move backward at exactly root travel speed");
            Assert.That(Vector2.Distance(Foot(.75f,direction),Vector2.up*1.8f),Is.LessThan(.0001f),"Either leg reaches the same swing lift");
        }
    }
}
