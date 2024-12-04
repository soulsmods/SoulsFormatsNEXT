using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SoulsFormats.Util
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class HideProperty : Attribute
    {
    }

    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RotationRadians : Attribute
    {
    }

    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RotationXZY : Attribute
    {
    }

    /// <summary>
    ///     Properties with this attribute are not used as a reference
    ///     to obtain render groups from.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NoRenderGroupInheritance : Attribute
    {
    }
}
