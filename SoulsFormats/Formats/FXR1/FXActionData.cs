using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoulsFormatsExtensions
{
    public partial class FXR1
    {
        public class ResourceEntry
        {
            [XmlAttribute]
            public int Unk;

            public FXField Data;
        }

        [XmlInclude(typeof(FXActionData27))]
        [XmlInclude(typeof(EmitterConeActionData28))]
        [XmlInclude(typeof(EmitterSquareActionData29))]
        [XmlInclude(typeof(EmitterCircleActionData30))]
        [XmlInclude(typeof(EmitterSphereActionData31))]
        [XmlInclude(typeof(EmitterBoxActionData32))]
        [XmlInclude(typeof(FXActionData40))]
        [XmlInclude(typeof(FXActionData43))]
        [XmlInclude(typeof(FXActionData55))]
        [XmlInclude(typeof(FXActionData59))]
        [XmlInclude(typeof(FXActionData61))]
        [XmlInclude(typeof(FXActionData66))]
        [XmlInclude(typeof(FXActionData70))]
        [XmlInclude(typeof(Particle2DActionData71))]
        [XmlInclude(typeof(FXActionData84))]
        [XmlInclude(typeof(FXActionData105))]
        [XmlInclude(typeof(FXActionData107))]
        [XmlInclude(typeof(Particle3DActionData108))]
        [XmlInclude(typeof(FXActionData117))]
        [XmlInclude(typeof(ActionDataRef))]
        public abstract class FXActionData : XIDable
        {
            public override bool ShouldSerializeXID() => FXR1.FlattenFXActionDatas;

            [XmlIgnore]
            public abstract int Type { get; }

            [XmlIgnore] // Set automatically during parent FXContainer's Write()
            public FXContainer ParentContainer;

            public List<ResourceEntry> Resources;

            [XmlIgnore]
            internal int DEBUG_SizeOnRead = -1;

            public virtual bool ShouldSerializeResources() => true;

            internal abstract void InnerRead(BinaryReaderEx br, FxrEnvironment env);
            internal abstract void InnerWrite(BinaryWriterEx bw, FxrEnvironment env);

            internal override void ToXIDs(FXR1 fxr)
            {
                ParentContainer = fxr.ReferenceFXContainer(ParentContainer);
                InnerToXIDs(fxr);
            }

            internal override void FromXIDs(FXR1 fxr)
            {
                ParentContainer = fxr.DereferenceFXContainer(ParentContainer);
                InnerFromXIDs(fxr);
            }

            internal virtual void InnerToXIDs(FXR1 fxr)
            {

            }

            internal virtual void InnerFromXIDs(FXR1 fxr)
            {

            }

            private FxrEnvironment currentWriteEnvironment = null;
            private Dictionary<FXField, List<long>> fieldWriteLocations = new Dictionary<FXField, List<long>>();

            internal void WriteField(FXField p)
            {
                WriteFXR1Varint(currentWriteEnvironment.bw, p.Type);

                if (p.IsEmptyPointer())
                {
                    currentWriteEnvironment.bw.WriteUInt32(0);
                }
                else
                {
                    if (!fieldWriteLocations.ContainsKey(p))
                        fieldWriteLocations.Add(p, new List<long>());

                    if (!fieldWriteLocations[p].Contains(currentWriteEnvironment.bw.Position))
                        fieldWriteLocations[p].Add(currentWriteEnvironment.bw.Position);

                    currentWriteEnvironment.RegisterPointerOffset(currentWriteEnvironment.bw.Position);

                    currentWriteEnvironment.bw.WriteUInt32(0xEEEEEEEE);
                }

                // garbage on end of offset to packed data
                WriteFXR1Garbage(currentWriteEnvironment.bw);
            }

            internal void Write(BinaryWriterEx bw, FxrEnvironment env)
            {
                env.RegisterOffset(bw.Position, this);

                long startPos = bw.Position;

                bw.WriteInt32(Type);
                bw.ReserveInt32("ActionData.Size");
                WriteFXR1Varint(bw, Resources.Count);

                env.RegisterPointerOffset(bw.Position);
                ReserveFXR1Varint(bw, "ActionData.Resources.Numbers");

                env.RegisterPointerOffset(bw.Position);
                ReserveFXR1Varint(bw, "ActionData.Resources.Datas");

                env.RegisterPointer(ParentContainer, useExistingPointerOnly: true, assertNotNull: true);

                fieldWriteLocations.Clear();
                currentWriteEnvironment = env;
                InnerWrite(bw, env);

                if (bw.VarintLong)
                    bw.Pad(8);

                FillFXR1Varint(bw, "ActionData.Resources.Numbers", (int)bw.Position);
                for (int i = 0; i < Resources.Count; i++)
                {
                    bw.WriteInt32(Resources[i].Unk);
                }

                FillFXR1Varint(bw, "ActionData.Resources.Datas", (int)bw.Position);
                for (int i = 0; i < Resources.Count; i++)
                {
                    WriteField(Resources[i].Data);
                }

                foreach (var kvp in fieldWriteLocations)
                {
                    long offsetOfThisField = bw.Position;

                    foreach (var location in kvp.Value)
                    {
                        bw.StepIn(location);
                        WriteFXR1Varint(bw, (int)offsetOfThisField);
                        bw.StepOut();
                    }

                    kvp.Key.InnerWrite(bw, env); 
                }

                int writtenSize = (int)(bw.Position - startPos);

                if (DEBUG_SizeOnRead != -1 && writtenSize != DEBUG_SizeOnRead)
                {
                    //throw new Exception("sdfsgfdsgfds");
                    //Console.WriteLine($"Warning: ActionDataType[{this.GetType().Name}] Read data size {DEBUG_SizeOnRead} but wrote {writtenSize} bytes.");
                }

                bw.FillInt32("ActionData.Size", writtenSize);

                bw.Pad(16); //Might be 16?

                fieldWriteLocations.Clear();
                currentWriteEnvironment = null;
            }

            internal static FXActionData Read(BinaryReaderEx br, FxrEnvironment env)
            {
                long startOffset = br.Position;

                int subType = br.ReadInt32();
                int size = br.ReadInt32();
                int preDataCount = ReadFXR1Varint(br);
                int offsetToResourceNumbers = ReadFXR1Varint(br);
                int offsetToResourceNodes = ReadFXR1Varint(br);

                int offsetToParentEffect = ReadFXR1Varint(br);
                var parentEffect = env.GetFXContainer(br, offsetToParentEffect);

                FXActionData data;

                switch (subType)
                {
                    case 27: data = new FXActionData27(); break;
                    case 28: data = new EmitterConeActionData28(); break;
                    case 29: data = new EmitterSquareActionData29(); break;
                    case 30: data = new EmitterCircleActionData30(); break;
                    case 31: data = new EmitterSphereActionData31(); break;
                    case 32: data = new EmitterBoxActionData32(); break;
                    case 40: data = new FXActionData40(); break;
                    case 43: data = new FXActionData43(); break;
                    case 55: data = new FXActionData55(); break;
                    case 59: data = new FXActionData59(); break;
                    case 61: data = new FXActionData61(); break;
                    case 66: data = new FXActionData66(); break;
                    case 70: data = new FXActionData70(); break;
                    case 71: data = new Particle2DActionData71(); break;
                    case 84: data = new FXActionData84(); break;
                    case 105: data = new FXActionData105(); break;
                    case 107: data = new FXActionData107(); break;
                    case 108: data = new Particle3DActionData108(); break;
                    case 117: data = new FXActionData117(); break;
                    default: throw new NotImplementedException();
                }

                env.RegisterOffset(startOffset, data);

                //Testing
                data.DEBUG_SizeOnRead = size;

                data.InnerRead(br, env);

                data.ParentContainer = parentEffect;

                data.Resources = new List<ResourceEntry>(preDataCount);

                br.StepIn(offsetToResourceNumbers);
                for (int i = 0; i < preDataCount; i++)
                {
                    data.Resources.Add(new ResourceEntry()
                    {
                        Unk = br.ReadInt32()
                    });
                }
                br.StepOut();

                br.StepIn(offsetToResourceNodes);
                for (int i = 0; i < preDataCount; i++)
                {
                    data.Resources[i].Data = FXField.Read(br, env);
                }
                br.StepOut();

                br.Position = startOffset + size;

                return data;
            }

            public class FXActionData27 : FXActionData
            {
                public override int Type => 27;

                public float Duration;
                public float DurationMult;
                public float DurationVariance;
                public float RenderDepth;
                public int TextureID;
                public int BlendMode;
                public FXField Scale;
                public FXField ScaleMult;
                public FXField Color1R;
                public FXField Color1G;
                public FXField Color1B;
                public FXField Color1A;
                public FXField Color2R;
                public FXField Color2G;
                public FXField Color2B;
                public FXField Color2A;
                public int Unk8;
                public int Unk9;
                public int Unk10;
                public float Unk11;
                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Duration = br.ReadSingle();
                    DurationMult = br.ReadSingle();
                    DurationVariance = br.ReadSingle();

                    br.AssertInt32(0);

                    RenderDepth = br.ReadSingle();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    TextureID = br.ReadInt32();
                    BlendMode = br.ReadInt32();

                    AssertFXR1Varint(br, 0);

                    Scale = FXField.Read(br, env);
                    ScaleMult = FXField.Read(br, env);
                    Color1R = FXField.Read(br, env);
                    Color1G = FXField.Read(br, env);
                    Color1B = FXField.Read(br, env);
                    Color1A = FXField.Read(br, env);
                    Color2R = FXField.Read(br, env);
                    Color2G = FXField.Read(br, env);
                    Color2B = FXField.Read(br, env);
                    Color2A = FXField.Read(br, env);

                    Unk8 = br.ReadInt32();
                    Unk9 = br.ReadInt32();
                    Unk10 = br.ReadInt32();
                    Unk11 = br.ReadSingle();

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                    
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Duration);
                    bw.WriteSingle(DurationMult);
                    bw.WriteSingle(DurationVariance);

                    bw.WriteInt32(0);

                    bw.WriteSingle(RenderDepth);

                    if (bw.VarintLong)
                        bw.WriteInt32(0);

                    bw.WriteInt32(TextureID);
                    bw.WriteInt32(BlendMode);

                    WriteFXR1Varint(bw, 0);

                    WriteField(Scale);
                    WriteField(ScaleMult);
                    WriteField(Color1R);
                    WriteField(Color1G);
                    WriteField(Color1B);
                    WriteField(Color1A);
                    WriteField(Color2R);
                    WriteField(Color2G);
                    WriteField(Color2B);
                    WriteField(Color2A);
                    bw.WriteInt32(Unk8);
                    bw.WriteInt32(Unk9);
                    bw.WriteInt32(Unk10);
                    bw.WriteSingle(Unk11);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }



            public class EmitterConeActionData28 : FXActionData
            {
                public override int Type => 28;

                public FXField EmitterDegree;
                public FXField EmitterSpread;
                public FXField EmitterSpeed;
                public int EmitterDirectionMode;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    EmitterDegree = FXField.Read(br, env);
                    EmitterSpread = FXField.Read(br, env);
                    EmitterSpeed = FXField.Read(br, env);
                    EmitterDirectionMode = ReadFXR1Varint(br);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(EmitterDegree);
                    WriteField(EmitterSpread);
                    WriteField(EmitterSpeed);
                    WriteFXR1Varint(bw, EmitterDirectionMode);
                }
            }


            public class EmitterSquareActionData29 : FXActionData
            {
                public override int Type => 29;

                public FXField EmitterSize;
                public FXField EmitterDegree;
                public FXField EmitterSpread;
                public FXField EmitterDistributionMode;
                public FXField EmitterSpeed;
                public int EmitterDirectionMode;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    EmitterSize = FXField.Read(br, env);
                    EmitterDegree = FXField.Read(br, env);
                    EmitterSpread = FXField.Read(br, env);
                    EmitterDistributionMode = FXField.Read(br, env);
                    EmitterSpeed = FXField.Read(br, env);
                    EmitterDirectionMode = br.ReadInt32();
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(EmitterSize);
                    WriteField(EmitterDegree);
                    WriteField(EmitterSpread);
                    WriteField(EmitterDistributionMode);
                    WriteField(EmitterSpeed);
                    bw.WriteInt32(EmitterDirectionMode);
                }
            }

            public class EmitterCircleActionData30 : FXActionData
            {
                public override int Type => 30;

                public FXField EmitterRadius;
                public FXField EmitterDegree;
                public FXField EmitterSpread;
                public FXField EmitterSpeed;
                public float EmitterDistributionMode;
                public int Unk3;
                public int EmitterDirectionMode;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    EmitterRadius = FXField.Read(br, env);
                    EmitterDegree = FXField.Read(br, env);
                    EmitterSpread = FXField.Read(br, env);
                    EmitterSpeed = FXField.Read(br, env);
                    EmitterDistributionMode = br.ReadSingle();
                    Unk3 = br.ReadInt32();
                    EmitterDirectionMode = ReadFXR1Varint(br);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(EmitterRadius);
                    WriteField(EmitterDegree);
                    WriteField(EmitterSpread);
                    WriteField(EmitterSpeed);
                    bw.WriteSingle(EmitterDistributionMode);
                    bw.WriteInt32(Unk3);
                    WriteFXR1Varint(bw, EmitterDirectionMode);
                }
            }


            public class EmitterSphereActionData31 : FXActionData
            {
                public override int Type => 31;

                public FXField EmitterRadius;
                public FXField EmitterDegree;
                public FXField EmitterSpread;
                public FXField EmitterSpeed;
                public int EmitterDistributionMode;
                public int EmitterMovementMode;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    EmitterRadius = FXField.Read(br, env);
                    EmitterDegree = FXField.Read(br, env);
                    EmitterSpread = FXField.Read(br, env);
                    EmitterSpeed = FXField.Read(br, env);
                    EmitterDistributionMode = br.ReadInt32();
                    EmitterMovementMode = br.ReadInt32();
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(EmitterRadius);
                    WriteField(EmitterDegree);
                    WriteField(EmitterSpread);
                    WriteField(EmitterSpeed);
                    bw.WriteInt32(EmitterDistributionMode);
                    bw.WriteInt32(EmitterMovementMode);
                }
            }

            public class EmitterBoxActionData32 : FXActionData
            {
                public override int Type => 32;

                public FXField EmitterSizeX;
                public FXField EmitterSizeY;
                public FXField EmitterSizeZ;
                public FXField EmitterDegree;
                public FXField EmitterSpread;
                public FXField EmitterSpeed;
                public int EmitterDistributionMode;
                public int EmitterMovementMode;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    EmitterSizeX = FXField.Read(br, env);
                    EmitterSizeY = FXField.Read(br, env);
                    EmitterSizeZ = FXField.Read(br, env);
                    EmitterDegree = FXField.Read(br, env);
                    EmitterSpread = FXField.Read(br, env);
                    EmitterSpeed = FXField.Read(br, env);
                    EmitterDistributionMode = br.ReadInt32();
                    EmitterMovementMode = br.ReadInt32();
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(EmitterSizeX);
                    WriteField(EmitterSizeY);
                    WriteField(EmitterSizeZ);
                    WriteField(EmitterDegree);
                    WriteField(EmitterSpread);
                    WriteField(EmitterSpeed);
                    bw.WriteInt32(EmitterDistributionMode);
                    bw.WriteInt32(EmitterMovementMode);
                }
            }

            public class FXActionData40 : FXActionData
            {
                public override int Type => 40;

                public float DS1R_Unk0;
                public float Duration;
                public int TextureID;
                public int OrientationMode;
                public int BlendMode;
                public int TrailThickness;
                public FXField Scale1X;
                public FXField Scale1Y;
                public FXField Scale2X;
                public FXField Scale2Y;
                public float Unk7;
                public float TrailMaxTime;
                public int TrailLength;
                public int Unk10;
                public FXField ColorR;
                public FXField ColorG;
                public FXField ColorB;
                public FXField ColorA;
                public int Unk12;
                public int Unk13;
                public FXField Unk14;
                public int Unk15;
                public float Unk16;
                public FXField Unk17_1;
                public FXField Unk17_2;
                public int Unk18;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    DS1R_Unk0 = br.ReadSingle();
                    Duration = br.ReadSingle();
                    TextureID = br.ReadInt32();

                    br.AssertInt32(0);

                    OrientationMode = br.ReadInt32();
                    BlendMode = br.ReadInt32();
                    TrailThickness = ReadFXR1Varint(br);
                    Scale1X = FXField.Read(br, env);
                    Scale1Y = FXField.Read(br, env);
                    Scale2X = FXField.Read(br, env);
                    Scale2Y = FXField.Read(br, env);
                    Unk7 = br.ReadSingle();
                    TrailMaxTime = br.ReadSingle();
                    TrailLength = br.ReadInt32();
                    Unk10 = br.ReadInt32();

                    AssertFXR1Varint(br, 0);

                    ColorR = FXField.Read(br, env);
                    ColorG = FXField.Read(br, env);
                    ColorB = FXField.Read(br, env);
                    ColorA = FXField.Read(br, env);
                    Unk12 = br.ReadInt32();
                    Unk13 = br.ReadInt32();

                    AssertFXR1Varint(br, 0);

                    Unk14 = FXField.Read(br, env);
                    Unk15 = br.ReadInt32();
                    Unk16 = br.ReadSingle();
                    Unk17_1 = FXField.Read(br, env);
                    Unk17_2 = FXField.Read(br, env);
                    Unk18 = ReadFXR1Varint(br);

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(DS1R_Unk0);
                    bw.WriteSingle(Duration);
                    bw.WriteInt32(TextureID);

                    bw.WriteInt32(0);

                    bw.WriteInt32(OrientationMode);
                    bw.WriteInt32(BlendMode);
                    WriteFXR1Varint(bw, TrailThickness);
                    WriteField(Scale1X);
                    WriteField(Scale1Y);
                    WriteField(Scale2X);
                    WriteField(Scale2Y);
                    bw.WriteSingle(Unk7);
                    bw.WriteSingle(TrailMaxTime);
                    bw.WriteInt32(TrailLength);
                    bw.WriteInt32(Unk10);

                    WriteFXR1Varint(bw, 0);

                    WriteField(ColorR);
                    WriteField(ColorG);
                    WriteField(ColorB);
                    WriteField(ColorA);
                    bw.WriteInt32(Unk12);
                    bw.WriteInt32(Unk13);

                    WriteFXR1Varint(bw, 0);

                    WriteField(Unk14);
                    bw.WriteInt32(Unk15);
                    bw.WriteSingle(Unk16);
                    WriteField(Unk17_1);
                    WriteField(Unk17_2);
                    WriteFXR1Varint(bw, Unk18);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }

            public class FXActionData43 : FXActionData
            {
                public override int Type => 43;

                public float Unk1;
                public int TextureID1;
                public int TextureID2;
                public int DistortionMode;
                public int ShapeMode;
                public int OrientationMode;
                public int BlendMode;
                public FXField ScaleX;
                public FXField ScaleY;
                public FXField ScaleZ;
                public FXField StretchingDistance;
                public FXField DistortionIntensity;
                public FXField WobblingSpeed;
                public FXField WobblingRadius;
                public FXField WaveSpreed;
                public FXField FlowSpeed;
                public FXField ColorR;
                public FXField ColorG;
                public FXField ColorB;
                public FXField ColorA;
                public int Unk8;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1 = br.ReadSingle();
                    br.AssertInt32(0);
                    br.AssertInt32(0);
                    TextureID1 = br.ReadInt32();
                    TextureID2 = br.ReadInt32();
                    DistortionMode = br.ReadInt32();
                    ShapeMode = br.ReadInt32();
                    OrientationMode = br.ReadInt32();
                    BlendMode = ReadFXR1Varint(br);
                    ScaleX = FXField.Read(br, env);
                    ScaleY = FXField.Read(br, env);
                    ScaleZ = FXField.Read(br, env);
                    StretchingDistance = FXField.Read(br, env);
                    DistortionIntensity = FXField.Read(br, env);
                    WobblingSpeed = FXField.Read(br, env);
                    WobblingRadius = FXField.Read(br, env);
                    WaveSpreed = FXField.Read(br, env);
                    FlowSpeed = FXField.Read(br, env);
                    ColorR = FXField.Read(br, env);
                    ColorG = FXField.Read(br, env);
                    ColorB = FXField.Read(br, env);
                    ColorA = FXField.Read(br, env);
                    Unk8 = ReadFXR1Varint(br);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk1);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                    bw.WriteInt32(TextureID1);
                    bw.WriteInt32(TextureID2);
                    bw.WriteInt32(DistortionMode);
                    bw.WriteInt32(ShapeMode);
                    bw.WriteInt32(OrientationMode);
                    WriteFXR1Varint(bw, BlendMode);
                    WriteField(ScaleX);
                    WriteField(ScaleY);
                    WriteField(ScaleZ);
                    WriteField(StretchingDistance);
                    WriteField(DistortionIntensity);
                    WriteField(WobblingSpeed);
                    WriteField(WobblingRadius);
                    WriteField(WaveSpreed);
                    WriteField(FlowSpeed);
                    WriteField(ColorR);
                    WriteField(ColorG);
                    WriteField(ColorB);
                    WriteField(ColorA);
                    WriteFXR1Varint(bw, Unk8);
                }
            }

            public class FXActionData55 : FXActionData
            {
                public override int Type => 55;

                public FXField Gravity;
                public FXField MovementFalloff;
                public FXField MovementFalloffMult;
                public float PhaseShift;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Gravity = FXField.Read(br, env);
                    MovementFalloff = FXField.Read(br, env);
                    MovementFalloffMult = FXField.Read(br, env);

                    br.AssertInt32(0);

                    PhaseShift = br.ReadSingle();
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(Gravity);
                    WriteField(MovementFalloff);
                    WriteField(MovementFalloffMult);

                    bw.WriteInt32(0);

                    bw.WriteSingle(PhaseShift);
                }
            }

            public class FXActionData59 : FXActionData
            {
                public override int Type => 59;

                public float Unk1;
                public int TextureID;
                public int OrientationMode;
                public int BlendMode;
                public FXField ScaleX;
                public FXField ScaleY;
                public FXField RotX;
                public FXField RotY;
                public FXField RotZ;
                public int AnimFrameSliceCountPerRow;
                public int AnimFrameTotalCount;
                public FXField AnimFramesPerSecond;
                public FXField Color1G;
                public FXField Color1B;
                public FXField Color1A;
                public FXField Color2R;
                public FXField Color2G;
                public FXField Color2B;
                public FXField Color2A;
                public int Unk8;
                public int Unk9;
                public int Unk10;
                public float Unk11;
                public int DS1R_Unk12;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1 = br.ReadSingle();

                    br.AssertInt32(0);

                    TextureID = br.ReadInt32();

                    br.AssertInt32(0);

                    OrientationMode = br.ReadInt32();
                    BlendMode = br.ReadInt32();
                    ScaleX = FXField.Read(br, env);
                    ScaleY = FXField.Read(br, env);
                    RotX = FXField.Read(br, env);
                    RotY = FXField.Read(br, env);
                    RotZ = FXField.Read(br, env);
                    AnimFrameSliceCountPerRow = br.ReadInt32();
                    AnimFrameTotalCount = br.ReadInt32();
                    AnimFramesPerSecond = FXField.Read(br, env);
                    Color1G = FXField.Read(br, env);
                    Color1B = FXField.Read(br, env);
                    Color1A = FXField.Read(br, env);
                    Color2R = FXField.Read(br, env);
                    Color2G = FXField.Read(br, env);
                    Color2B = FXField.Read(br, env);
                    Color2A = FXField.Read(br, env);
                    Unk8 = br.ReadInt32();
                    Unk9 = br.ReadInt32();

                    br.AssertInt32(0);

                    Unk10 = br.ReadInt32();
                    Unk11 = br.ReadSingle();

                    DS1R_Unk12 = br.ReadInt32();

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk1);

                    bw.WriteInt32(0);

                    bw.WriteInt32(TextureID);

                    bw.WriteInt32(0);

                    bw.WriteInt32(OrientationMode);
                    bw.WriteInt32(BlendMode);
                    WriteField(ScaleX);
                    WriteField(ScaleY);
                    WriteField(RotX);
                    WriteField(RotY);
                    WriteField(RotZ);
                    bw.WriteInt32(AnimFrameSliceCountPerRow);
                    bw.WriteInt32(AnimFrameTotalCount);
                    WriteField(AnimFramesPerSecond);
                    WriteField(Color1G);
                    WriteField(Color1B);
                    WriteField(Color1A);
                    WriteField(Color2R);
                    WriteField(Color2G);
                    WriteField(Color2B);
                    WriteField(Color2A);
                    bw.WriteInt32(Unk8);
                    bw.WriteInt32(Unk9);

                    bw.WriteInt32(0);

                    bw.WriteInt32(Unk10);
                    bw.WriteSingle(Unk11);

                    bw.WriteInt32(DS1R_Unk12);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }

            public class FXActionData61 : FXActionData
            {
                public override int Type => 61;

                public int TextureID;
                public int Unk1;
                public int Unk2;
                public int Unk3_1;
                public int Unk3_2;
                public FXField Unk4_1;
                public FXField Unk4_2;
                public FXField Unk4_3;
                public int Unk5;
                public float Unk6;
                public FXField Unk7;
                public int Unk8;
                public int Unk9;
                public FXField Unk10_1;
                public FXField Unk10_2;
                public FXField Unk10_3;
                public FXField Unk10_4;
                public FXField Unk10_5;
                public FXField Unk10_6;
                public FXField Unk10_7;
                public FXField Unk10_8;
                public FXField Unk10_9;
                public FXField Unk10_10;
                public int Unk11;
                public int Unk12;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertInt32(0);
                    br.AssertInt32(0);

                    TextureID = br.ReadInt32();
                    Unk1 = br.ReadInt32();
                    Unk2 = br.ReadInt32();
                    Unk3_1 = br.ReadInt32();
                    Unk3_2 = ReadFXR1Varint(br);
                    Unk4_1 = FXField.Read(br, env);
                    Unk4_2 = FXField.Read(br, env);
                    Unk4_3 = FXField.Read(br, env);

                    br.AssertInt32(0);
                    br.AssertInt32(0);

                    Unk5 = br.ReadInt32();
                    Unk6 = br.ReadSingle();

                    AssertFXR1Varint(br, 0);

                    Unk7 = FXField.Read(br, env);
                    Unk8 = br.ReadInt32();
                    Unk9 = br.ReadInt32();
                    Unk10_1 = FXField.Read(br, env);
                    Unk10_2 = FXField.Read(br, env);
                    Unk10_3 = FXField.Read(br, env);
                    Unk10_4 = FXField.Read(br, env);
                    Unk10_5 = FXField.Read(br, env);
                    Unk10_6 = FXField.Read(br, env);
                    Unk10_7 = FXField.Read(br, env);
                    Unk10_8 = FXField.Read(br, env);
                    Unk10_9 = FXField.Read(br, env);
                    Unk10_10 = FXField.Read(br, env);
                    Unk11 = br.ReadInt32();
                    Unk12 = br.ReadInt32();

                    br.AssertInt32(0);
                    br.AssertInt32(0);

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);

                    bw.WriteInt32(TextureID);
                    bw.WriteInt32(Unk1);
                    bw.WriteInt32(Unk2);
                    bw.WriteInt32(Unk3_1);
                    WriteFXR1Varint(bw, Unk3_2);
                    WriteField(Unk4_1);
                    WriteField(Unk4_2);
                    WriteField(Unk4_3);

                    bw.WriteInt32(0);
                    bw.WriteInt32(0);

                    bw.WriteInt32(Unk5);
                    bw.WriteSingle(Unk6);

                    WriteFXR1Varint(bw, 0);

                    WriteField(Unk7);
                    bw.WriteInt32(Unk8);
                    bw.WriteInt32(Unk9);
                    WriteField(Unk10_1);
                    WriteField(Unk10_2);
                    WriteField(Unk10_3);
                    WriteField(Unk10_4);
                    WriteField(Unk10_5);
                    WriteField(Unk10_6);
                    WriteField(Unk10_7);
                    WriteField(Unk10_8);
                    WriteField(Unk10_9);
                    WriteField(Unk10_10);
                    bw.WriteInt32(Unk11);
                    bw.WriteInt32(Unk12);

                    bw.WriteInt32(0);
                    bw.WriteInt32(0);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }


            public class FXActionData66 : FXActionData
            {
                public override int Type => 66;

                public float Unk0;
                public float Unk1;
                public float DS1R_Unk2;
                public int Unk3;
                public float Unk4;
                public int Unk5;

                public int DS1R_Unk5B;

                public FXField Unk6_1;
                public FXField Unk6_2;
                public FXField Unk6_3;
                public FXField Unk6_4;
                public FXField Unk6_5;
                public FXField Unk6_6;
                public FXField Unk6_7;
                public FXField Unk6_8;
                public FXField Unk6_9;
                public FXField Unk6_10;
                public FXField Unk6_11;
                public FXField Unk6_12;
                public FXField Unk6_13;
                public FXField Unk6_14;
                public FXField Unk6_15;
                public FXField Unk6_16;
                public FXField Unk6_17;
                public FXField Unk6_18;
                public FXField Unk6_19;
                public FXField Unk6_20;
                public FXField Unk6_21;
                public FXField Unk6_22;
                public FXField Unk6_23;
                public FXField Unk6_24;
                public FXField Unk6_25;
                public FXField Unk6_26;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk0 = br.ReadSingle();
                    Unk1 = br.ReadSingle();
                    DS1R_Unk2 = br.ReadSingle();
                    Unk3 = br.ReadInt32();
                    Unk4 = br.ReadSingle();
                    Unk5 = br.ReadInt32();

                    if (br.VarintLong)
                        DS1R_Unk5B = ReadFXR1Varint(br);

                    Unk6_1 = FXField.Read(br, env);
                    Unk6_2 = FXField.Read(br, env);
                    Unk6_3 = FXField.Read(br, env);
                    Unk6_4 = FXField.Read(br, env);
                    Unk6_5 = FXField.Read(br, env);
                    Unk6_6 = FXField.Read(br, env);
                    Unk6_7 = FXField.Read(br, env);
                    Unk6_8 = FXField.Read(br, env);
                    Unk6_9 = FXField.Read(br, env);
                    Unk6_10 = FXField.Read(br, env);
                    Unk6_11 = FXField.Read(br, env);
                    Unk6_12 = FXField.Read(br, env);
                    Unk6_13 = FXField.Read(br, env);
                    Unk6_14 = FXField.Read(br, env);
                    Unk6_15 = FXField.Read(br, env);
                    Unk6_16 = FXField.Read(br, env);
                    Unk6_17 = FXField.Read(br, env);
                    Unk6_18 = FXField.Read(br, env);
                    Unk6_19 = FXField.Read(br, env);
                    Unk6_20 = FXField.Read(br, env);
                    Unk6_21 = FXField.Read(br, env);
                    Unk6_22 = FXField.Read(br, env);
                    Unk6_23 = FXField.Read(br, env);
                    Unk6_24 = FXField.Read(br, env);
                    Unk6_25 = FXField.Read(br, env);
                    Unk6_26 = FXField.Read(br, env);

                    br.AssertInt32(0);

                    if (br.VarintLong)
                    {
                        br.AssertInt32(0);
                        DS1RData = DS1RExtraNodes.Read(br, env);
                    }
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk0);
                    bw.WriteSingle(Unk1);
                    bw.WriteSingle(DS1R_Unk2);
                    bw.WriteInt32(Unk3);
                    bw.WriteSingle(Unk4);
                    bw.WriteInt32(Unk5);

                    if (bw.VarintLong)
                        WriteFXR1Varint(bw, DS1R_Unk5B);

                    WriteField(Unk6_1);
                    WriteField(Unk6_2);
                    WriteField(Unk6_3);
                    WriteField(Unk6_4);
                    WriteField(Unk6_5);
                    WriteField(Unk6_6);
                    WriteField(Unk6_7);
                    WriteField(Unk6_8);
                    WriteField(Unk6_9);
                    WriteField(Unk6_10);
                    WriteField(Unk6_11);
                    WriteField(Unk6_12);
                    WriteField(Unk6_13);
                    WriteField(Unk6_14);
                    WriteField(Unk6_15);
                    WriteField(Unk6_16);
                    WriteField(Unk6_17);
                    WriteField(Unk6_18);
                    WriteField(Unk6_19);
                    WriteField(Unk6_20);
                    WriteField(Unk6_21);
                    WriteField(Unk6_22);
                    WriteField(Unk6_23);
                    WriteField(Unk6_24);
                    WriteField(Unk6_25);
                    WriteField(Unk6_26);

                    bw.WriteInt32(0);

                    if (bw.VarintLong)
                    {
                        bw.WriteInt32(0);
                        DS1RData.Write(bw, this);
                    }
                }
            }

            public class FXActionData70 : FXActionData
            {
                public override int Type => 70;

                public float Unk1;
                public float Unk2;
                public float Unk3;
                public int Unk4;
                public float Unk5;
                public int TextureID1;
                public int TextureID2;
                public int TextureID3;
                public int Unk6;
                public int Unk7;
                public int Unk8;
                public FXField Scale1X;
                public FXField Scale1Y;
                public FXField Scale2X;
                public FXField Scale2Y;
                public FXField Unk9_5;
                public FXField Unk9_6;
                public FXField Unk9_7;
                public FXField Unk9_8;
                public FXField Unk9_9;
                public FXField Unk9_10;
                public FXField Unk9_11;
                public FXField Unk9_12;
                public FXField Unk9_13;
                public FXField Unk9_14;
                public FXField Unk9_15;
                public FXField Unk9_16;
                public FXField Unk9_17;
                public FXField Unk9_18;
                public FXField Unk9_19;
                public FXField Unk9_20;
                public FXField Unk9_21;
                public FXField Unk9_22;
                public FXField Color1R;
                public FXField Color1G;
                public FXField Color1B;
                public FXField Color1A;
                public FXField Color2R;
                public FXField Color2G;
                public FXField Color2B;
                public FXField Color2A;
                public int Unk10;
                public int Unk11;
                public int Unk12;
                public int Unk13;
                public float Unk14;
                public int Unk15;
                public float Unk16;
                public int Unk17;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1 = br.ReadSingle();
                    Unk2 = br.ReadSingle();
                    Unk3 = br.ReadSingle();
                    Unk4 = br.ReadInt32();
                    Unk5 = br.ReadSingle();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    TextureID1 = br.ReadInt32();
                    TextureID2 = br.ReadInt32();
                    TextureID3 = br.ReadInt32();
                    Unk6 = br.ReadInt32();
                    Unk7 = br.ReadInt32();
                    Unk8 = br.ReadInt32();
                    Scale1X = FXField.Read(br, env);
                    Scale1Y = FXField.Read(br, env);
                    Scale2X = FXField.Read(br, env);
                    Scale2Y = FXField.Read(br, env);
                    Unk9_5 = FXField.Read(br, env);
                    Unk9_6 = FXField.Read(br, env);
                    Unk9_7 = FXField.Read(br, env);
                    Unk9_8 = FXField.Read(br, env);
                    Unk9_9 = FXField.Read(br, env);
                    Unk9_10 = FXField.Read(br, env);
                    Unk9_11 = FXField.Read(br, env);
                    Unk9_12 = FXField.Read(br, env);
                    Unk9_13 = FXField.Read(br, env);
                    Unk9_14 = FXField.Read(br, env);
                    Unk9_15 = FXField.Read(br, env);
                    Unk9_16 = FXField.Read(br, env);
                    Unk9_17 = FXField.Read(br, env);
                    Unk9_18 = FXField.Read(br, env);
                    Unk9_19 = FXField.Read(br, env);
                    Unk9_20 = FXField.Read(br, env);
                    Unk9_21 = FXField.Read(br, env);
                    Unk9_22 = FXField.Read(br, env);
                    Color1R = FXField.Read(br, env);
                    Color1G = FXField.Read(br, env);
                    Color1B = FXField.Read(br, env);
                    Color1A = FXField.Read(br, env);
                    Color2R = FXField.Read(br, env);
                    Color2G = FXField.Read(br, env);
                    Color2B = FXField.Read(br, env);
                    Color2A = FXField.Read(br, env);
                    Unk10 = br.ReadInt32();

                    br.AssertInt32(0);
                    br.AssertInt32(0);
                    br.AssertInt32(0);
                    br.AssertInt32(0);

                    if (br.VarintLong)
                    {
                        br.AssertInt32(0);
                        br.AssertInt32(0);
                        br.AssertInt32(0);
                        br.AssertInt32(0);
                    }

                    Unk11 = br.ReadInt32();
                    Unk12 = br.ReadInt32();
                    Unk13 = br.ReadInt32();
                    Unk14 = br.ReadSingle();
                    Unk15 = br.ReadInt32();
                    Unk16 = br.ReadSingle();
                    Unk17 = br.ReadInt32();

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk1);
                    bw.WriteSingle(Unk2);
                    bw.WriteSingle(Unk3);
                    bw.WriteInt32(Unk4);
                    bw.WriteSingle(Unk5);

                    if (bw.VarintLong)
                        bw.WriteInt32(0);

                    bw.WriteInt32(TextureID1);
                    bw.WriteInt32(TextureID2);
                    bw.WriteInt32(TextureID3);
                    bw.WriteInt32(Unk6);
                    bw.WriteInt32(Unk7);
                    bw.WriteInt32(Unk8);
                    WriteField(Scale1X);
                    WriteField(Scale1Y);
                    WriteField(Scale2X);
                    WriteField(Scale2Y);
                    WriteField(Unk9_5);
                    WriteField(Unk9_6);
                    WriteField(Unk9_7);
                    WriteField(Unk9_8);
                    WriteField(Unk9_9);
                    WriteField(Unk9_10);
                    WriteField(Unk9_11);
                    WriteField(Unk9_12);
                    WriteField(Unk9_13);
                    WriteField(Unk9_14);
                    WriteField(Unk9_15);
                    WriteField(Unk9_16);
                    WriteField(Unk9_17);
                    WriteField(Unk9_18);
                    WriteField(Unk9_19);
                    WriteField(Unk9_20);
                    WriteField(Unk9_21);
                    WriteField(Unk9_22);
                    WriteField(Color1R);
                    WriteField(Color1G);
                    WriteField(Color1B);
                    WriteField(Color1A);
                    WriteField(Color2R);
                    WriteField(Color2G);
                    WriteField(Color2B);
                    WriteField(Color2A);
                    bw.WriteInt32(Unk10);

                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);

                    if (bw.VarintLong)
                    {
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                    }

                    bw.WriteInt32(Unk11);
                    bw.WriteInt32(Unk12);
                    bw.WriteInt32(Unk13);
                    bw.WriteSingle(Unk14);
                    bw.WriteInt32(Unk15);
                    bw.WriteSingle(Unk16);
                    bw.WriteInt32(Unk17);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }

            public class Particle2DActionData71 : FXActionData
            {
                public override int Type => 71;

                public float Duration;
                public float Unk2;
                public float Unk3;
                public int Attached;
                public float RenderDepth;
                public int TextureID;
                public int Unk6; //this might be a mistake
                public int Unk7;
                public int Unk8;
                public int BlendMode;
                public int Unk10;
                public FXField Scale1X;
                public FXField Scale1Y;
                public FXField Scale2X;
                public FXField Scale2Y;
                public FXField RotX;
                public FXField RotY;
                public FXField RotZ;
                public FXField RotSpeedX;
                public FXField RotSpeedY;
                public FXField RotSpeedZ;
                public int AnimFrameSliceCountPerDimension;
                public int AnimFrameTotalCount;
                public FXField Unk14_1;
                public FXField AnimFrameSelection;
                public FXField Color1R;
                public FXField Color1G;
                public FXField Color1B;
                public FXField Color1A;
                public FXField Color2R;
                public FXField Color2G;
                public FXField Color2B;
                public FXField Color2A;

                public int DS1R_UnkA1;
                public int DS1R_UnkA2;
                public int DS1R_UnkA3;
                public int DS1R_UnkA4;

                public int Unk15;
                public int Unk16;
                public int Unk17;
                public int Unk18;
                public float Unk19;
                public int Unk20;
                public float Unk21;
                public int Unk22;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Duration = br.ReadSingle();
                    Unk2 = br.ReadSingle();
                    Unk3 = br.ReadSingle();
                    Attached = br.ReadInt32();
                    RenderDepth = br.ReadSingle();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    TextureID = br.ReadInt32();
                    Unk7 = br.ReadInt32();
                    Unk8 = br.ReadInt32();
                    BlendMode = br.ReadInt32();
                    Unk10 = br.ReadInt32();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    Scale1X = FXField.Read(br, env);
                    Scale1Y = FXField.Read(br, env);
                    Scale2X = FXField.Read(br, env);
                    Scale2Y = FXField.Read(br, env);
                    RotX = FXField.Read(br, env);
                    RotY = FXField.Read(br, env);
                    RotZ = FXField.Read(br, env);
                    RotSpeedX = FXField.Read(br, env);
                    RotSpeedY = FXField.Read(br, env);
                    RotSpeedZ = FXField.Read(br, env);

                    AnimFrameSliceCountPerDimension = br.ReadInt32();
                    AnimFrameTotalCount = br.ReadInt32();

                    Unk14_1 = FXField.Read(br, env);
                    AnimFrameSelection = FXField.Read(br, env);
                    Color1R = FXField.Read(br, env);
                    Color1G = FXField.Read(br, env);
                    Color1B = FXField.Read(br, env);
                    Color1A = FXField.Read(br, env);
                    Color2R = FXField.Read(br, env);
                    Color2G = FXField.Read(br, env);
                    Color2B = FXField.Read(br, env);
                    Color2A = FXField.Read(br, env);

                    if (br.VarintLong)
                    {
                        DS1R_UnkA1 = br.ReadInt32();
                        DS1R_UnkA2 = br.ReadInt32();
                        DS1R_UnkA3 = br.ReadInt32();
                        DS1R_UnkA4 = br.ReadInt32();
                    }

                    Unk15 = br.ReadInt32();

                    br.AssertInt32(0);
                    br.AssertInt32(0);
                    br.AssertInt32(0);
                    br.AssertInt32(0);

                    Unk16 = br.ReadInt32();
                    Unk17 = br.ReadInt32();
                    Unk18 = br.ReadInt32();
                    Unk19 = br.ReadSingle();
                    Unk20 = br.ReadInt32();
                    Unk21 = br.ReadSingle();
                    Unk22 = br.ReadInt32();

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Duration);
                    bw.WriteSingle(Unk2);
                    bw.WriteSingle(Unk3);
                    bw.WriteInt32(Attached);
                    bw.WriteSingle(RenderDepth);

                    if (bw.VarintLong)
                        bw.WriteInt32(0);

                    bw.WriteInt32(TextureID);
                    bw.WriteInt32(Unk7);
                    bw.WriteInt32(Unk8);
                    bw.WriteInt32(BlendMode);
                    bw.WriteInt32(Unk10);

                    if (bw.VarintLong)
                        bw.WriteInt32(0);

                    WriteField(Scale1X);
                    WriteField(Scale1Y);
                    WriteField(Scale2X);
                    WriteField(Scale2Y);
                    WriteField(RotX);
                    WriteField(RotY);
                    WriteField(RotZ);
                    WriteField(RotSpeedX);
                    WriteField(RotSpeedY);
                    WriteField(RotSpeedZ);

                    bw.WriteInt32(AnimFrameSliceCountPerDimension);
                    bw.WriteInt32(AnimFrameTotalCount);

                    WriteField(Unk14_1);
                    WriteField(AnimFrameSelection);
                    WriteField(Color1R);
                    WriteField(Color1G);
                    WriteField(Color1B);
                    WriteField(Color1A);
                    WriteField(Color2R);
                    WriteField(Color2G);
                    WriteField(Color2B);
                    WriteField(Color2A);

                    if (bw.VarintLong)
                    {
                        bw.WriteInt32(DS1R_UnkA1);
                        bw.WriteInt32(DS1R_UnkA2);
                        bw.WriteInt32(DS1R_UnkA3);
                        bw.WriteInt32(DS1R_UnkA4);
                    }

                    bw.WriteInt32(Unk15);

                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);

                    bw.WriteInt32(Unk16);
                    bw.WriteInt32(Unk17);
                    bw.WriteInt32(Unk18);
                    bw.WriteSingle(Unk19);
                    bw.WriteInt32(Unk20);
                    bw.WriteSingle(Unk21);
                    bw.WriteInt32(Unk22);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }

            public class FXActionData84 : FXActionData
            {
                public override int Type => 84;

                public FXField Unk1_1;
                public FXField Unk1_2;
                public FXField Unk1_3;
                public float Unk2;
                public FXField Unk3;
                public int Unk4;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1_1 = FXField.Read(br, env);
                    Unk1_2 = FXField.Read(br, env);
                    Unk1_3 = FXField.Read(br, env);
                    br.AssertInt32(0);
                    Unk2 = br.ReadSingle();
                    Unk3 = FXField.Read(br, env);
                    Unk4 = ReadFXR1Varint(br);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(Unk1_1);
                    WriteField(Unk1_2);
                    WriteField(Unk1_3);
                    bw.WriteInt32(0);
                    bw.WriteSingle(Unk2);
                    WriteField(Unk3);
                    WriteFXR1Varint(bw, Unk4);
                }
            }

            public class FXActionData105 : FXActionData
            {
                public override int Type => 105;

                public FXField Unk1_1;
                public FXField Unk1_2;
                public FXField Unk1_3;
                public float Unk2;
                public FXField Unk3;
                public int Unk4;
                public FXField Unk5;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1_1 = FXField.Read(br, env);
                    Unk1_2 = FXField.Read(br, env);
                    Unk1_3 = FXField.Read(br, env);
                    br.AssertInt32(0);
                    Unk2 = br.ReadSingle();
                    Unk3 = FXField.Read(br, env);
                    Unk4 = ReadFXR1Varint(br);
                    Unk5 = FXField.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(Unk1_1);
                    WriteField(Unk1_2);
                    WriteField(Unk1_3);
                    bw.WriteInt32(0);
                    bw.WriteSingle(Unk2);
                    WriteField(Unk3);
                    WriteFXR1Varint(bw, Unk4);
                    WriteField(Unk5);
                }
            }

            public class FXActionData107 : FXActionData
            {
                public override int Type => 107;

                public float Unk1;
                public int TextureID;
                public int Unk2;
                public FXField Unk3;
                public FXField Unk4;
                public FXField Unk5;
                public FXField Unk6;
                public FXField Unk7;
                public FXField Unk8;
                public FXField Unk9;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1 = br.ReadSingle();
                    br.AssertInt32(0);
                    TextureID = br.ReadInt32();
                    Unk2 = br.ReadInt32();
                    Unk3 = FXField.Read(br, env);
                    Unk4 = FXField.Read(br, env);
                    Unk5 = FXField.Read(br, env);
                    Unk6 = FXField.Read(br, env);
                    Unk7 = FXField.Read(br, env);
                    Unk8 = FXField.Read(br, env);
                    Unk9 = FXField.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk1);
                    bw.WriteInt32(0);
                    bw.WriteInt32(TextureID);
                    bw.WriteInt32(Unk2);
                    WriteField(Unk3);
                    WriteField(Unk4);
                    WriteField(Unk5);
                    WriteField(Unk6);
                    WriteField(Unk7);
                    WriteField(Unk8);
                    WriteField(Unk9);
                }
            }

            public class Particle3DActionData108 : FXActionData
            {
                public override int Type => 108;

                public float Unk1;
                public float Unk2;
                public float Unk3;
                public int Unk4;
                public float Unk5;
                public int ModelID;
                public int Unk6;
                public int Unk7;
                public int Unk8;
                public int DS1R_Unk8B;
                public FXField Scale1X;
                public FXField Scale1Y;
                public FXField Scale1Z;
                public FXField Scale2X;
                public FXField Scale2Y;
                public FXField Scale2Z;
                public FXField RotSpeedX;
                public FXField RotSpeedY;
                public FXField RotSpeedZ;
                public FXField RotVal2X;
                public FXField RotVal2Y;
                public FXField RotVal2Z;
                public int Unk9;
                public int Unk10;
                public FXField Unk11_1;
                public FXField Unk11_2;
                public FXField Unk11_3;
                public FXField Unk11_4;
                public FXField Unk11_5;
                public FXField Unk11_6;
                public FXField Color1R;
                public FXField Color1G;
                public FXField Color1B;
                public FXField Color1A;
                public FXField Color2R;
                public FXField Color2G;
                public FXField Color2B;
                public FXField Color2A;
                public int Unk12;
                public int Unk13;
                public int Unk14;
                public float Unk15;
                public int Unk16;

                public DS1RExtraNodes DS1RData;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1 = br.ReadSingle();
                    Unk2 = br.ReadSingle();
                    Unk3 = br.ReadSingle();
                    Unk4 = br.ReadInt32();
                    Unk5 = br.ReadSingle();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    ModelID = br.ReadInt32();
                    Unk6 = br.ReadInt32();
                    Unk7 = br.ReadInt32();
                    Unk8 = br.ReadInt32();
                    DS1R_Unk8B = ReadFXR1Varint(br);
                    Scale1X = FXField.Read(br, env);
                    Scale1Y = FXField.Read(br, env);
                    Scale1Z = FXField.Read(br, env);
                    Scale2X = FXField.Read(br, env);
                    Scale2Y = FXField.Read(br, env);
                    Scale2Z = FXField.Read(br, env);
                    RotSpeedX = FXField.Read(br, env);
                    RotSpeedY = FXField.Read(br, env);
                    RotSpeedZ = FXField.Read(br, env);
                    RotVal2X = FXField.Read(br, env);
                    RotVal2Y = FXField.Read(br, env);
                    RotVal2Z = FXField.Read(br, env);
                    Unk9 = br.ReadInt32();
                    Unk10 = br.ReadInt32();
                    Unk11_1 = FXField.Read(br, env);
                    Unk11_2 = FXField.Read(br, env);
                    Unk11_3 = FXField.Read(br, env);
                    Unk11_4 = FXField.Read(br, env);
                    Unk11_5 = FXField.Read(br, env);
                    Unk11_6 = FXField.Read(br, env);
                    Color1R = FXField.Read(br, env);
                    Color1G = FXField.Read(br, env);
                    Color1B = FXField.Read(br, env);
                    Color1A = FXField.Read(br, env);
                    Color2R = FXField.Read(br, env);
                    Color2G = FXField.Read(br, env);
                    Color2B = FXField.Read(br, env);
                    Color2A = FXField.Read(br, env);
                    Unk12 = br.ReadInt32();
                    Unk13 = br.ReadInt32();
                    Unk14 = br.ReadInt32();
                    Unk15 = br.ReadSingle();
                    Unk16 = ReadFXR1Varint(br);

                    if (br.VarintLong)
                        DS1RData = DS1RExtraNodes.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteSingle(Unk1);
                    bw.WriteSingle(Unk2);
                    bw.WriteSingle(Unk3);
                    bw.WriteInt32(Unk4);
                    bw.WriteSingle(Unk5);

                    if (bw.VarintLong)
                        bw.WriteInt32(0);

                    bw.WriteInt32(ModelID);
                    bw.WriteInt32(Unk6);
                    bw.WriteInt32(Unk7);
                    bw.WriteInt32(Unk8);
                    WriteFXR1Varint(bw, DS1R_Unk8B);
                    WriteField(Scale1X);
                    WriteField(Scale1Y);
                    WriteField(Scale1Z);
                    WriteField(Scale2X);
                    WriteField(Scale2Y);
                    WriteField(Scale2Z);
                    WriteField(RotSpeedX);
                    WriteField(RotSpeedY);
                    WriteField(RotSpeedZ);
                    WriteField(RotVal2X);
                    WriteField(RotVal2Y);
                    WriteField(RotVal2Z);
                    bw.WriteInt32(Unk9);
                    bw.WriteInt32(Unk10);
                    WriteField(Unk11_1);
                    WriteField(Unk11_2);
                    WriteField(Unk11_3);
                    WriteField(Unk11_4);
                    WriteField(Unk11_5);
                    WriteField(Unk11_6);
                    WriteField(Color1R);
                    WriteField(Color1G);
                    WriteField(Color1B);
                    WriteField(Color1A);
                    WriteField(Color2R);
                    WriteField(Color2G);
                    WriteField(Color2B);
                    WriteField(Color2A);
                    bw.WriteInt32(Unk12);
                    bw.WriteInt32(Unk13);
                    bw.WriteInt32(Unk14);
                    bw.WriteSingle(Unk15);
                    WriteFXR1Varint(bw, Unk16);

                    if (bw.VarintLong)
                        DS1RData.Write(bw, this);
                }
            }

            public class FXActionData117 : FXActionData
            {
                public override int Type => 117;

                public FXField Unk1_1;
                public FXField Unk1_2;
                public FXField Unk1_3;
                public FXField Unk1_4;
                public FXField Unk1_5;
                public FXField Unk1_6;
                public int Unk2;
                public int Unk3;
                public FXField Unk4;

                internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
                {
                    Unk1_1 = FXField.Read(br, env);
                    Unk1_2 = FXField.Read(br, env);
                    Unk1_3 = FXField.Read(br, env);
                    Unk1_4 = FXField.Read(br, env);
                    Unk1_5 = FXField.Read(br, env);
                    Unk1_6 = FXField.Read(br, env);
                    Unk2 = br.ReadInt32();
                    Unk3 = br.ReadInt32();
                    Unk4 = FXField.Read(br, env);
                }

                internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
                {
                    WriteField(Unk1_1);
                    WriteField(Unk1_2);
                    WriteField(Unk1_3);
                    WriteField(Unk1_4);
                    WriteField(Unk1_5);
                    WriteField(Unk1_6);
                    bw.WriteInt32(Unk2);
                    bw.WriteInt32(Unk3);
                    WriteField(Unk4);
                }
            }
        }


        public class ActionDataRef : FXActionData
        {
            public override int Type => -1;

            [XmlAttribute]
            public string ReferenceXID;

            public override bool ShouldSerializeResources() => false;

            public ActionDataRef(FXActionData refVal)
            {
                ReferenceXID = refVal?.XID;
            }

            public ActionDataRef()
            {

            }

            internal override void InnerRead(BinaryReaderEx br, FxrEnvironment env)
            {
                throw new InvalidOperationException("Cannot actually serialize a reference class.");
            }

            internal override void InnerWrite(BinaryWriterEx bw, FxrEnvironment env)
            {
                throw new InvalidOperationException("Cannot actually deserialize a reference class.");
            }
        }

    }
}
