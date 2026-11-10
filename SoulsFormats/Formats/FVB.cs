using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;

namespace SoulsFormats
{
    /// <summary>
    /// The fog volumes placed in a map. Associated with a <c>FogVolume ParamEditor</c> from <see cref="GPARAM"/>.
    /// Extension: .fvb
    /// </summary>
    public class FVB : SoulsFile<FVB>
    {
        /// <summary>
        /// 1 in ER
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The fog volumes in this file.
        /// </summary>
        public List<FogVolume> Volumes { get; set; }

        public FVB()
        {
            Version = 1;
            Volumes = new List<FogVolume>();
        }

        private static int VolumeSize(int version) => version >= 2 ? 0x70 : 0x60;

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;

            br.AssertInt32(1);
            Version = br.AssertInt32(1, 2);
            int count = br.ReadInt32();
            int namesLength = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(VolumeSize(Version));
            br.AssertPattern(0x24, 0x00);

            long namesStart = br.Position;
            br.Skip(namesLength);
            Volumes = new List<FogVolume>(count);
            for (int i = 0; i < count; i++)
                Volumes.Add(new FogVolume(br, namesStart, Version));
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = false;

            var names = new BinaryWriterEx(false);
            var offsets = new List<FogVolume.TableOffsets>(Volumes.Count);
            foreach (FogVolume volume in Volumes)
                offsets.Add(volume.WriteTable(names, Version));

            byte[] table = names.FinishBytes();
            bw.WriteInt32(1);
            bw.WriteInt32(Version);
            bw.WriteInt32(Volumes.Count);
            bw.WriteInt32(table.Length);
            bw.WriteInt32(0);
            bw.WriteInt32(VolumeSize(Version));
            bw.WritePattern(0x24, 0x00);
            bw.WriteBytes(table);

            for (int i = 0; i < Volumes.Count; i++)
                Volumes[i].Write(bw, offsets[i], Version);
        }

        private static void WriteName(BinaryWriterEx bw, string name)
        {
            long start = bw.Position;
            bw.WriteUTF16(name, true);
            bw.PadRelative(start, 16);
        }

        /// <summary>
        /// A box of fog.
        /// </summary>
        public class FogVolume
        {
            /// <summary>
            /// The property key the game reads as <see cref="MapRegionId"/>.
            /// </summary>
            public const int MapRegionKey = 0;

            /// <summary>
            /// Unknown sequence of random bytes prefixed with a timestamp. Probably a unique ID or some kind of provenance.
            /// </summary>
            public byte[] Id { get; set; }

            /// <summary>
            /// The volume's name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The GPARAM <c>FogVolume ParamEditor</c> group the volume is drawn with.
            /// </summary>
            public int GparamId { get; set; }

            /// <summary>
            /// Name of a single channel texture where each texel represents how high the fog reaches over a given point,
            /// relative to the transformed fog volume bounds.
            /// </summary>
            public string HeightMapName { get; set; }

            /// <summary>
            /// Unknown. A second mask texture associated with cloud volumes. Only present in Version 2.
            /// </summary>
            public string Unk30TextureName { get; set; }

            /// <summary>
            /// The center point of the box.
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Rotation of the box, in radians
            /// </summary>
            public Vector3 Rotation { get; set; }

            /// <summary>
            /// Size of the box from the center point to the edge on each axis.
            /// </summary>
            public Vector3 HalfExtents { get; set; }

            /// <summary>
            /// A bag of properties associated with each fog volume. Only <see cref="MapRegionKey"/> is used in ELDEN RING.
            /// </summary>
            public Dictionary<int, object> Properties { get; set; }

            public int MapRegionId
            {
                get => Properties.TryGetValue(MapRegionKey, out object value) && value is int region ? region : -1;
                set => Properties[MapRegionKey] = value;
            }

            /// <summary>
            /// Creates a new fog volume.
            /// </summary>
            public FogVolume()
            {
                Name = "";
                HeightMapName = "";
                Unk30TextureName = "";
                HalfExtents = Vector3.One;
                Properties = new Dictionary<int, object> { [MapRegionKey] = -1 };
            }

            public FogVolume DeepCopy()
            {
                var copy = (FogVolume)MemberwiseClone();
                copy.Properties = new Dictionary<int, object>(Properties);
                return copy;
            }

