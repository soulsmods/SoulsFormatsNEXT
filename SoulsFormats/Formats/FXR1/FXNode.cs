using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoulsFormatsExtensions
{
    public partial class FXR1
    {
        [XmlInclude(typeof(IntNode))]
        [XmlInclude(typeof(IntArrayNode))]
        [XmlInclude(typeof(IntSequenceNode3))]
        [XmlInclude(typeof(IntSequenceNode5))]
        [XmlInclude(typeof(IntSequenceNode6))]
        [XmlInclude(typeof(FloatNode))]
        [XmlInclude(typeof(IntSequenceNode9))]
        [XmlInclude(typeof(FloatSequenceNode11))]
        [XmlInclude(typeof(FloatSequenceNode12))]
        [XmlInclude(typeof(Float3SequenceNode13))]
        [XmlInclude(typeof(Float3SequenceNode14))]
        [XmlInclude(typeof(ColorSequenceNode19))]
        [XmlInclude(typeof(ColorSequenceNode20))]
        [XmlInclude(typeof(Color3SequenceNode21))]
        [XmlInclude(typeof(Color3SequenceNode22))]
        [XmlInclude(typeof(ColorSequenceNode27))]
        [XmlInclude(typeof(ColorSequenceNode28))]
        [XmlInclude(typeof(Color3SequenceNode29))]
        [XmlInclude(typeof(EffectCallNode))]
        [XmlInclude(typeof(ActionCallNode))]
        [XmlInclude(typeof(IntNode41))]
        [XmlInclude(typeof(IndexedIntNode))]
        [XmlInclude(typeof(IndexedIntArrayNode))]
        [XmlInclude(typeof(IndexedIntSequenceNode))]
        [XmlInclude(typeof(IndexedFloatNode))]
        [XmlInclude(typeof(IndexedEffectNode))]
        [XmlInclude(typeof(IndexedActionNode))]
        [XmlInclude(typeof(IndexedSoundIDNode))]
        [XmlInclude(typeof(SoundIDNode68))]
        [XmlInclude(typeof(SoundIDNode69))]
        [XmlInclude(typeof(TickNode))]
        [XmlInclude(typeof(IndexedTickNode))]
        [XmlInclude(typeof(Int2Node))]
        [XmlInclude(typeof(Float2Node))]
        [XmlInclude(typeof(Tick2Node))]
        [XmlInclude(typeof(IndexedTick2Node))]
        [XmlInclude(typeof(IntSequenceNode89))]
        [XmlInclude(typeof(FloatSequenceNodeNode91))]
        [XmlInclude(typeof(FloatSequenceNodeNode95))]
        [XmlInclude(typeof(IntNode111))]
        [XmlInclude(typeof(EmptyNode112))]
        [XmlInclude(typeof(EmptyNode113))]
        [XmlInclude(typeof(UnkIndexedValueNode114))]
        [XmlInclude(typeof(UnkIndexedValueNode115))]
        [XmlInclude(typeof(Node2Node120))]
        [XmlInclude(typeof(Node2Node121))]
        [XmlInclude(typeof(Node2Node122))]
        [XmlInclude(typeof(Node2Node123))]
        [XmlInclude(typeof(Node2Node124))]
        [XmlInclude(typeof(Node2Node126))]
        [XmlInclude(typeof(Node2Node127))]
        [XmlInclude(typeof(NodeNode128))]
        [XmlInclude(typeof(EmptyNode129))]
        [XmlInclude(typeof(EmptyNode130))]
        [XmlInclude(typeof(EmptyNode131))]
        [XmlInclude(typeof(EmptyNode132))]
        [XmlInclude(typeof(EffectCreateNode133))]
        [XmlInclude(typeof(EffectCreateNode134))]
        [XmlInclude(typeof(EmptyNode136))]
        [XmlInclude(typeof(FXNode137))]
        [XmlInclude(typeof(FXNode138))]
        [XmlInclude(typeof(FXNode139))]
        [XmlInclude(typeof(FXNode140))]
        [XmlInclude(typeof(FXNodeRef))]
        public abstract class FXNode : XIDable
        {
            public override bool ShouldSerializeXID() => FXR1.FlattenFXNodes;

            //public readonly long ID;
            //internal FXNode(long id)
            //{
            //    ID = id;
            //}
            internal abstract void ReadInner(BinaryReaderEx br, FxrEnvironment env);
            internal abstract void WriteInner(BinaryWriterEx bw, FxrEnvironment env);

            //long DEBUG_DataSizeOnRead = -1;

            internal override void ToXIDs(FXR1 fxr)
            {
                InnerToXIDs(fxr);
            }

            internal override void FromXIDs(FXR1 fxr)
            {
                InnerFromXIDs(fxr);
            }

            internal virtual void InnerToXIDs(FXR1 fxr)
            {

            }

            internal virtual void InnerFromXIDs(FXR1 fxr)
            {

            }

            internal void Write(BinaryWriterEx bw, FxrEnvironment env)
            {
                env.RegisterOffset(bw.Position, this);
                env.RegisterFXNodeOffsetHere();
                //long start = bw.Position;
                //Console.WriteLine($"TYPE: {GetType().Name}");
                WriteInner(bw, env);
                //long end = bw.Position;
                //long dataLength = end - start;
                //if (dataLength != DEBUG_DataSizeOnRead)
                //{
                //    Console.WriteLine($"Warning: NodeType[{GetType().Name}] Read {DEBUG_DataSizeOnRead} bytes of data but wrote {dataLength} bytes.");
                //}
            }

            internal static FXNode GetProperFXNodeType(BinaryReaderEx br, FxrEnvironment env)
            {
                long functionID = br.GetFXR1Varint(br.Position);
                FXNode func = null;
                switch (functionID)
                {
                    case 1: func = new IntNode(); break;
                    case 2: func = new IntArrayNode(); break;
                    case 3: func = new IntSequenceNode3(); break;
                    case 5: func = new IntSequenceNode5(); break;
                    case 6: func = new IntSequenceNode6(); break;
                    case 7: func = new FloatNode(); break;
                    case 9: func = new IntSequenceNode9(); break;
                    case 11: func = new FloatSequenceNode11(); break;
                    case 12: func = new FloatSequenceNode12(); break;
                    case 13: func = new Float3SequenceNode13(); break;
                    case 14: func = new Float3SequenceNode14(); break;
                    case 19: func = new ColorSequenceNode19(); break;
                    case 20: func = new ColorSequenceNode20(); break;
                    case 21: func = new Color3SequenceNode21(); break;
                    case 22: func = new Color3SequenceNode22(); break;
                    case 27: func = new ColorSequenceNode27(); break;
                    case 28: func = new ColorSequenceNode28(); break;
                    case 29: func = new Color3SequenceNode29(); break;
                    case 37: func = new EffectCallNode(); break;
                    case 38: func = new ActionCallNode(); break;
                    case 41: func = new IntNode41(); break;
                    case 44: func = new IndexedIntNode(); break;
                    case 45: func = new IndexedIntArrayNode(); break;
                    case 46: func = new IndexedIntSequenceNode(); break;
                    case 47: func = new IndexedFloatNode(); break;
                    case 59: func = new IndexedEffectNode(); break;
                    case 60: func = new IndexedActionNode(); break;
                    case 66: func = new IndexedSoundIDNode(); break;
                    case 68: func = new SoundIDNode68(); break;
                    case 69: func = new SoundIDNode69(); break;
                    case 70: func = new TickNode(); break;
                    case 71: func = new IndexedTickNode(); break;
                    case 79: func = new Int2Node(); break;
                    case 81: func = new Float2Node(); break;
                    case 85: func = new Tick2Node(); break;
                    case 87: func = new IndexedTick2Node(); break;
                    case 89: func = new IntSequenceNode89(); break;
                    case 91: func = new FloatSequenceNodeNode91(); break;
                    case 95: func = new FloatSequenceNodeNode95(); break;
                    case 111: func = new IntNode111(); break;
                    case 112: func = new EmptyNode112(); break;
                    case 113: func = new EmptyNode113(); break;
                    case 114: func = new UnkIndexedValueNode114(); break;
                    case 115: func = new UnkIndexedValueNode115(); break;
                    case 120: func = new Node2Node120(); break;
                    case 121: func = new Node2Node121(); break;
                    case 122: func = new Node2Node122(); break;
                    case 123: func = new Node2Node123(); break;
                    case 124: func = new Node2Node124(); break;
                    case 126: func = new Node2Node126(); break;
                    case 127: func = new Node2Node127(); break;
                    case 128: func = new NodeNode128(); break;
                    case 129: func = new EmptyNode129(); break;
                    case 130: func = new EmptyNode130(); break;
                    case 131: func = new EmptyNode131(); break;
                    case 132: func = new EmptyNode132(); break;
                    case 133: func = new EffectCreateNode133(); break;
                    case 134: func = new EffectCreateNode134(); break;
                    case 136: func = new EmptyNode136(); break;
                    case 137: func = new FXNode137(); break;
                    case 138: func = new FXNode138(); break;
                    case 139: func = new FXNode139(); break;
                    case 140: func = new FXNode140(); break;
                    default:
                        throw new NotImplementedException();
                }

                return func;
            }

            internal void Read(BinaryReaderEx br, FxrEnvironment env)
            {
                //long start = br.Position;
                ReadInner(br, env);
                //long end = br.Position;
                //DEBUG_DataSizeOnRead = end - start;
            }

            public class FXNodeRef : FXNode
            {
                public string ReferenceXID;

                public FXNodeRef(FXNode refVal)
                {
                    ReferenceXID = refVal?.XID;
                }

                public FXNodeRef()
                {

                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    throw new InvalidOperationException("Cannot actually deserialize a FXNodeRef.");
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    throw new InvalidOperationException("Cannot actually serialize a FXNodeRef.");
                }
            }

            public class EffectCallNode : FXNode
            {
                public int EffectID;
                public FXContainer Container;
                public int Unk;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Container = fxr.ReferenceFXContainer(Container);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Container = fxr.DereferenceFXContainer(Container);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(37);

                    EffectID = br.ReadFXR1Varint();
                    int astOffset = br.ReadFXR1Varint();
                    Unk = br.ReadFXR1Varint();

                    Container = env.GetFXContainer(br, astOffset);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(37);

                    bw.WriteFXR1Varint(EffectID);
                    env.RegisterPointer(Container);
                    bw.WriteFXR1Varint(Unk);
                }
            }

            public class ActionCallNode : FXNode
            {
                [XmlAttribute]
                public int ActionType;
                public FXContainer Container;
                [XmlAttribute]
                public int Unk;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Container = fxr.ReferenceFXContainer(Container);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Container = fxr.DereferenceFXContainer(Container);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(38);

                    ActionType = br.ReadFXR1Varint();
                    int astOffset = br.ReadFXR1Varint();
                    Unk = br.ReadFXR1Varint();

                    Container = env.GetFXContainer(br, astOffset);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(38);

                    bw.WriteFXR1Varint(ActionType);
                    env.RegisterPointer(Container);
                    bw.WriteFXR1Varint(Unk);
                }
            }

            public class EffectCreateNode133 : FXNode
            {
                public int EffectID;
                public int Unk;
                public FXContainer Container1;
                public FXContainer Container2;
                public List<FXState> States;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    for (int i = 0; i < States.Count; i++)
                        States[i] = fxr.ReferenceState(States[i]);
                    Container1 = fxr.ReferenceFXContainer(Container1);
                    Container2 = fxr.ReferenceFXContainer(Container2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    for (int i = 0; i < States.Count; i++)
                        States[i] = fxr.DereferenceState(States[i]);
                    Container1 = fxr.DereferenceFXContainer(Container1);
                    Container2 = fxr.DereferenceFXContainer(Container2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(133);

                    EffectID = br.ReadFXR1Varint();
                    for (int i = 0; i < 7; i++)
                        br.AssertFXR1Varint(0);
                    Unk = br.ReadFXR1Varint();
                    //throw new NotImplementedException();

                    Container1 = env.GetFXContainer(br, br.Position);
                    br.Position += FXContainer.GetSize(br.VarintLong);

                    Container2 = env.GetFXContainer(br, br.Position);
                    br.Position += FXContainer.GetSize(br.VarintLong);

                    int offsetToNodeList = br.ReadFXR1Varint();
                    int nodeCount = br.ReadFXR1Varint();
                    States = new List<FXState>();
                    br.StepIn(offsetToNodeList);
                    for (int i = 0; i < nodeCount; i++)
                    {
                        States.Add(env.GetFXState(br, br.Position));
                        br.Position += FXState.GetSize(br.VarintLong);
                    }
                    br.StepOut();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(133);

                    bw.WriteFXR1Varint(EffectID);
                    for (int i = 0; i < 7; i++)
                        bw.WriteFXR1Varint(0);
                    bw.WriteFXR1Varint(Unk);
                    Container1.Write(bw, env);
                    Container2.Write(bw, env);
                    env.RegisterPointer(States);
                    bw.WriteFXR1Varint(States.Count);
                }
            }

            public class EffectCreateNode134 : FXNode
            {
                public int EffectID;
                public int Unk;
                public List<FXNode> Nodes;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    for (int i = 0; i < Nodes.Count; i++)
                        Nodes[i] = fxr.ReferenceFXNode(Nodes[i]);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    for (int i = 0; i < Nodes.Count; i++)
                        Nodes[i] = fxr.DereferenceFXNode(Nodes[i]);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(134);

                    EffectID = br.ReadFXR1Varint();
                    Unk = br.ReadFXR1Varint();
                    int offsetToNodeOffsetList = br.ReadFXR1Varint();
                    int funcCount = br.ReadFXR1Varint();
                    Nodes = new List<FXNode>(funcCount);
                    br.StepIn(offsetToNodeOffsetList);
                    for (int i = 0; i < funcCount; i++)
                    {
                        int nextNodeOffset = br.ReadInt32();
                        var func = env.GetFXNode(br, nextNodeOffset);
                        Nodes.Add(func);
                    }
                    br.StepOut();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(134);

                    bw.WriteFXR1Varint(EffectID);
                    bw.WriteFXR1Varint(Unk);
                    env.RegisterPointer(Nodes);
                    bw.WriteFXR1Varint(Nodes.Count);
                }
            }

            public class IntNode : FXNode
            {
                [XmlAttribute]
                public int Value;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(1);

                    Value = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(1);

                    bw.WriteFXR1Varint(Value);
                }
            }

            public class IntArrayNode : FXNode
            {
                public List<int> Values;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(2);

                    int listOffset = br.ReadFXR1Varint();
                    int listCount = br.ReadFXR1Varint();

                    Values = new List<int>(listCount);

                    br.StepIn(listOffset);
                    for (int i = 0; i < listCount; i++)
                    {
                        Values.Add(br.ReadInt32());
                    }
                    br.StepOut();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(2);

                    env.RegisterPointer(Values);
                    bw.WriteFXR1Varint(Values.Count);
                }
            }

            public class IntSequenceNode3 : FXNode
            {
                public List<IntTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(3);

                    Ticks = IntTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(3);

                    IntTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class IntSequenceNode5 : FXNode
            {
                public List<IntTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(5);

                    Ticks = IntTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(5);

                    IntTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class IntSequenceNode6 : FXNode
            {
                public List<IntTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(6);

                    Ticks = IntTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(6);

                    IntTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class FloatNode : FXNode
            {
                [XmlAttribute]
                public float Value;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(7);

                    Value = br.ReadFXR1Single();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(7);

                    bw.WriteFXR1Single(Value);
                }
            }

            public class IntSequenceNode9 : FXNode
            {
                public List<IntTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(9);

                    Ticks = IntTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(9);

                    IntTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class FloatSequenceNode11 : FXNode
            {
                public List<FloatTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(11);

                    Ticks = FloatTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(11);

                    FloatTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class FloatSequenceNode12 : FXNode
            {
                public List<FloatTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(12);

                    Ticks = FloatTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(12);

                    FloatTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class Float3SequenceNode13 : FXNode
            {
                public List<Float3Tick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(13);

                    Ticks = Float3Tick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(13);

                    Float3Tick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class Float3SequenceNode14 : FXNode
            {
                public List<Float3Tick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(14);

                    Ticks = Float3Tick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(14);

                    Float3Tick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class ColorSequenceNode19 : FXNode
            {
                public List<ColorTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(19);

                    Ticks = ColorTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(19);

                    ColorTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class ColorSequenceNode20 : FXNode
            {
                public List<ColorTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(20);

                    Ticks = ColorTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(20);

                    ColorTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class Color3SequenceNode21 : FXNode
            {
                public List<Color3Tick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(21);

                    Ticks = Color3Tick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(21);

                    Color3Tick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class Color3SequenceNode22 : FXNode
            {
                public List<Color3Tick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(22);

                    Ticks = Color3Tick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(22);

                    Color3Tick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class ColorSequenceNode27 : FXNode
            {
                public List<ColorTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(27);

                    Ticks = ColorTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(27);

                    ColorTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class ColorSequenceNode28 : FXNode
            {
                public List<ColorTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(28);

                    Ticks = ColorTick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(28);

                    ColorTick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class Color3SequenceNode29 : FXNode
            {
                public List<Color3Tick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(29);

                    Ticks = Color3Tick.ReadListInFXNode(br);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(29);

                    Color3Tick.WriteListInFXNode(bw, env, Ticks);
                }
            }

            public class IntNode41 : FXNode
            {
                [XmlAttribute]
                public int Value;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(41);

                    Value = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(41);

                    bw.WriteFXR1Varint(Value);
                }
            }

            public class IndexedIntNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(44);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(44);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IndexedIntArrayNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(45);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(45);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IndexedIntSequenceNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(46);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(46);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IndexedFloatNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(47);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(47);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IndexedEffectNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(59);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(59);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IndexedActionNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(60);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(60);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }


            public class IndexedSoundIDNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(66);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(66);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class SoundIDNode68 : FXNode
            {
                [XmlAttribute]
                public int SoundID;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(68);

                    SoundID = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(68);

                    bw.WriteFXR1Varint(SoundID);
                }
            }

            public class SoundIDNode69 : FXNode
            {
                [XmlAttribute]
                public int SoundID;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(69);

                    SoundID = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(69);

                    bw.WriteFXR1Varint(SoundID);
                }
            }

            public class TickNode : FXNode
            {
                [XmlAttribute]
                public float Tick;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(70);

                    Tick = br.ReadFXR1Single();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(70);

                    bw.WriteFXR1Single(Tick);
                }
            }


            public class IndexedTickNode : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(71);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(71);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class Int2Node : FXNode
            {
                [XmlAttribute]
                public int X;
                [XmlAttribute]
                public int Y;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(79);

                    X = br.ReadInt32();
                    Y = br.ReadInt32();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(79);

                    bw.WriteInt32(X);
                    bw.WriteInt32(Y);
                }
            }

            public class Float2Node : FXNode
            {
                [XmlAttribute]
                public float X;
                [XmlAttribute]
                public float Y;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(81);

                    X = br.ReadSingle();
                    Y = br.ReadSingle();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(81);

                    bw.WriteSingle(X);
                    bw.WriteSingle(Y);
                }
            }

            public class Tick2Node : FXNode
            {
                [XmlAttribute]
                public float Tick1;
                [XmlAttribute]
                public float Tick2;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(85);

                    Tick1 = br.ReadSingle();
                    Tick2 = br.ReadSingle();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(85);

                    bw.WriteSingle(Tick1);
                    bw.WriteSingle(Tick2);
                }
            }

            public class IndexedTick2Node : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(87);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(87);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class IntSequenceNode89 : FXNode
            {
                public List<IntTick> Ticks;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(89);

                    Ticks = IntTick.ReadListInFXNode(br);
                    br.AssertFXR1Varint(1);
                    br.AssertFXR1Varint(0);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(89);

                    IntTick.WriteListInFXNode(bw, env, Ticks);
                    bw.WriteFXR1Varint(1);
                    bw.WriteFXR1Varint(0);
                }
            }

            public class FloatSequenceNodeNode91 : FXNode
            {
                public List<FloatTick> Ticks;
                public FXNode Node;
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(91);

                    Ticks = FloatTick.ReadListInFXNode(br);
                    br.AssertFXR1Varint(1);
                    long paramOffset = br.ReadFXR1Varint();
                    Node = env.GetFXNode(br, paramOffset);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(91);

                    FloatTick.WriteListInFXNode(bw, env, Ticks);
                    bw.WriteFXR1Varint(1);
                    env.RegisterPointer(Node);
                }
            }

            public class FloatSequenceNodeNode95 : FXNode
            {
                public List<FloatTick> Ticks;
                public FXNode Node;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node = fxr.ReferenceFXNode(Node);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node = fxr.DereferenceFXNode(Node);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(95);

                    Ticks = FloatTick.ReadListInFXNode(br);
                    br.AssertFXR1Varint(1);
                    long paramOffset = br.ReadFXR1Varint();
                    Node = env.GetFXNode(br, paramOffset);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(95);

                    FloatTick.WriteListInFXNode(bw, env, Ticks);
                    bw.WriteFXR1Varint(1);
                    env.RegisterPointer(Node);
                }
            }

            public class IntNode111 : FXNode
            {
                [XmlAttribute]
                public int Value;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(111);

                    Value = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(111);

                    bw.WriteFXR1Varint(Value);
                }
            }

            public class EmptyNode112 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(112);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(112);
                }
            }

            public class EmptyNode113 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(113);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(113);
                }
            }

            public class UnkIndexedValueNode114 : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(114);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(114);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class UnkIndexedValueNode115 : FXNode
            {
                [XmlAttribute]
                public short Type;
                [XmlAttribute]
                public short Index;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(115);

                    Type = br.ReadInt16();
                    Index = br.ReadInt16();

                    br.AssertFXR1Garbage(); //????
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(115);

                    bw.WriteInt16(Type);
                    bw.WriteInt16(Index);

                    bw.WriteFXR1Garbage(); //????
                }
            }

            public class Node2Node120 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(120);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(120);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class Node2Node121 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(121);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(121);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class Node2Node122 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(122);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(122);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class Node2Node123 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(123);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(123);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class Node2Node124 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(124);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(124);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }
            public class Node2Node126 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(126);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(126);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class Node2Node127 : FXNode
            {
                public FXNode Node1;
                public FXNode Node2;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node1 = fxr.ReferenceFXNode(Node1);
                    Node2 = fxr.ReferenceFXNode(Node2);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node1 = fxr.DereferenceFXNode(Node1);
                    Node2 = fxr.DereferenceFXNode(Node2);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(127);

                    int funcOffset1 = br.ReadFXR1Varint();
                    int funcOffset2 = br.ReadFXR1Varint();

                    Node1 = env.GetFXNode(br, funcOffset1);
                    Node2 = env.GetFXNode(br, funcOffset2);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(127);

                    env.RegisterPointer(Node1);
                    env.RegisterPointer(Node2);
                }
            }

            public class NodeNode128 : FXNode
            {
                public FXNode Node;

                internal override void InnerToXIDs(FXR1 fxr)
                {
                    Node = fxr.ReferenceFXNode(Node);
                }

                internal override void InnerFromXIDs(FXR1 fxr)
                {
                    Node = fxr.DereferenceFXNode(Node);
                }

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(128);

                    int funcOffset = br.ReadFXR1Varint();

                    Node = env.GetFXNode(br, funcOffset);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(128);

                    env.RegisterPointer(Node);
                }
            }

            public class EmptyNode129 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(129);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(129);
                }
            }

            public class EmptyNode130 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(130);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(130);
                }
            }

            public class EmptyNode131 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(131);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(131);
                }
            }

            public class EmptyNode132 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(132);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(132);
                }
            }

            public class EmptyNode136 : FXNode
            {
                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(136);
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(136);
                }
            }

            public class FXNode137 : FXNode
            {
                [XmlAttribute]
                public int Unk1;
                [XmlAttribute]
                public int Unk2;
                [XmlAttribute]
                public int Unk3;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(137);

                    Unk1 = br.ReadFXR1Varint();
                    Unk2 = br.ReadFXR1Varint();
                    Unk3 = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(137);

                    bw.WriteFXR1Varint(Unk1);
                    bw.WriteFXR1Varint(Unk2);
                    bw.WriteFXR1Varint(Unk3);
                }
            }

            public class FXNode138 : FXNode
            {
                [XmlAttribute]
                public int Unk1;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(138);

                    Unk1 = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(138);

                    bw.WriteFXR1Varint(Unk1);
                }
            }

            public class FXNode139 : FXNode
            {
                [XmlAttribute]
                public int Unk1;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(139);

                    Unk1 = br.ReadFXR1Varint();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(139);

                    bw.WriteFXR1Varint(Unk1);
                }
            }

            public class FXNode140 : FXNode
            {
                [XmlAttribute]
                public int Unk1;
                [XmlAttribute]
                public int Unk2;

                internal override void ReadInner(BinaryReaderEx br, FxrEnvironment env)
                {
                    br.AssertFXR1Varint(140);

                    Unk1 = br.ReadInt32();
                    Unk2 = br.ReadInt32();
                }

                internal override void WriteInner(BinaryWriterEx bw, FxrEnvironment env)
                {
                    bw.WriteFXR1Varint(140);

                    bw.WriteInt32(Unk1);
                    bw.WriteInt32(Unk2);
                }
            }


        }
    }
}
