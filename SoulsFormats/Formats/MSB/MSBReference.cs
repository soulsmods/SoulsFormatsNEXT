using System;
using System.Collections.Generic;
using System.Text;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SoulsFormats
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MSBReference : Attribute
    {
        public Type ReferenceType;
    }

    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class MSBParamReference : Attribute
    {
        public string ParamName;
    }

    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MSBEntityReference : Attribute
    {
    }
}
