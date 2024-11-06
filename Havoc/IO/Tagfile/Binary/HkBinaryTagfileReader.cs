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
    public class HkBinaryTagfileReader : IDisposable {
        private readonly string CompendiumPath;
        private readonly byte[] CompendiumBytes;
        private List<ulong> mCompendiumIDs;

        private readonly bool mLeaveOpen;
        private readonly BinaryReader mReader;
        private readonly Stream mStream;
        private long mDataOffset;
        private List<Item> mItems;
        private List<HkType> mTypes;
        private Dictionary<HkType, uint[]> mPatches = new ();
        private Dictionary<HkType, int> mCurrentPatches = new ();

        private HkBinaryTagfileReader( Stream stream, string compendium, bool leaveOpen )
        {
            CompendiumPath = compendium;
            mStream = stream;
            mReader = new BinaryReader( mStream, Encoding.UTF8, true );
            mLeaveOpen = leaveOpen;
        }

        private HkBinaryTagfileReader(Stream stream, byte[] compendium, bool leaveOpen)
        {
            CompendiumBytes = compendium;
            mStream = stream;
            mReader = new BinaryReader(mStream, Encoding.UTF8, true);
            mLeaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            if ( !mLeaveOpen )
                mStream.Dispose();

            mReader.Dispose();
        }

        private void ReadTagSection( HkSection section )
        {
            foreach ( var subSection in section.SubSections )
            {
                Debug.ReadProcess("Read section: " + subSection.Signature);
                switch ( subSection.Signature )
                {
                    case "TCRF": {
                        mStream.Seek( subSection.Position, SeekOrigin.Begin );
                        var compId = mReader.ReadUInt64();
                        // Read 8 as Compendium ID
                        if (string.IsNullOrWhiteSpace(CompendiumPath) && CompendiumBytes == null) {
                            throw new InvalidDataException("TCRF found but Compendium is empty");
                        }

                        if (!mCompendiumIDs.Contains(compId)) {
                            throw new InvalidDataException($"TCRF ref comp id {compId} but not found");
                        }
                        break;
                    }
                    case "SDKV":
                    {
                        mStream.Seek( subSection.Position, SeekOrigin.Begin );

                        string sdkVersion = mReader.ReadString( 8 );
                        if ( !HkSdkVersion.SupportedSdkVersions.Contains( new HkSdkVersion( sdkVersion ) ) )
                            throw new NotSupportedException( $"Unsupported SDK version: {sdkVersion}" );
                        break;
                    }

                    case "DATA":
                        mDataOffset = subSection.Position;
                        break;

                    case "TYPE":
                        //if (!string.IsNullOrWhiteSpace(CompendiumPath) || CompendiumBytes != null) {
                        //    break;
                        //    // throw new InvalidDataException("Types found in HKX, but expected to be in Compendium)");
                        //}
                        ReadTypeSection( subSection );
                        break;

                    case "INDX":
                        ReadIndexSection( subSection );
                        break;

                    default:
                        throw new InvalidDataException( $"Unexpected signature: {subSection.Signature}" );
                }}
        }

        private void ReadTypeCompendiumSection( HkSection section )
        {
            // Very barebones
            foreach ( var subSection in section.SubSections )
                switch ( subSection.Signature )
                {
                    case "TCID":
                        ReadIDsSection( subSection );
                        break;

                    case "TYPE":
                        ReadTypeSection( subSection );
                        break;

                    default:
                        throw new InvalidDataException( $"Unexpected signature: {subSection.Signature}" );
                }
        }

        private void ReadIDsSection( HkSection section ) {
            mCompendiumIDs = new List<ulong>();
            if (section.Length % 8 != 0) {
                throw new InvalidDataException($"TCID length {section.Length} can't be mod by 8");
            }

            mReader.BaseStream.Seek( section.Position, SeekOrigin.Begin );
            for (int i = 0; i < section.Length / 8; i++) {
                mCompendiumIDs.Add(mReader.ReadUInt64());
            }
        }

        private void ReadTypeSection( HkSection section )
        {
            mTypes = HkBinaryTypeReader.ReadTypeSection( mReader, section );
        }

        private void ReadPatchSection(HkSection section) {
            mPatches = new Dictionary<HkType, uint[]>();
            
            mReader.BaseStream.Seek( section.Position, SeekOrigin.Begin );
            while (mReader.BaseStream.Position < section.Position + section.Length) {
                var typeIndex = mReader.ReadInt32();
                var type = mTypes[typeIndex];
                var count = mReader.ReadInt32();
                Debug.ReadProcess($"PTCH: type {type}({typeIndex}), count {count}");
                mPatches[type] = new uint[count];

                for (int i = 0; i < count; i++) {
                    var offset = mReader.ReadUInt32();
                    if (count < 10) {
                        Debug.ReadProcess($"PTCH:   offset {offset}");
                    }
                    mPatches[type][i] = offset;
                }
            }
            
        }

        private void ReadIndexSection( HkSection section )
        {
            foreach ( var subSection in section.SubSections )
                switch ( subSection.Signature )
                {
                    case "ITEM":
                    {
                        mStream.Seek( subSection.Position, SeekOrigin.Begin );

                        mItems = new List<Item>( ( int ) ( subSection.Length / 24 ) );
                        while ( mStream.Position < subSection.Position + subSection.Length )
                            mItems.Add( new Item( this ) );

                        break;
                    }

                    case "PTCH":
                        // ReadPatchSection(subSection);
                        break;

                    default:
                        throw new InvalidDataException( $"Unexpected signature: {subSection.Signature}" );
                }
        }

        private void ReadCompendium() {
            if (!string.IsNullOrWhiteSpace(CompendiumPath))
            {
                var compendiums = ReadCompendiums(CompendiumPath);
                mTypes = compendiums.mTypes;
                mCompendiumIDs = compendiums.mCompendiumIDs;
                // mCompendiumIDs.ForEach(Console.WriteLine);
            }
            else if (CompendiumBytes != null)
            {
                var compendiums = ReadCompendiums(CompendiumBytes);
                mTypes = compendiums.mTypes;
                mCompendiumIDs = compendiums.mCompendiumIDs;
                // mCompendiumIDs.ForEach(Console.WriteLine);
            }
        }

        private void ReadRootSection()
        {
            var section = new HkSection( mReader );
            switch ( section.Signature )
            {
                case "TAG0":
                    ReadTagSection( section );
                    break;

                case "TCM0":
                    ReadTypeCompendiumSection( section );
                    break;

                default:
                    throw new InvalidDataException( $"Unexpected signature: {section.Signature}" );
            }
        }

        public static List<IHkObject> ReadAllObjects( Stream source, string compendium = "", bool leaveOpen = false )
        {
            using ( var reader = new HkBinaryTagfileReader( source, compendium, leaveOpen ) )
            {
                // stuff
                reader.ReadCompendium();
                reader.ReadRootSection();
                // Console.WriteLine("FoundTypes: " + reader.mTypes.Count);
                
                Debug.Temporary($"items: {reader.mItems.Count} {reader.mItems.Sum(x=>x.Objects.Count)}");
                Debug.Temporary($"items: {reader.mItems[ 1 ].Objects.Count}");
                return reader.mItems.SelectMany(x=>x.Objects).ToList();
            }
        }

        public static List<IHkObject> ReadAllObjects( string filePath, string compendium = "" )
        {
            using ( var source = File.OpenRead( filePath ) )
            {
                return ReadAllObjects( source, compendium );
            }
        }

        public static IHkObject Read(Stream source, byte[] compendium, bool leaveOpen = false)
        {
            using (var reader = new HkBinaryTagfileReader(source, compendium, leaveOpen))
            {
                // stuff
                reader.ReadCompendium();
                reader.ReadRootSection();
                // Console.WriteLine("FoundTypes: " + reader.mTypes.Count);
                return reader.mItems[1].Objects[0];
            }
        }

        public static IHkObject Read(byte[] file, byte[] compendium = null)
        {
            using (var source = new MemoryStream(file))
            {
                return Read(source, compendium);
            }
        }

        public static IHkObject Read( Stream source, string compendium = "", bool leaveOpen = false )
        {
            using ( var reader = new HkBinaryTagfileReader( source, compendium, leaveOpen ) )
            {
                // stuff
                reader.ReadCompendium();
                reader.ReadRootSection();
                // Console.WriteLine("FoundTypes: " + reader.mTypes.Count);
                
                Debug.Temporary($"items: {reader.mItems.Count} {reader.mItems.Sum(x=>x.Objects.Count)}");
                Debug.Temporary($"items: {reader.mItems[ 1 ].Objects.Count}");
                return reader.mItems[ 1 ].Objects[ 0 ];
            }
        }

        public static IHkObject Read( string filePath, string compendium = "" )
        {
            using ( var source = File.OpenRead( filePath ) )
            {
                return Read( source, compendium );
            }
        }

        public static HkBinaryTagfileReader ReadCompendiums(Stream source, bool leaveOpen = false)
        {
            using (var reader = new HkBinaryTagfileReader(source, "", leaveOpen))
            {
                // stuff
                reader.ReadRootSection();
                return reader;
            }
        }

        public static HkBinaryTagfileReader ReadCompendiums(byte[] source, bool leaveOpen = false)
        {
            using (var ms = new MemoryStream(source))
            {
                using (var reader = new HkBinaryTagfileReader(ms, "", leaveOpen))
                {
                    // stuff
                    reader.ReadRootSection();
                    return reader;
                }
            }
        }

        public static HkBinaryTagfileReader ReadCompendiums(string compendium)
        {
            using ( var source = File.OpenRead( compendium ) )
            {
                return ReadCompendiums( source );
            }
        }

        public void BackportTypesTo2012() {
            void LimitVersion(HkType type, int maxVer) {
                if (type != null && type.Version > maxVer) {
                    type.mVersion = maxVer;
                }
            }

            foreach (var type in mTypes) {
                var toRemoveTypes = new string[]
                {
                    "hkDefaultPropertyBag",
                    "hkHash",
                    "hkTuple",
                    "hkPropertyId",
                    "hkPtrAndInt",
                    "hkPropertyDesc",
                };
                mTypes.RemoveAll(x => toRemoveTypes.Contains(x.Name));
                
                if (type.Name == "hkReferencedObject") {
                    LimitVersion(type, 0);
                    type.mFields.RemoveAll(x => x.Name == "propertyBag");
                    type.mFields.ForEach(x => {
                        if (x.Name == "refCount") {
                            x.Name = "referenceCount";
                        }
                    });
                }
                
                if (type.Name == "hkxMeshSection") {
                    LimitVersion(type, 4);
                    type.mFields.RemoveAll(x => x.Name == "boneMatrixMap");
                }

                if (type.Name == "hkxVertexBuffer::VertexData") {
                    LimitVersion(type, 0);
                }
                
                if (type.Name == "hkxVertexDescription::ElementDecl") {
                    LimitVersion(type, 3);
                    type.mFields.RemoveAll(x => x.Name == "channelID");
                }

                if (type.Name == "hkxMaterial") {
                    LimitVersion(type, 4);
                    type.mFields.RemoveAll(x => x.Name == "userData");
                }

                if (type.Name == "hkaSkeleton") {
                    LimitVersion(type, 5);
                }

                if (type.Name == "hkcdStaticMeshTreeBase") {
                    LimitVersion(type, 0);
                    type.mFields.RemoveAll(x => x.Name == "primitiveStoresIsFlatConvex");
                }

                if (type.Name == "hkaInterleavedUncompressedAnimation") {
                    LimitVersion(type, 0);
                }

                if (type.Name == "hkpStaticCompoundShape") {
                    // TODO:
                    // type.mFields.ForEach(x => {
                    //     if (x.Name == "numBitsForChildShapeKey") {
                    //         
                    //     }
                    // });
                }

                if (type.Name == "hkpStaticCompoundShape::Instance") {
                    LimitVersion(type, 0);
                }

            }
        }

        private class Item
        {
            private readonly HkBinaryTagfileReader mTag;

            private List<IHkObject> mObjects;

            public Item( HkBinaryTagfileReader tag )
            {
                mTag = tag;

                int typeIndex = mTag.mReader.ReadInt32() & 0xFFFFFF;
                Type = typeIndex == 0 ? null : tag.mTypes[ typeIndex - 1 ];
                Position = mTag.mReader.ReadUInt32() + tag.mDataOffset;
                Count = mTag.mReader.ReadInt32();
            }

            public HkType Type { get; }
            private long Position { get; }
            private int Count { get; }
            public bool IsArray = false;

            public IReadOnlyList<IHkObject> Objects
            {
                get
                {
                    if ( mObjects == null )
                        ReadThisObject();

                    return mObjects;
                }
            }

            private void ReadThisObject()
            {
                if ( mObjects != null )
                    return;

                mObjects = new List<IHkObject>( Count );
                if (objIdx >= 40000000 && Count > 1 && Type.ToString() != "char") {
                    Debug.ReadProcess($"read item objects {Type}, length: {Count}");
                }

                for (int i = 0; i < Count; i++) {
                    var patchedOffset = Position + i * Type.ByteSize;
                    // if (mTag.mPatches.ContainsKey(Type)) {
                    //     if (!mTag.mCurrentPatches.ContainsKey(Type)) {
                    //         mTag.mCurrentPatches[Type] = 0;
                    //     }
                    //
                    //     var itemIdx = mTag.mCurrentPatches[Type];
                    //     mTag.mCurrentPatches[Type] += 1;
                    //     if (itemIdx < mTag.mPatches[Type].Length) {
                    //         // patchedOffset = mTag.mDataOffset + mTag.mPatches[Type][itemIdx];
                    //     }
                    // }

                    if (IsArray && (Type.ToString() == "hkStringPtr" || Type.IsPtr)) {
                        patchedOffset = Position + i * 4; // FIXME: hacky solution for array
                    }
                    
                    if (objIdx >= 40000000 && Type.ToString() != "char") {
                        Debug.ReadProcess($"read item obj idx {i} of type {Type}({Type.ByteSize}) (parent: {Type.ParentType}), isPtr? {Type.IsPtr}");
                    }

                    Debug.ReadProcessIndent ++;
                    mObjects.Add( ReadObject( Type, patchedOffset ) );
                    Debug.ReadProcessIndent --;
                }
            }

            private static Dictionary<string, bool> InfoLogged = new();
            private static int objIdx = -1;
            private IHkObject ReadObject( HkType type, long offset ) {
                objIdx++;
                mTag.mStream.Seek( offset, SeekOrigin.Begin );

                if (type.ToString() != "char" && objIdx >= 40000000) {
                    Debug.ReadProcess($"Read object index {objIdx} type {type}/{type.Format} offset {offset}, pos {mTag.mReader.BaseStream.Position}");
                }
                var info = Convert.ToString(type.FormatInfo, 2).PadLeft(24, '0');
                if (!InfoLogged.ContainsKey(type.Name + info) && type.FormatInfo > 0b00001000) {
                    InfoLogged[type.Name + info] = true;
                    // Debug.Temporary($"Type {type.Name} has format {type.Format} (info: {Convert.ToString(type.FormatInfo, 2)}), flag {type.Flags}");
                    // Debug.TypeDef($"Type {type.Name.PadRight(16, ' ')} {info} Format {type.Format }");
                    if (type.Format == HkTypeFormat.String) {
                        // Debug.TypeDef($"  IsFixedSize {type.IsFixedSize}");
                    }
                }

                // 
                
                switch ( type.Format )
                {
                    case HkTypeFormat.Void:
                        return new HkVoid( type );

                    case HkTypeFormat.Opaque:
                        return new HkOpaque( type );

                    case HkTypeFormat.Bool:
                    {
                        bool value;

                        switch ( type.BitCount )
                        {
                            case 8:
                                value = mTag.mReader.ReadByte() != 0;
                                break;

                            case 16:
                                value = mTag.mReader.ReadInt16() != 0;
                                break;

                            case 32:
                                value = mTag.mReader.ReadInt32() != 0;
                                break;

                            case 64:
                                value = mTag.mReader.ReadInt64() != 0;
                                break;

                            default:
                                throw new InvalidDataException( $"Unexpected bit count: {type.BitCount}" );
                        }

                        return new HkBool( type, value );
                    }

                    case HkTypeFormat.String:
                    {
                        string value;
                        if ( type.IsFixedSize )
                        {
                            value = mTag.mReader.ReadString( type.FixedSize );
                        }
                        else
                        {
                            var item = ReadItemIndex();
                            if ( item != null && item.Count > 0)
                            {
                                var stringBuilder = new StringBuilder( item.Count - 1 );
                                for ( int i = 0; i < item.Count - 1; i++ )
                                    {
                                        if (item[i].Value is sbyte)
                                        {
                                            stringBuilder.Append((char)unchecked((byte)(sbyte)item[i].Value));
                                        }
                                        else
                                        {
                                            stringBuilder.Append((char)(byte)item[i].Value);
                                        }
                                    }
                                    

                                value = stringBuilder.ToString();
                                if (objIdx >= 40000000) {
                                    Debug.ReadProcess($"Read object index {objIdx} string: {value}, pos {mTag.mReader.BaseStream.Position}");
                                }
                            }
                            else
                            {
                                value = null;
                                if (objIdx >= 40000000) {
                                    Debug.ReadProcess($"Read object index {objIdx} but null");
                                }
                            }
                        }

                        return new HkString( type, value );
                    }

                    case HkTypeFormat.Int:
                    {
                        switch ( type.BitCount )
                        {
                            case 8:
                                return type.IsSigned
                                    ? new HkSByte( type, mTag.mReader.ReadSByte() )
                                    : ( IHkObject ) new HkByte( type, mTag.mReader.ReadByte() );

                            case 16:
                                return type.IsSigned
                                    ? new HkInt16( type, mTag.mReader.ReadInt16() )
                                    : ( IHkObject ) new HkUInt16( type, mTag.mReader.ReadUInt16() );

                            case 32:
                                return type.IsSigned
                                    ? new HkInt32( type, mTag.mReader.ReadInt32() )
                                    : ( IHkObject ) new HkUInt32( type, mTag.mReader.ReadUInt32() );

                            case 64:
                                return type.IsSigned
                                    ? new HkInt64( type, mTag.mReader.ReadInt64() )
                                    : ( IHkObject ) new HkUInt64( type, mTag.mReader.ReadUInt64() );

                            default:
                                throw new InvalidDataException( $"Unexpected bit count: {type.BitCount}" );
                        }
                    }

                    case HkTypeFormat.FloatingPoint: {
                        return
                            type.IsHalf ? new HkHalf(type, mTag.mReader.ReadHalf()) :
                            type.IsSingle ? new HkSingle( type, mTag.mReader.ReadSingle() ) :
                            type.IsDouble ? ( IHkObject ) new HkDouble( type, mTag.mReader.ReadDouble() ) :
                            throw new InvalidDataException( "Unexpected floating point format" );
                    }
                    case HkTypeFormat.Ptr: {
                        var item = ReadItemIndex();

                        if (item == null || item.Count == 0) {
                            return new HkPtr(type, null);
                        }
                        return new HkPtr( type, item?[0] );
                    }

                    case HkTypeFormat.Class: {
                        return new HkClass( type,
                            type.AllFields.ToDictionary( x => x,
                                x => {
                                    var patchedOffset = offset + x.ByteOffset;
                                    return ReadObject(x.Type, patchedOffset);
                                }) );
                    }

                    case HkTypeFormat.Array:
                    {
                        if (objIdx >= 40000000) {
                            Debug.ReadProcess($"read array, is fixed size? {type.IsFixedSize}");
                        }
                        if ( !type.IsFixedSize )
                            return new HkArray( type, ReadItemIndex(true) );

                        var array = new IHkObject[ type.FixedSize ];
                        if (objIdx >= 40000000) {
                            Debug.ReadProcess($"read array, length: {array.Length}");
                        }
                        for ( int i = 0; i < array.Length; i++ )
                            array[ i ] = ReadObject( type.SubType, offset + i * type.SubType.ByteSize );

                        return new HkArray( type, array );
                    }

                    default:
                        throw new ArgumentOutOfRangeException( nameof( type.Format ) );
                }

                IReadOnlyList<IHkObject> ReadItemIndex(bool isArray = false)
                {
                    int index = mTag.mReader.ReadInt32();
                    if (index < 0) {
                        throw new Exception($"ReadItemIndex: {index} < 0");
                    } else if (index >= mTag.mItems.Count) {
                        throw new Exception($"ReadItemIndex: {index} > {mTag.mItems.Count}");
                    }

                    if (objIdx >= 40000000) {
                        Debug.ReadProcess($"ReadItemIndex: {index}");
                    }

                    mTag.mItems[index].IsArray = true;
                    return index == 0 ? null : mTag.mItems[ index ].Objects;
                }
            }
        }
    }
}