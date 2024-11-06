using System;
using System.Collections.Generic;
using System.IO;
using Havoc.Reflection;

namespace Havoc.Objects
{
    public static class HkObjectEx
    {
        public static TValueType GetValue<TObjectType, TValueType>( this IHkObject obj )
        {
            if ( !( obj is TObjectType ) || !( obj.Value is TValueType value ) )
                throw new InvalidDataException( $"Expected HK object to be of {typeof( TObjectType ).Name} type." );

            return value;
        }

        public static TValueType GetValueOrDefault<TObjectType, TValueType>( this IHkObject obj ) where TValueType: class
        {
            if (obj == null || obj.Value == null) {
                return null;
            }
            if ( !( obj is TObjectType ) )
                throw new InvalidDataException( $"Expected HK object to be of {typeof( TObjectType ).Name} type." );

            if ( obj.Value.Equals( default( TValueType ) ) )
                return default;

            if ( !( obj.Value is TValueType value ) )
                throw new InvalidDataException( $"Expected value to be of {typeof( TValueType ).Name} type." );

            return value;
        }

        public static bool IsWorthWriting( this IHkObject obj )
        {
            switch ( obj.Type.Format )
            {
                case HkTypeFormat.Void:
                case HkTypeFormat.Opaque:
                    return false;

                case HkTypeFormat.Bool:
                    return obj.GetValue<HkBool, bool>();

                case HkTypeFormat.String:
                    return !string.IsNullOrEmpty( obj.GetValueOrDefault<HkString, string>() );

                case HkTypeFormat.Int:
                    return Convert.ToDecimal(obj.Value) != 0;
                case HkTypeFormat.FloatingPoint:
                    if (obj.Type.IsHalf) {
                        return ((Half)obj.Value) != (Half)0;
                    }
                    if ((obj.Type.IsSingle && ((float)obj.Value >= 0xff7fffee) || float.IsNaN((float)obj.Value))) {
                        // Handle "NaN"
                        // TODO: support more NaN?
                        return true;
                    }

                    decimal dec = 0;
                    try {
                        dec = Convert.ToDecimal(obj.Value);
                    } catch (Exception e) {
                        Console.WriteLine($"value: {obj.GetType()} {obj.Value}, error: {e}");
                        return true;
                    }
                    return dec != 0;

                case HkTypeFormat.Ptr:
                    return obj.Value != null;

                case HkTypeFormat.Array when !obj.Type.IsFixedSize:
                    return obj.Value != null && obj.GetValue<HkArray, IReadOnlyList<IHkObject>>().Count != 0;
            }

            return true;
        }
    }
}