            internal FogVolume(BinaryReaderEx br, long namesStart, int version)
            {
                Id = br.ReadBytes(16);
                long nameOffset = br.ReadInt64();
                br.AssertInt32(0);
                br.AssertInt32(1); 
                GparamId = br.ReadInt32();
                br.AssertInt32(0);
                long heightMapOffset = br.ReadInt64();
                long unk30TextureOffset = 0;
                if (version >= 2)
                {
                    unk30TextureOffset = br.ReadInt64();
                    br.AssertInt64(0);
                }
                Position = br.ReadVector3();
                Rotation = br.ReadVector3();
                HalfExtents = br.ReadVector3();
                int propertyCount = br.ReadInt32();
                long propertiesOffset = br.ReadInt64();

                Name = br.GetUTF16(namesStart + nameOffset);
                HeightMapName = br.GetUTF16(namesStart + heightMapOffset);
                if (unk30TextureOffset != 0)
                {
                    Unk30TextureName = br.GetUTF16(namesStart + unk30TextureOffset);
                }

                Properties = new Dictionary<int, object>(propertyCount);
                br.StepIn(namesStart + propertiesOffset);
                for (int i = 0; i < propertyCount; i++)
                {
                    int key = br.ReadInt32();
                    var type = br.ReadEnum32<PropertyType>();
                    long valueOffset = namesStart + br.ReadInt64();
                    Properties.TryAdd(key, type switch
                    {
                        PropertyType.Int => br.GetInt32(valueOffset),
                        PropertyType.UInt => br.GetUInt32(valueOffset),
                        PropertyType.Float => br.GetSingle(valueOffset),
                        PropertyType.Bool => br.GetBoolean(valueOffset),
                        _ => br.GetUTF16(valueOffset),
                    });
                }
                br.StepOut();
            }

            internal readonly record struct TableOffsets(long Name, long HeightMap, long Unk30Texture, long Properties);

            internal TableOffsets WriteTable(BinaryWriterEx names, int version)
            {
                long name = names.Position;
                WriteName(names, Name);
                long heightMap = 0;
                if (!string.IsNullOrEmpty(HeightMapName))
                {
                    heightMap = names.Position;
                    WriteName(names, HeightMapName);
                }

                long unk30Texture = 0;
                if (version >= 2 && !string.IsNullOrEmpty(Unk30TextureName))
                {
                    unk30Texture = names.Position;
                    WriteName(names, Unk30TextureName);
                }

                long properties = names.Position;
                foreach ((int key, object value) in Properties)
                {
                    names.WriteInt32(key);
                    names.WriteInt32((int)TypeOf(key, value));
                    names.ReserveInt64($"PropertyValue{properties}_{key}");
                }

                foreach ((int key, object value) in Properties)
                {
                    names.FillInt64($"PropertyValue{properties}_{key}", names.Position);
                    switch (value)
                    {
                        case int i: names.WriteInt32(i); break;
                        case uint u: names.WriteUInt32(u); break;
                        case float f: names.WriteSingle(f); break;
                        case bool b: names.WriteBoolean(b); break;
                        case string s: names.WriteUTF16(s, true); break;
                    }
                }

                return new TableOffsets(name, heightMap, unk30Texture, properties);
            }

            internal void Write(BinaryWriterEx bw, TableOffsets offsets, int version)
            {
                bw.WriteBytes(Id);
                bw.WriteInt64(offsets.Name);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(GparamId);
                bw.WriteInt32(0);
                bw.WriteInt64(offsets.HeightMap);
                if (version >= 2)
                {
                    bw.WriteInt64(offsets.Unk30Texture);
                    bw.WriteInt64(0);
                }
                bw.WriteVector3(Position);
                bw.WriteVector3(Rotation);
                bw.WriteVector3(HalfExtents);
                bw.WriteInt32(Properties.Count);
                bw.WriteInt64(offsets.Properties);
            }

            public override string ToString() => $"{Name}";

            /// <summary>The type tag a property value is stored with.</summary>
            private enum PropertyType : int
            {
                Int = 0,
                UInt = 1,
                Float = 2,
                Bool = 3,
                String = 4,
            }

            private static PropertyType TypeOf(int key, object value) => value switch
            {
                int => PropertyType.Int,
                uint => PropertyType.UInt,
                float => PropertyType.Float,
                bool => PropertyType.Bool,
                string => PropertyType.String,
                _ => throw new InvalidOperationException($"Fog volume property {key} is not supported."),
            };
        }
    }
}
