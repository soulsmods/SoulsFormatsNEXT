using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SoulsFormats
{
    public class ESD_DLSE : SoulsFile<ESD_DLSE>
    {
        public DLSEHeader Header;
        public EzStateProject Project;
        public string[] ClassNames;
        
        public object[] Pointers = new object[1024];
        private int pointerCount = 0;
        
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = true;
            
            // Header
            Header = new DLSEHeader();
            br.AssertASCII("DLSE");
            Header.Unk04 = br.AssertInt16(2);
            Header.Version = br.AssertInt16(0, 1, 3);
            if (Header.Version == 3)
            {
                Header.Unk08 = br.ReadInt32();
                Header.Unk0C = br.ReadInt32();
                Header.Unk10 = br.ReadInt32();
            }
            Header.ClassCount = br.ReadInt16();
            
            ClassNames = new string[Header.ClassCount];
            br.ReadInt32();
            ClassNames[0] = br.AssertASCII("EzStateProject");
            br.ReadInt32();
            ClassNames[1] = br.AssertASCII("DLVector");
            br.ReadInt32();
            ClassNames[2] = br.AssertASCII("EzStateMap");
            br.ReadInt32();
            ClassNames[3] = br.AssertASCII("EzStateMapState");
            br.ReadInt32();
            ClassNames[4] = br.AssertASCII("EzStateTransition");
            br.ReadInt32();
            ClassNames[5] = br.AssertASCII("buffer");
            br.ReadInt32();
            ClassNames[6] = br.AssertASCII(Header.Version == 3 ? "EzStateExternalEventT<ES_EVENT_PARAM_NUM_6>" : "EzStateExternalEvent");

            Project = EzStateProject.Read(this, br);
        }

        public class PrefixString()
        {
            public int length;
            public char[] str;
        }

        public class DLSEHeader()
        {
            public short Unk04;
            public short Version;
            public int Unk08;
            public int Unk0C;
            public int Unk10;
            public short ClassCount;
        }

        public class EzStateProject
        {
            public Block block;
            public DLVector stateMaps;

            public static EzStateProject Read(ESD_DLSE esd, BinaryReaderEx br)
            {
                EzStateProject project = new EzStateProject();
                project.block = Block.Read(br);
                project.stateMaps = DLVector.Read(esd, br, 3);
                return project;
            }
        }

        public class DLVector
        {
            public short Type;
            public int Count;
            public object[] Items;

            public static DLVector Read(ESD_DLSE esd, BinaryReaderEx br, short assertType)
            {
                DLVector vector = new DLVector();
                if (br.Position == 0x5FD)
                {
                    Console.WriteLine();
                }

                if (br.GetInt16(br.Position) > 7 || br.GetInt16(br.Position) < 1 ) return vector;
                vector.Type = br.GetInt16(br.Position);
                if (vector.Type != 2 || (br.GetInt16(br.Position + 6) != assertType && br.GetInt16(br.Position + 6) != 2))
                {
                    return vector;
                }

                br.ReadInt16();
                vector.Count = br.ReadInt32();
                vector.Items = new object[vector.Count];
                for (int i = 0; i < vector.Count; i++)
                {
                    if (assertType == 5 && vector.Count == 3)
                    {
                        Console.WriteLine();
                    }
                    
                    int nextType = br.GetInt16(br.Position);
                    int nextValue = br.GetInt32(br.Position + 2);
                    if (assertType == 8 && nextType == 5)
                    {
                        vector.Items[i] = EzStateCondition.Read(esd, br);
                        continue;
                    }
                    if (assertType == 8)
                    {
                        continue;
                    }
                    if (nextType != 2 && nextType == assertType && nextType > 0 && nextType < 8)
                    {
                        vector.Items[i] = Pointer.Read(esd, br, nextType, vector);
                        continue;
                    }
                    else if (nextType == 2)
                    {
                        vector.Items[i] = DLVector.Read(esd, br, assertType);
                        continue;
                    }
                    /*int type = br.ReadInt16();
                    if (type == 2)
                    {
                        vector.Items[i] = DLVector.Read(esd, br);
                    }
                    else if (type == 5)
                    {
                        vector.Items[i] = EzStateTransition.Read(esd, br);
                    }*/
                }
                
                return vector;
            }
        }

        public class EzStateMap
        {
            public Block Block;
            public int Unk00;
            public Pointer Root;

            public static EzStateMap Read(ESD_DLSE esd, BinaryReaderEx br)
            {
                EzStateMap map = new EzStateMap();
                map.Block = Block.Read(br);
                map.Unk00 = br.ReadInt32();
                map.Root = Pointer.Read(esd, br, 4, map);
                return map;
            }
        }

        public class EzStateMapState
        {
            public Block Block;
            public int Unk00;
            public DLVector Transitions;
            public DLVector ExternalEvents;
            public List<Pointer> Unknown;

            public static EzStateMapState Read(ESD_DLSE esd, BinaryReaderEx br, object caller)
            {
                EzStateMapState state = new EzStateMapState();
                state.Block = Block.Read(br);
                state.Unk00 = br.ReadInt32();
                state.Transitions = DLVector.Read(esd, br, 5);
                state.ExternalEvents = DLVector.Read(esd, br, 7);
                
                int nextType = br.GetInt16(br.Position);
                while (nextType == 2)
                {
                    br.ReadInt16();
                    br.ReadInt32();
                    nextType = br.GetInt16(br.Position);
                }

                if (caller is EzStateMapState)
                {
                    return state;
                }

                if (state.Unk00 == 262144)
                {
                    Console.WriteLine();
                }
                
                state.Unknown = new List<Pointer>();
                int i = 0;
                foreach (object item in state.Transitions.Items)
                {
                    if (item is Pointer pointer)
                    {
                        if (i == 0)
                        {
                            i++;
                            continue;
                        }
                        if (esd.Pointers[pointer.Id] is EzStateTransition transition)
                        {
                            transition.State = Pointer.Read(esd, br, 4, state);
                        }
                    }
                    else if (item is DLVector { Items: not null } vector)
                    {
                        int k = 0;
                        foreach (object vectorItem in vector.Items)
                        {
                            if (k > 0)
                            {
                                if (vectorItem is Pointer vectorPointer)
                                {
                                    if (esd.Pointers[vectorPointer.Id] is EzStateTransition vectorTransition)
                                    {
                                        if (k == 6)
                                        {
                                            Console.WriteLine();
                                        }

                                        if (br.GetInt16(br.Position) == 4)
                                        {
                                            vectorTransition.State = Pointer.Read(esd, br, 4, state);
                                        }
                                    }
                                }
                            }

                            k++;
                        }
                    }
                }

                /*int i = 0;
                while (br.GetInt16(br.Position) == 4 && i < limit - 1)
                {
                    state.Unknown.Add(Pointer.Read(esd, br, 4, state));
                    i++;
                }*/
                return state;
            }
        }

        public class Block
        {
            public short Type;
            public int Version;

            public static Block Read(BinaryReaderEx br)
            {
                Block block = new Block();
                block.Type = br.ReadInt16();
                block.Version = br.ReadInt32();
                return block;
            }
        }

        public class Pointer(short type)
        {
            public short Type = type;
            public int Id;
            public object? Value;
            
            public static Pointer? Read(ESD_DLSE esd, BinaryReaderEx br, int assertType, object caller)
            {
                short type = br.GetInt16(br.Position);
                Pointer pointer = new Pointer(type);
                /*if (assertType != type)
                {
                    return null;
                }*/

                br.ReadInt16();
                pointer.Id = br.ReadInt32();
                int actualType = br.GetInt16(br.Position);
                if (actualType != type && type != 6) return null;
                if (assertType == 8 && br.GetInt16(br.Position + 6) != 6)
                {
                    return null;
                }
                
                if (assertType == 2)
                {
                    DLVector vector = DLVector.Read(esd, br, 2);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = vector;
                }
                else if (assertType == 3)
                {
                    EzStateMap stateMap = EzStateMap.Read(esd,br);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = stateMap;
                }
                else if (assertType == 4)
                {
                    EzStateMapState state = EzStateMapState.Read(esd, br, caller);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = state;
                }
                else if (assertType == 5)
                {
                    EzStateTransition transition = EzStateTransition.Read(esd, br, caller);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = transition;
                }
                else if (assertType == 6)
                {
                    Buffer newBuffer;
                    if (br.GetInt16(br.Position) != 6)
                    {
                        Buffer buffer = new Buffer()
                        {
                            Type = pointer.Type,
                            Length = pointer.Id,
                            Data = new byte[pointer.Id]
                        };
                        Buffer.FillBufferData(buffer.Data, br);
                        newBuffer = buffer;
                    }
                    else
                    {
                        newBuffer = Buffer.Read(br);
                    }
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = newBuffer;
                }
                else if (assertType == 7)
                {
                    EzStateExternalEvent externalEvent = EzStateExternalEvent.Read(br);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = externalEvent;
                }
                else if (assertType == 8)
                {
                    EzStateCondition condition = EzStateCondition.Read(esd, br);
                    pointer.ShiftPointers(esd);
                    esd.Pointers[pointer.Id] = condition;
                }

                return pointer;
            }

            private void ShiftPointers(ESD_DLSE esd)
            {
                if (esd.Pointers[Id] != null)
                {
                    int i = Id;
                    i++;
                    while (i < esd.Pointers.Length)
                    {
                        if (esd.Pointers[i] == null)
                        {
                            Id = i;
                            break;
                        }
                        i++;
                    }
                }
            }
        }

        public class EzStateTransition
        {
            public Block Block;
            public Buffer Condition;
            public Pointer? State;
            public DLVector ExternalEvents;
            public List<Pointer> SubConditions;
            public long InitialPosition;

            public static EzStateTransition Read(ESD_DLSE esd, BinaryReaderEx br, object caller)
            {
                long initialPosition = br.Position;
                if (br.Position == 3306)
                {
                    Console.WriteLine();
                }
                EzStateTransition transition = new EzStateTransition();
                transition.InitialPosition = initialPosition;
                
                
                transition.Block = Block.Read(br);
                if (br.GetInt16(br.Position) == 6)
                {
                    transition.Condition = Buffer.Read(br);
                }
                
                int nextType = br.GetInt16(br.Position);
                while (nextType == 2)
                {
                    if (br.GetInt16(br.Position) == 2 && br.GetInt32(br.Position + 2) > 0
                        && br.GetInt16(br.Position + 6) == 7)
                    {
                        transition.ExternalEvents = DLVector.Read(esd, br, 7);
                        nextType = br.GetInt16(br.Position);
                        continue;
                    }
                    br.ReadInt16();
                    br.ReadInt32();
                    nextType = br.GetInt16(br.Position);
                }
                
                /*if (br.GetInt16(br.Position) == 5)
                {
                    transition.SubTransition = Pointer.Read(esd, br, 5, transition);
                }*/

                if (caller is EzStateTransition)
                {
                    return transition;
                }
                
                //DLVector subConditions = DLVector.Read(esd, br, 8);
                transition.SubConditions = new List<Pointer>();
                int i = 1;
                while (br.GetInt16(br.Position) == 5)
                {
                    if (caller is DLVector vector)
                    {
                        if (i >= vector.Items.Length) break;
                        vector.Items[i] = Pointer.Read(esd, br, 5, transition);
                        i++;
                    }
                    //transition.SubConditions.Add(Pointer.Read(esd, br, 5, transition));
                }
                
                if (br.GetInt16(br.Position) == 4)
                {
                    transition.State = Pointer.Read(esd, br, 4, transition);
                }
                
                return transition;
            }
        }

        public class EzStateCondition
        {
            public Block Block;
            public Buffer Evaluator;
            public DLVector ExternalEvents;
            
            public static EzStateCondition Read(ESD_DLSE esd, BinaryReaderEx br)
            {
                EzStateCondition condition = new EzStateCondition();
                condition.Block = Block.Read(br);
                condition.Evaluator = Buffer.Read(br);
                
                int nextType = br.GetInt16(br.Position);
                while (nextType == 2)
                {
                    if (br.GetInt16(br.Position) == 2 && br.GetInt32(br.Position + 2) > 0
                                                      && br.GetInt16(br.Position + 6) == 7)
                    {
                        condition.ExternalEvents = DLVector.Read(esd, br, 7);
                        nextType = br.GetInt16(br.Position);
                        continue;
                    }
                    br.ReadInt16();
                    br.ReadInt32();
                    nextType = br.GetInt16(br.Position);
                }
                
                return condition;
            }
        }
        
        public class EzStateEvaluator
        {
            public Block Block;
            public Buffer Buffer;

            public static EzStateEvaluator Read(BinaryReaderEx br)
            {
                EzStateEvaluator evaluator = new EzStateEvaluator();
                evaluator.Block = Block.Read(br);
                evaluator.Buffer = Buffer.Read(br);
                return evaluator;
            }
        }

        public class EzStateExternalEvent
        {
            public Block Block;
            public int ID;
            public int ArgCount;
            public Buffer[] Evaluators;

            public static EzStateExternalEvent Read(BinaryReaderEx br)
            {
                EzStateExternalEvent exEvent  = new EzStateExternalEvent();
                exEvent.Block = Block.Read(br);
                exEvent.ID = br.ReadInt32();
                exEvent.ArgCount = br.ReadInt32();
                exEvent.Evaluators = new Buffer[exEvent.ArgCount - 1];
                for (int i = 0; i < exEvent.ArgCount - 1; i++)
                {
                    exEvent.Evaluators[i] = Buffer.Read(br);
                }
                return exEvent;
            }
        }

        public class Buffer
        {
            public short Type;
            public int Length;
            public byte[] Data;

            public static Buffer Read(BinaryReaderEx br)
            {
                Buffer buffer = new Buffer();
                buffer.Type = br.AssertInt16(6);
                buffer.Length = br.ReadInt32();
                buffer.Data = new byte[buffer.Length];
                //FillBufferData(buffer.Data, br);
                int i = 0;
                byte b = br.ReadByte();
                if (br.Position == 681)
                {
                    Console.WriteLine();
                }
                buffer.Data[i] = b;
                while (b != 0xA1 || (b == 0xA1 && br.GetInt16(br.Position) is not 1 and not 2 and not 3 and not 4 and not 5 and not 6 and not 7))
                {
                    i++;
                    b = br.ReadByte();
                    buffer.Data[i] = b;
                }

                if (i < buffer.Data.Length - 1)
                {
                    buffer.Length = i + 1;
                    Array.Resize(ref buffer.Data, buffer.Length);
                }
                
                return buffer;
            }

            public static void FillBufferData(byte[] data, BinaryReaderEx br)
            {
                int i = 0;
                byte b = br.ReadByte();
                data[i] = b;
                while (b != 0xA1 || (b == 0xA1 && br.GetInt16(br.Position) is not 1 and not 2 and not 3 and not 4 and not 5 and not 6 and not 7))
                {
                    i++;
                    b = br.ReadByte();
                    data[i] = b;
                }

                if (i < data.Length - 1)
                {
                    int length = i + 1;
                    Array.Resize(ref data, length);
                }
            }
        }
    }
}