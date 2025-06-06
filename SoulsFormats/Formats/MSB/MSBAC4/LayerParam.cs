﻿using System.Collections.Generic;

namespace SoulsFormats
{
    public partial class MSBAC4
    {
        /// <summary>
        /// Layers which parts can selectively be enabled or disabled on.
        /// </summary>
        public class LayerParam : Param<Layer>, IMsbParam<Layer>
        {
            /// <summary>
            /// The available layers to use.
            /// </summary>
            public List<Layer> Layers { get; set; }

            /// <summary>
            /// Creates an empty LayerParam with the default version.
            /// </summary>
            public LayerParam() : base(10001002, "LAYER_PARAM_ST")
            {
                Layers = new List<Layer>();
            }

            /// <summary>
            /// Adds a <see cref="Layer"/> to the appropriate list for its type and returns it.
            /// </summary>
            public Layer Add(Layer layer)
            {
                Layers.Add(layer);
                return layer;
            }

            public override List<Layer> GetEntries() => Layers;
            IReadOnlyList<Layer> IMsbParam<Layer>.GetEntries() => GetEntries();

            internal override Layer ReadEntry(BinaryReaderEx br, int version)
            {
                return Layers.EchoAdd(new Layer(br));
            }
        }

        /// <summary>
        /// A layer that parts can selectively be enabled or disabled on.
        /// </summary>
        public class Layer : ParamEntry, IMsbEntry
        {
            /// <summary>
            /// The ID of this layer to identify it.
            /// </summary>
            public int LayerID { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk08 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk0C { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk10 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk14 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk18 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk1C { get; set; }

            /// <summary>
            /// Creates a new layer with default values.
            /// </summary>
            public Layer()
            {
                Name = "normal";
                LayerID = 0;
                Unk08 = 0;
                Unk10 = 0;
                Unk14 = 0;
                Unk18 = 0;
                Unk1C = 0;
            }

            /// <summary>
            /// Creates a new layer with a name and default values.
            /// </summary>
            /// <param name="name">The name of the Layer.</param>
            public Layer(string name)
            {
                Name = name;
                LayerID = 0;
                Unk08 = 0;
                Unk10 = 0;
                Unk14 = 0;
                Unk18 = 0;
                Unk1C = 0;
            }

            /// <summary>
            /// Create a new layer with a name, layer ID, and default values.
            /// </summary>
            /// <param name="name">The name of the Layer.</param>
            /// <param name="layerID">The ID that identifies this Layer in parts.</param>
            public Layer(string name, int layerID)
            {
                Name = name;
                LayerID = layerID;
                Unk08 = 0;
                Unk10 = 0;
                Unk14 = 0;
                Unk18 = 0;
                Unk1C = 0;
            }

            /// <summary>
            /// Reads a layer from a stream.
            /// </summary>
            internal Layer(BinaryReaderEx br)
            {
                long start = br.Position;

                int nameOffset = br.ReadInt32();
                LayerID = br.ReadInt32();
                Unk08 = br.ReadInt32();
                Unk0C = br.ReadInt32();
                Unk10 = br.ReadInt32();
                Unk14 = br.ReadInt32();
                Unk18 = br.ReadInt32();
                Unk1C = br.ReadInt32();

                br.Position = start + nameOffset;
                Name = br.ReadShiftJIS();
            }

            internal override void Write(BinaryWriterEx bw, int version, int id)
            {
                long start = bw.Position;

                bw.ReserveInt32("NameOffset");
                bw.WriteInt32(LayerID);
                bw.WriteInt32(Unk08);
                bw.WriteInt32(Unk0C);
                bw.WriteInt32(Unk10);
                bw.WriteInt32(Unk14);
                bw.WriteInt32(Unk18);
                bw.WriteInt32(Unk1C);

                bw.FillInt32("NameOffset", (int)(bw.Position - start));
                bw.WriteShiftJIS(MSB.ReambiguateName(Name), true);
                bw.Pad(4);
            }

            /// <summary>
            /// Creates a deep copy of the <see cref="Layer"/>.
            /// </summary>
            public Layer DeepCopy()
            {
                return (Layer)MemberwiseClone();
            }
        }
    }
}
