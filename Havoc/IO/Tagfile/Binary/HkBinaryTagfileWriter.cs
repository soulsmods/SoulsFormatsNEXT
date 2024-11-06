using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Havoc.Extensions;
using Havoc.IO.Tagfile.Binary.Sections;
using Havoc.IO.Tagfile.Binary.Types;
using Havoc.Objects;
using Havoc.Reflection;

namespace Havoc.IO.Tagfile.Binary
{
    public class HkBinaryTagfileWriter : IDisposable
    {
        private readonly Dictionary<IHkObject, int> mIndexMap;
        private readonly List<Item> mItems;
        private readonly bool mLeaveOpen;
        private readonly Dictionary<HkType, List<long>> mPatches;

        private readonly IHkObject mRootObject;

        private readonly HkSdkVersion mSdkVersion;
        private readonly Stream mStream;
        private readonly HkTypeCompendium mTypeCompendium;
        private readonly BinaryWriter mWriter;

        private long mDataOffset;

        private HkBinaryTagfileWriter( Stream stream, bool leaveOpen, IHkObject rootObject, HkSdkVersion sdkVersion )
        {
            mStream = stream;
            mWriter = new BinaryWriter( mStream, Encoding.UTF8, true );
            mLeaveOpen = leaveOpen;
            mRootObject = rootObject;
            mTypeCompendium = new HkTypeCompendium( rootObject );
            mSdkVersion = sdkVersion;
            mIndexMap = new Dictionary<IHkObject, int>();
            mItems = new List<Item>();
            mPatches = new Dictionary<HkType, List<long>>();
            AddItemsRecursively( rootObject );
        }

        public void Dispose()
        {
            if ( !mLeaveOpen )
                mStream.Dispose();

            mWriter.Dispose();
        }

        private void WriteTagSection()
        {
            using ( var tagSection = new HkSectionWriter( mWriter, "TAG0", true ) )
            {
                using ( var sdkVersionSection = new HkSectionWriter( mWriter, "SDKV", false ) )
                {
                    mWriter.Write( mSdkVersion.ToString(), 8 );
                }

                WriteDataSection();
                WriteTypeSection();
                // WriteTCRFSection();
                WriteIndexSection();
            }
        }

        private void WriteDataSection()
        {
            using ( var dataSection = new HkSectionWriter( mWriter, "DATA", false ) )
            {
                mDataOffset = mStream.Position;

                // Debug.WriteProcess($"WRITE: items {mItems.Count}");
                foreach ( var item in mItems )
                {
                    mWriter.WriteAlignmentPadding( item.Type.Alignment );

                    item.Position = mStream.Position;
                    // Debug.WriteProcess($"WRITE:   objects {item.Objects.Count}");
                    foreach ( var obj in item.Objects )
                        WriteObject( obj );
                }
            }
        }

        private void WriteTypeSection()
        {
            HkBinaryTypeWriter.WriteTypeSection( mWriter, mTypeCompendium, mSdkVersion );
        }

        private int GetTypeIndex( HkType type )
        {
            return type != null && mTypeCompendium.IndexMap.TryGetValue( type, out int index ) ? index + 1 : 0;
        }

        private int GetItemIndex( IHkObject obj )
        {
            return obj != null && mIndexMap.TryGetValue( obj, out int index ) ? index + 1 : 0;
        }

        private void WriteIndexSection()
        {
            using ( var indexSection = new HkSectionWriter( mWriter, "INDX", true ) )
            {
                Debug.WriteProcess($"Items: {mItems.Count}, Patches: {mPatches.Select(x=>x.Value.Count).Sum()}");
                using ( var itemSection = new HkSectionWriter( mWriter, "ITEM", false ) )
                {
                    mWriter.WriteNulls( 12 );

                    foreach ( var item in mItems )
                    {
                        mWriter.Write( GetTypeIndex( item.Type ) | ( item.IsPtr ? 0x10000000 : 0x20000000 ) );
                        mWriter.Write( ( uint ) ( item.Position - mDataOffset ) );
                        mWriter.Write( item.Objects.Count );
                    }
                }

                using ( var patchSection = new HkSectionWriter( mWriter, "PTCH", false ) ) {
                    foreach ( var ( type, positions ) in mPatches.OrderBy( x => GetTypeIndex( x.Key ) ) ) {
                        mWriter.Write( GetTypeIndex( type ) );
                        mWriter.Write( positions.Count );

                        Debug.WriteProcess($"WritePTCH: {GetTypeIndex( type )} {type.Name} ({positions.Count})");
                        var idx = -1;
                        // Debug.Temporary($"WritePTCH: {GetTypeIndex( type )} {type.Name} ({positions.Count})");
                        foreach (long position in positions.OrderBy(x => x)) {
                            idx++;
                            if (idx < 10) {
                                Debug.WriteProcess($"WritePTCH: offset {position - mDataOffset}; real: {position} = {mDataOffset} + {(position - mDataOffset)}");
                            }
                            mWriter.Write( ( uint ) ( position - mDataOffset ) );
                        }
                    }
                }
            }
        }

