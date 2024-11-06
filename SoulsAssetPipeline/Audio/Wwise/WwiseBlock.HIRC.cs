using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public abstract partial class WwiseBlock
    {
        public class HIRC : WwiseBlock
        {
            public HIRC()
                : base("HIRC")
            {

            }

            public class WwiseObjectInfo
            {
                public enum ObjTypes : byte
                {
                    CAkState = 1,
                    CAkSound = 2,
                    CAkAction = 3,
                    CAkEvent = 4,
                    CAkRanSeqCntr = 5,
                    CAkSwitchCntr = 6,
                    CAkActorMixer = 7,
                    CAkBus = 8,
                    CAkLayerCntr = 9,
                    Unk10 = 10,
                    Unk11 = 11,
                    Unk12 = 12,
                    Unk13 = 13,
                    CAkAttenuation = 14,
                    CAkDialogueEvent = 15,
                    CAkFxCustom = 17,
                    CAkAuxBus = 18,
                }
                public ObjTypes ObjectType;
                public int HircOffset;
                public int ObjectSize;
            }

            internal BinaryReaderEx objFetchBinaryReader;
            private Dictionary<uint, WwiseObjectInfo> wwObjectInfos = new Dictionary<uint, WwiseObjectInfo>();
            public IReadOnlyDictionary<uint, WwiseObjectInfo> ObjectInfos => wwObjectInfos;

            private Dictionary<uint, IWwiseObject> loadedObjects = new Dictionary<uint, IWwiseObject>();
            public IReadOnlyDictionary<uint, IWwiseObject> LoadedObjects => loadedObjects;

            public IWwiseObject LoadObjectDynamic(uint id)
            {
                if (loadedObjects.ContainsKey(id))
                {
                    return loadedObjects[id];
                }
                else
                {
                    if (!wwObjectInfos.ContainsKey(id))
                        return null;
                    var info = wwObjectInfos[id];

                    IWwiseObject t = null;

                    switch (info.ObjectType)
                    {
                        case WwiseObjectInfo.ObjTypes.CAkAction:
                            t = new WwiseObject.CAkAction();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkEvent:
                            t = new WwiseObject.CAkEvent();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkRanSeqCntr:
                            t = new WwiseObject.CAkRanSeqCntr();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkSound:
                            t = new WwiseObject.CAkSound();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkSwitchCntr:
                            t = new WwiseObject.CAkSwitchCntr();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkLayerCntr:
                            t = new WwiseObject.CAkLayerCntr();
                            break;
                        case WwiseObjectInfo.ObjTypes.CAkDialogueEvent:
                            t = new WwiseObject.CAkDialogueEvent();
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    objFetchBinaryReader.StepIn(info.HircOffset);
                    objFetchBinaryReader.AssertUInt32(id);
                    t.Read(objFetchBinaryReader, null);
                    loadedObjects.Add(id, t);
                    return t;
                }
            }

            public T LoadObject<T>(uint id)
                where T : class, IWwiseObject, new()
            {
                if (loadedObjects.ContainsKey(id) && loadedObjects[id] is T asT)
                {
                    return asT;
                }
                else
                {
                    if (!wwObjectInfos.ContainsKey(id))
                        return null;
                    var info = wwObjectInfos[id];
                    var t = new T();
                    objFetchBinaryReader.StepIn(info.HircOffset);
                    objFetchBinaryReader.AssertUInt32(id);
                    t.Read(objFetchBinaryReader, null);
                    loadedObjects.Add(id, t);
                    return t;
                }

            }

            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                var startPos = br.Position;
                objFetchBinaryReader = new BinaryReaderEx(false, br.GetBytes(br.Position, sectionLength));

                int numObjects = br.ReadInt32();
                wwObjectInfos.Clear();
                for (int i = 0; i < numObjects; i++)
                {
                    var objectType = (WwiseObjectInfo.ObjTypes)br.ReadByte();
                    int objectSize = br.ReadInt32();
                    var objectStart = br.Position;
                    int offset = (int)(objectStart - startPos);
                    uint id = br.ReadUInt32();
                    wwObjectInfos.Add(id, new WwiseObjectInfo()
                    {
                        ObjectType = objectType,
                        HircOffset = offset,
                        ObjectSize = objectSize
                    });
                    br.Position = (objectStart + objectSize);
                }
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
