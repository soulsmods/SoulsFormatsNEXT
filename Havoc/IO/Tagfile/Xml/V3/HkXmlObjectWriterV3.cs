using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Havoc.Collections;
using Havoc.Extensions;
using Havoc.Objects;
using Havoc.Reflection;

namespace Havoc.IO.Tagfile.Xml.V3
{
    public class HkXmlObjectWriterV3 : IHkXmlObjectWriter
    {
        private readonly OrderedSet<IHkObject> mObjects;

        public HkXmlObjectWriterV3( IHkObject rootObject )
        {
            TypeWriter = new HkXmlTypeWriterV3( new HkTypeCompendium( rootObject ) );
            RootObject = rootObject;

            mObjects = new OrderedSet<IHkObject>();
            AddObjectsRecursively( rootObject );
        }

        public HkXmlObjectWriterV3( List<IHkObject> objs )
        {
            TypeWriter = new HkXmlTypeWriterV3( new HkTypeCompendium( objs ) );
            RootObject = objs[0];

            mObjects = new OrderedSet<IHkObject>();
            objs.ForEach(AddObjectsRecursively);
        }

        public IHkXmlTypeWriter TypeWriter { get; }
        public IHkObject RootObject { get; }

        public IReadOnlyList<IHkObject> Objects => mObjects;

        public void WriteObject( XmlWriter writer, IHkObject obj, bool writeObjectDefinition = false )
        {
            if ( writeObjectDefinition )
                writer.WriteStartElement( "object", obj.Type, ( "id", GetObjectIdString( obj ) ),
                    ( "typeid", TypeWriter.GetTypeIdString( obj.Type ) ) );

            switch ( obj.Type.Format )
            {
                case HkTypeFormat.Void:
                case HkTypeFormat.Opaque:
                    break;

                case HkTypeFormat.Bool:
                    writer.WriteElement( "bool", ( "value", obj.GetValue<HkBool, bool>() ? "true" : "false" ) );
                    break;

                case HkTypeFormat.String:
                {
                    string value = obj.Value != null ? obj.GetValue<HkString, string>() : null;
                    if ( !string.IsNullOrEmpty( value ) )
                        writer.WriteElement( "string", ( "value", value ) );
                    else
                        writer.WriteElement( "string" );

                    break;
                }

                case HkTypeFormat.Int:
                    writer.WriteElement( "integer", ( "value", obj.Value ) );
                    break;

                case HkTypeFormat.FloatingPoint:
                {
                    if ( obj.Type.IsSingle )
                    {
                        float value = obj.GetValue<HkSingle, float>();
                        unsafe
                        {
                            writer.WriteElement( "real", ( "dec", value.ToString( CultureInfo.InvariantCulture ) ),
                                ( "hex", $"#{*( uint* ) &value:X}" ) );
                        }
                    }
                    else if ( obj.Type.IsDouble )
                    {
                        double value = obj.GetValue<HkDouble, double>();
                        unsafe
                        {
                            writer.WriteElement( "real", ( "dec", value.ToString( CultureInfo.InvariantCulture ) ),
                                ( "hex", $"#{*( ulong* ) &value:X}" ) );
                        }
                    }
                    else if (obj.Type.IsHalf) {
                        Half value = obj.GetValue<HkHalf, Half>();
                        unsafe
                        {
                            writer.WriteElement( "real", ( "dec", value.ToString( CultureInfo.InvariantCulture ) ),
                                ( "hex", $"#{*( ulong* ) &value:X}" ) );
                        }
                    }
                    else
                    {
                        throw new InvalidDataException(
                            $"Unexpected floating point format: 0x{obj.Type.FormatInfo:X}" );
                    }

                    break;
                }

                case HkTypeFormat.Ptr:
                    writer.WriteElement( "pointer",
                        ( "id", GetObjectIdString( obj.Value != null ? obj.GetValue<HkPtr, IHkObject>() : null ) ) );
                    break;

                case HkTypeFormat.Class:
                {
                    writer.WriteStartElement( "record", obj.Type );
                    {
                        foreach ( var (field, fieldObject) in obj
                            .GetValue<HkClass, IReadOnlyDictionary<HkField, IHkObject>>() ) {
                            if (obj.Type.Name == "<DEBUG>" && field.Name == "<DEBUG>") {
                                Debug.WriteProcess($"Write: {obj.Type.Name}.{field.Name}");
                                Debug.WriteProcess($"  {field.Type.Name}: {fieldObject.Type.Name}({fieldObject.Type.Format}): " +
                                                   $"{(fieldObject.Value == null? "null": fieldObject.Value.GetType())}");
                            }
                            if ( ( field.Flags & HkFieldFlags.IsNotSerializable ) != 0 || !fieldObject.IsWorthWriting() )
                                continue;

                            writer.WriteStartElement( "field", ( "name", field.Name ) );
                            {
                                WriteObject( writer, fieldObject );
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    break;
                }

                case HkTypeFormat.Array:
                {
                    var array = obj.Value != null ? obj.GetValue<HkArray, IReadOnlyList<IHkObject>>() : null;

                    writer.WriteStartElement( "array", $"ArrayOf {obj.Type.SubType}", ( "count", array?.Count ?? 0 ),
                        ( "elementtypeid", TypeWriter.GetTypeIdString( obj.Type.SubType ) ) );

                    if ( array != null )
                        foreach ( var childObject in array )
                            WriteObject( writer, childObject );

                    writer.WriteEndElement();
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException( nameof( obj.Type.Format ) );
            }

            if ( writeObjectDefinition )
                writer.WriteEndElement();
        }

        public void WriteAllObjects( XmlWriter writer )
        {
            foreach ( var obj in mObjects )
                WriteObject( writer, obj, true );
        }

        public string GetObjectIdString( IHkObject obj )
        {
            return $"type{( obj != null && mObjects.IndexMap.TryGetValue( obj, out int index ) ? index + 1 : 0 )}";
        }

        private void AddObjectsRecursively( IHkObject obj )
        {
            if ( obj == null || mObjects.Contains( obj ) || obj.Value == null )
                return;

            // Debug.WriteProcess($"Add Obj {obj.Type.Format}");
            switch ( obj.Type.Format )
            {
                case HkTypeFormat.Ptr: {
                    var ptrObject = obj.GetValue<HkPtr, IHkObject>();
                    AddObjectsRecursively( ptrObject );
                    mObjects.Add( ptrObject );
                    break;
                }

                case HkTypeFormat.Class:
                {
                    if ( obj == RootObject )
                        mObjects.Add( obj );

                    foreach (var (field, fieldObject) in obj.GetValue<HkClass, IReadOnlyDictionary<HkField, IHkObject>>()) {
                        Debug.WriteProcessIndent++;
                        // Debug.WriteProcess($"Add field {field.Name}");
                        AddObjectsRecursively( fieldObject );
                        Debug.WriteProcessIndent--;
                    }

                    break;
                }

                case HkTypeFormat.Array:
                {
                    Debug.WriteProcessIndent++;
                    // Debug.WriteProcess($"Add array {((HkArray)obj).Value.Count}");
                    foreach (var childObject in obj.GetValue<HkArray, IReadOnlyList<IHkObject>>()) {
                        AddObjectsRecursively( childObject );
                    }
                    Debug.WriteProcessIndent--;

                    break;
                }
                
                default:
                    mObjects.Add(obj);
                    break;
            }
        }
    }
}