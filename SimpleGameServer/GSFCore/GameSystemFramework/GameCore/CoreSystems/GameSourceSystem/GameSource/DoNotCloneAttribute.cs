using System;
using System.Collections.Generic;
using System.Text;

namespace GameSystem.GameCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    public class DoNotCloneAttribute : Attribute
    {
    }
}