        private static int writeIdx = -1;
        private void WriteObject( IHkObject obj ) {
            writeIdx++;
            if (writeIdx < 100) {
                Debug.WriteProcess($"Write object type {obj.Type}: pos {mStream.Position}");
            }
            
            switch ( obj.Type.Format )
            {
                case HkTypeFormat.Void:
                case HkTypeFormat.Opaque:
                    break;

                case HkTypeFormat.Bool:
                {
                    int value = obj.GetValue<HkBool, bool>() ? 1 : 0;

                    switch ( obj.Type.BitCount )
                    {
                        case 8:
                            mWriter.Write( ( byte ) value );
                            break;
                        case 16:
                            mWriter.Write( ( short ) value );
                            break;
                        case 32:
                            mWriter.Write( value );
                            break;
                        case 64:
                            mWriter.Write( ( long ) value );
                            break;

                        default:
                            throw new InvalidDataException( $"Unexpected bit count: {obj.Type.BitCount}" );
                    }

                    break;
                }

                case HkTypeFormat.String:
                {
                    if (obj.Type.IsFixedSize) {
                        mWriter.Write( obj.GetValue<HkString, string>(), obj.Type.FixedSize );
                    }
                    else {
                        // Debug.WriteProcess($"Write patched VariantString {obj.Type.Name}: {obj.Value}");
                        WriteItemIndex( obj );
                    }

                    break;
                }

                case HkTypeFormat.Int:
                {
                    switch ( obj.Type.BitCount )
                    {
                        case 8:
                            if ( obj.Type.IsSigned )
                                mWriter.Write( obj.GetValue<HkSByte, sbyte>() );
                            else
                                mWriter.Write( obj.GetValue<HkByte, byte>() );
                            break;

                        case 16:
                            if ( obj.Type.IsSigned )
                                mWriter.Write( obj.GetValue<HkInt16, short>() );
                            else
                                mWriter.Write( obj.GetValue<HkUInt16, ushort>() );
                            break;

                        case 32:
                            if ( obj.Type.IsSigned )
                                mWriter.Write( obj.GetValue<HkInt32, int>() );
                            else
                                mWriter.Write( obj.GetValue<HkUInt32, uint>() );
                            break;

                        case 64:
                            if ( obj.Type.IsSigned )
                                mWriter.Write( obj.GetValue<HkInt64, long>() );
                            else
                                mWriter.Write( obj.GetValue<HkUInt64, ulong>() );
                            break;

                        default:
                            throw new InvalidDataException( $"Unexpected bit count: {obj.Type.BitCount}" );
                    }

                    break;
                }

                case HkTypeFormat.FloatingPoint:
                {
                    if ( obj.Type.IsSingle )
                        mWriter.Write( obj.GetValue<HkSingle, float>() );

                    else if ( obj.Type.IsDouble )
                        mWriter.Write( obj.GetValue<HkDouble, double>() );

                    else if ( obj.Type.IsHalf )
                        mWriter.Write( obj.GetValue<HkHalf, Half>() );

                    else
                        throw new InvalidDataException(
                            $"Unexpected floating point format: 0x{obj.Type.FormatInfo:X}" );

                    break;
                }

                case HkTypeFormat.Ptr:
                {
                    // Debug.WriteProcess($"Write patched VariantPtr {obj.Type.Name}: {obj.Value}");
                    WriteItemIndex( obj );
                    break;
                }

                case HkTypeFormat.Class:
                {
                    long position = mStream.Position;
                    mWriter.WriteNulls( obj.Type.ByteSize );

                    foreach ( var (field, fieldObject) in
                        obj.GetValue<HkClass, IReadOnlyDictionary<HkField, IHkObject>>() )
                    {
                        mStream.Seek( position + field.ByteOffset, SeekOrigin.Begin );
                        WriteObject( fieldObject );
                    }

                    mStream.Seek( position + obj.Type.ByteSize, SeekOrigin.Begin );

                    break;
                }

                case HkTypeFormat.Array:
                {
                    if ( obj.Type.IsFixedSize )
                    {
                        foreach (var childObject in obj.GetValue<HkArray, IReadOnlyList<IHkObject>>()) {
                            WriteObject( childObject );
                        }}
                    else {
                        // Debug.WriteProcess($"Write patched VariantSize Array {obj.Type.Name}: {obj.Value}");
                        WriteItemIndex( obj );
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException( nameof( obj.Type.Format ) );
            }
        }

        private static int itemIdx = -1;
        private void WriteItemIndex( IHkObject obj )
        {
            int index = GetItemIndex( obj );
            if (index != 0) {
                itemIdx++;
                // if (itemIdx < 100) {
                    // Debug.WriteProcess($"WriteItem index {index} for obj {obj.Type}: pos {mStream.Position}");
                // }
                AddPatch( obj.Type, mStream.Position );
            }

            mWriter.Write( index );
        }

        private void AddItemsRecursively( IHkObject obj )
        {
            if ( obj == null || mIndexMap.ContainsKey( obj ) || obj.Value == null )
                return;

            switch ( obj.Type.Format )
            {
                case HkTypeFormat.String when !obj.Type.IsFixedSize:
                {
                    // Debug.WriteProcess($"Write VariantSize Str: {obj.Type.Name}, {obj.Value}.");
                    Debug.WriteProcessIndent += 1;
                    // TODO: Find a better way to accomplish this
                    var charType = mTypeCompendium.FirstOrDefault( x => x.Name.Equals( "char" ) );
                    if ( charType == null ) {
                        mTypeCompendium.Add( charType = new HkType
                        {
                            Name = "char",
                            Flags = HkTypeFlags.HasFormatInfo | HkTypeFlags.HasByteSize,
                            mFormatInfo = 0x2004,
                            mByteSize = 1,
                            mAlignment = 1
                        } );
                        
                    }

                    var bytes = Encoding.UTF8.GetBytes(obj.GetValue<HkString, string>())
                        .Select(x => new HkByte(charType, x)).Append(new HkByte(charType, 0));
                    AddItem( new Item( charType, bytes ) );

                    Debug.WriteProcessIndent -= 1;
                    break;
                }

                case HkTypeFormat.Ptr:
                {
                    var ptrObject = obj.GetValue<HkPtr, IHkObject>();
                    AddItem( new Item( ptrObject.Type, ptrObject ) );
                    // Debug.WriteProcess($"Write Ptr: {obj.Type.Name}");
                    Debug.WriteProcessIndent += 1;
                    AddItemsRecursively( ptrObject );
                    Debug.WriteProcessIndent -= 1;

                    break;
                }

                case HkTypeFormat.Class:
                {
                    // TODO: Do this in a way that makes more sense
                    if ( obj == mRootObject )
                        AddItem( new Item( obj.Type, obj ) );

                    // Debug.WriteProcess($"Write Class: {obj.Type.Name}");
                    foreach (var (fieldType, fieldObject) in obj.GetValue<HkClass, IReadOnlyDictionary<HkField, IHkObject>>()) {
                        Debug.WriteProcessIndent += 1;
                        // Debug.WriteProcess($"^-{fieldType.Name} ({fieldType.Type.Name})");
                        AddItemsRecursively( fieldObject );
                        Debug.WriteProcessIndent -= 1;
                    }

                    break;
                }

                case HkTypeFormat.Array:
                {
                    var array = obj.GetValue<HkArray, IReadOnlyList<IHkObject>>();

                    if ( array.Count == 0 )
                        return;

                    if ( !obj.Type.IsFixedSize )
                        AddItem( new Item( obj.Type.SubType, array ) );

                    Debug.WriteProcessIndent += 1;
                    if (array.Count > 1000) {
                        Debug.WriteProcess($"Write Array: {obj.Type} ({array.Count})");
                    }
                    foreach (var childObject in array) {
                        AddItemsRecursively( childObject );
                    }
                    Debug.WriteProcessIndent -= 1;

                    break;
                }
                
                default:
                    AddItem( new Item( obj.Type, obj ) );
                    break;
            }

            void AddItem( Item item )
            {
                mIndexMap.Add( obj, mItems.Count );
                mItems.Add( item );
            }
        }

        private static int patchIdx = -1;
        private void AddPatch( HkType type, long position )
        {
            if (!mPatches.TryGetValue(type, out var positions)) {
                mPatches[ type ] = positions = new List<long>();
            }

            patchIdx++;
            if (patchIdx < 100) {
                Debug.WriteProcess($"AddPatch: {type.Name} real-pos {position}");
            }
            positions.Add( position );
        }

        public static void Write( Stream stream, bool leaveOpen, IHkObject rootObject, HkSdkVersion sdkVersion )
        {
            using ( var writer = new HkBinaryTagfileWriter( stream, leaveOpen, rootObject, sdkVersion ) )
            {
                writer.WriteTagSection();
            }
        }

        public static void Write( string filePath, IHkObject rootObject, HkSdkVersion sdkVersion )
        {
            Write( File.Create( filePath ), true, rootObject, sdkVersion );
        }

        private class Item
        {
            public Item( HkType type, IEnumerable<IHkObject> objects )
            {
                Type = type;
                Objects = new List<IHkObject>( objects );
            }

            public Item( HkType type, IHkObject obj )
            {
                Type = type;
                Objects = new List<IHkObject> { obj };
                IsPtr = true;
            }

            public HkType Type { get; }
            public List<IHkObject> Objects { get; }

            public bool IsPtr { get; }

            public long Position { get; set; }
        }
    }
}