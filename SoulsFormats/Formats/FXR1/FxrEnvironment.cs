using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulsFormats
{
    public partial class FXR1
    {
        internal class FxrEnvironment
        {
            public List<long> DEBUG_PointerTable = new List<long>();
            public List<long> DEBUG_PointerTable_Unused = new List<long>();

            public void DEBUG_PointerTableCheck(BinaryReaderEx br)
            {
                //if (DEBUG_PointerTable.Contains(br.Position))
                //{
                //    if (DEBUG_PointerTable_Unused.Contains(br.Position))
                //        DEBUG_PointerTable_Unused.Remove(br.Position);
                //}
                //else
                //{
                //    Console.WriteLine($"WARNING: OFFSET 0x{br.Position:X} NOT IN POINTER TABLE.");
                //}
            }

            public BinaryWriterEx bw;

            public FXR1 fxr;

            public List<FXNode> XmlFXContainer = new List<FXNode>();

            private List<long> PointerOffsets = new List<long>();
            private List<long> FXNodeOffets = new List<long>();

            public Dictionary<long, object> ObjectsByOffset = new Dictionary<long, object>();
            public Dictionary<object, long> OffsetsByObject = new Dictionary<object, long>();

            private List<object> ThingsToWrite = new List<object>();

            public void RegisterPointerOffset(long offset)
            {
                if (!PointerOffsets.Contains(offset))
                    PointerOffsets.Add(offset);
            }

            public void AddThingToWrite(object thingToWrite)
            {
                if (!ThingsToWrite.Contains(thingToWrite))
                    ThingsToWrite.Add(thingToWrite);
            }

            public void RegisterOffset(long offset, object thingThere)
            {
                if (!ObjectsByOffset.ContainsKey(offset))
                    ObjectsByOffset.Add(offset, thingThere);

                if (!OffsetsByObject.ContainsKey(thingThere))
                    OffsetsByObject.Add(thingThere, offset);
            }

            public FXNodePointer GetFXNodePointer(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXNodePointer v)
                        return v;
                    else 
                        throw new InvalidOperationException();
                }
                else
                {
                    var newVal = new FXNodePointer();
                    RegisterOffset(offset, newVal);
                    br.StepIn(offset);
                    newVal.Read(br, this);
                    br.StepOut();
                    
                    return newVal;
                }
            }

            public FXActionData GetFXActionData(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXActionData v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    br.StepIn(offset);
                    var newVal = FXActionData.Read(br, this);
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    br.StepOut();

                    return newVal;
                }
            }

            public FXModifier GetFXModifier(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXModifier v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    br.StepIn(offset);
                    var newVal = FXModifier.GetProperType(br, this);
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    newVal.Read(br, this);
                    br.StepOut();
                    
                    return newVal;
                }
            }

            public FXNode GetFXNode(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXNode v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    br.StepIn(offset);
                    var newVal = FXNode.GetProperFXNodeType(br, this);
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    XmlFXContainer.Add(newVal);
                    newVal.Read(br, this);
                    br.StepOut();
                    
                    return newVal;
                }
            }

            public FXContainer GetFXContainer(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXContainer v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    var newVal = new FXContainer();
                    RegisterOffset(offset, newVal);
                    br.StepIn(offset);
                    newVal.Read(br, this);
                    br.StepOut();
                    
                    return newVal;
                }
            }

            public FXState GetFXState(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXState v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    var newVal = new FXState();
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    br.StepIn(offset);
                    newVal.Read(br, this);
                    br.StepOut();
                    
                    return newVal;
                }
            }

            public FXTransition GetFXTransition(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXTransition v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    var newVal = new FXTransition();
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    br.StepIn(offset);
                    newVal.Read(br, this);
                    br.StepOut();

                    return newVal;
                }
            }

            public FXAction GetFXAction(BinaryReaderEx br, long offset)
            {
                if (offset == 0)
                    return null;

                if (offset != br.Position)
                    DEBUG_PointerTableCheck(br);

                if (ObjectsByOffset.ContainsKey(offset))
                {
                    if (ObjectsByOffset[offset] is FXAction v)
                        return v;
                    else
                        throw new InvalidOperationException();
                }
                else
                {   
                    var newVal = new FXAction();
                    RegisterOffset(offset, newVal);
                    newVal.XID = $"0x{offset:X}";
                    br.StepIn(offset);
                    newVal.Read(br, this);
                    br.StepOut();
                    return newVal;
                }
            }

            private Dictionary<object, List<long>> PointerWriteLocations = new Dictionary<object, List<long>>();

            //public void RegisterPointer32(object pointToObject)
            //{
            //    if (!PointerWriteLocations.ContainsKey(pointToObject))
            //        PointerWriteLocations.Add(pointToObject, new List<long>());

            //    if (!PointerWriteLocations[pointToObject].Contains(bw.Position))
            //        PointerWriteLocations[pointToObject].Add(bw.Position);

            //    bw.WriteUInt32(0xEFBEADDE); //DEADBEEF
            //}

            private int GetListObjectCount(object obj)
            {
                switch (obj)
                {
                    case List<int> intList: return intList.Count;
                    case List<float> floatList: return floatList.Count;
                    case List<FXState> fxStateList: return fxStateList.Count;
                    case List<FXTransition> fxTransitionList: return fxTransitionList.Count;
                    case List<FXAction> fxActionList: return fxActionList.Count;
                    case List<FXNode> nodeList: return nodeList.Count;
                    default: return -1;
                }
            }

            public void RegisterPointer(object pointToObject, 
                bool useExistingPointerOnly = false, 
                bool assertNotNull = false)
            {
                //idk wtf im doing
                //bw.Pad(8);

                if (pointToObject == null || GetListObjectCount(pointToObject) == 0)
                {
                    if (assertNotNull)
                    {
                        throw new InvalidOperationException("Assertion that pointer is not null failed.");
                    }
                    else
                    {
                        WriteFXR1Varint(bw, 0);
                        return;
                    }
                    
                }

                if (!PointerOffsets.Contains(bw.Position))
                    PointerOffsets.Add(bw.Position);

                if (OffsetsByObject.ContainsKey(pointToObject))
                {
                    WriteFXR1Varint(bw, (int)OffsetsByObject[pointToObject]);
                }
                else
                {
                    if (useExistingPointerOnly)
                        throw new InvalidOperationException("Assertion that pointer already existed failed.");

                    if (!PointerWriteLocations.ContainsKey(pointToObject))
                        PointerWriteLocations.Add(pointToObject, new List<long>());

                    if (!PointerWriteLocations[pointToObject].Contains(bw.Position))
                        PointerWriteLocations[pointToObject].Add(bw.Position);

                    bw.WriteUInt32(0xEEEEEEEE);
                    WriteFXR1Garbage(bw);
                }
            }

            private List<object> DEBUG_ObjectWriteOrder = new List<object>();


            public void FinishRecursiveWrite()
            {
                // Copy the current write shit to a new collection so we can start queueing the
                // shit for the next recursive pass :demonicfatcat:
                var currentWriteFeed = new Dictionary<object, List<long>>();
                foreach (var kvp in PointerWriteLocations)
                {
                    currentWriteFeed.Add(kvp.Key, kvp.Value);
                }
                PointerWriteLocations.Clear();

                foreach (var kvp in currentWriteFeed)
                {
                    bw.Pad(16);

                    // Register this as an offset for recursive write and pointer shit
                    RegisterOffset(bw.Position, kvp.Key);

                    DEBUG_ObjectWriteOrder.Add(kvp.Key);

                    // Write the actual data
                    void DoDataWrite(object data)
                    {
                        switch (data)
                        {
                            case FXNodePointer asEffectFXNode: asEffectFXNode.Write(bw, this); break;
                            case FXActionData asActionData: asActionData.Write(bw, this); break;
                            case FXModifier asModifier: asModifier.Write(bw, this); break;
                            case FXContainer asEffect: asEffect.Write(bw, this); break;
                            case FXAction asFXAction: asFXAction.Write(bw, this); break;
                            case FXTransition asTransition: asTransition.Write(bw, this); break;
                            case FXState asState: asState.Write(bw, this); break;
                            case FXNode asFXNode: asFXNode.Write(bw, this); break;
                            case List<FXNode> asFXContainer:
                                foreach (var v in asFXContainer)
                                    RegisterPointer(v);
                                break;
                            case List<FXTransition> asTransitionList:
                                //if (!PointerOffsets.Contains(bw.Position))
                                //    PointerOffsets.Add(bw.Position);

                                foreach (var v in asTransitionList)
                                {
                                    RegisterOffset(bw.Position, v);
                                    v.Write(bw, this);
                                }
                                break;
                            case List<FXAction> asFXActionList:
                                foreach (var v in asFXActionList)
                                {
                                    RegisterOffset(bw.Position, v);
                                    v.Write(bw, this);
                                }
                                break;
                            case List<FXState> asStateList:
                                foreach (var v in asStateList)
                                {
                                    RegisterOffset(bw.Position, v);
                                    v.Write(bw, this);
                                }
                                break;
                            case List<FXNodePointer> asEffectFXContainer:
                                foreach (var v in asEffectFXContainer)
                                {
                                    RegisterOffset(bw.Position, v);
                                    RegisterPointerOffset(bw.Position);
                                    v.Write(bw, this);
                                }
                                break;
                            case List<float> asFloatList:
                                foreach (var v in asFloatList)
                                {
                                    bw.WriteSingle(v);
                                }
                                break;
                            case List<int> asIntList:
                                foreach (var v in asIntList)
                                {
                                    bw.WriteInt32(v);
                                }
                                break;
                            default: throw new NotImplementedException($"FxrEnvironment recursive write not implemented for '{data.GetType().ToString()}'");
                        }
                    }
                    DoDataWrite(kvp.Key);

                    // Fill in any current references to this shit
                    var locationItPointsTo = OffsetsByObject[kvp.Key];
                    foreach (var location in kvp.Value)
                    {
                        bw.StepIn(location);
                        RegisterPointerOffset(bw.Position);
                        bw.WriteInt32((int)locationItPointsTo);
                        bw.StepOut();
                    }
                }

                if (PointerWriteLocations.Count > 0)
                {
                    FinishRecursiveWrite();
                }
            }

            public void RegisterFXNodeOffsetHere()
            {
                if (!FXNodeOffets.Contains(bw.Position))
                    FXNodeOffets.Add(bw.Position);
            }

            //internal void WriteAllFXNodes()
            //{
            //    foreach (var func in ThingsToWrite.OfType<FXNode>())
            //    {
            //        RegisterFXNodeOffsetHere();
            //        func.WriteInner(bw, this);
            //    }
            //}

            internal void WriteFXNodeTable(string tableCountFillLabel)
            {
                foreach (var location in FXNodeOffets.OrderBy(x => x))
                {
                    WriteFXR1Varint(bw, (int)location);
                }
                bw.FillInt32(tableCountFillLabel, FXNodeOffets.Count);
            }

            internal void WritePointerTable(string tableCountFillLabel)
            {
                foreach (var offset in PointerOffsets.OrderBy(x => x))
                {
                    WriteFXR1Varint(bw, (int)offset);
                }
                bw.FillInt32(tableCountFillLabel, PointerOffsets.Count);
            }

            internal void ReadPointerTable(BinaryReaderEx br, int count)
            {
                DEBUG_PointerTable = new List<long>(count);
                DEBUG_PointerTable_Unused = new List<long>(count);
                for (int i = 0; i < count; i++)
                {
                    int next = ReadFXR1Varint(br);
                    DEBUG_PointerTable.Add(next);
                    DEBUG_PointerTable_Unused.Add(next);
                }
            }
        }
    }
}
