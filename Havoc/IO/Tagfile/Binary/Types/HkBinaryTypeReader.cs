using System;
using System.Collections.Generic;
using System.IO;
using Havoc.Extensions;
using Havoc.IO.Tagfile.Binary.Sections;
using Havoc.Reflection;

namespace Havoc.IO.Tagfile.Binary.Types {
    public static class HkBinaryTypeReader {
        public static List<HkType> ReadTypeSection(BinaryReader reader, HkSection section) {
            var types = new List<HkType>();
            var typeStrings = new List<string>();
            var fieldStrings = new List<string>();

            foreach (var subSection in section.SubSections) {
                reader.BaseStream.Seek(subSection.Position, SeekOrigin.Begin);

                Debug.ReadProcess($"  Read Type section: {subSection.Signature}");
                switch (subSection.Signature) {
                    case "TPTR":
                        break;

                    case "TSTR":
                    case "TST1": {
                        while (reader.BaseStream.Position < subSection.Position + subSection.Length)
                            typeStrings.Add(reader.ReadNullTerminatedString());

                        break;
                    }

                    case "TNAM":
                    case "TNA1": {
                        int typeCount = (int)reader.ReadPackedInt();

                        Debug.TypeDef($"Typedef: count: {typeCount}");
                        types = new List<HkType>(typeCount);
                        for (int i = 0; i < typeCount; i++)
                            types.Add(new HkType());

                        var idx = 0;
                        foreach (var type in types) {
                            idx++;
                            type.Name = typeStrings[(int)reader.ReadPackedInt()];

                            Debug.TypeDef($"{idx} Read type {type}, name: {type.Name}");
                            int templateCount = (int)reader.ReadPackedInt();
                            if (templateCount >= 0x40) {
                                // for hkRootLevelContainer,
                                // 40 00 13 F8
                                // wtf? is first two bits flags?
                                // Debug.Temporary("POSITION: " + reader.BaseStream.Position);
                                // templateCount = (int)reader.ReadPackedInt();
                                templateCount &= 0x3F;
                            }
                            
                            Debug.TypeDef($"{idx} Read type parameters {templateCount}");
                            type.mParameters.Capacity = templateCount;
                            for (int j = 0; j < templateCount; j++) {
                                var templateInfo = new HkParameter {
                                    Name = typeStrings[(int)reader.ReadPackedInt()]
                                };

                                Debug.TypeDef($"{idx}-{j} Read type parameters {templateInfo.Name}");
                                if (templateInfo.Name[0] == 't')
                                    templateInfo.Value = ReadTypeIndex();
                                else
                                    templateInfo.Value = reader.ReadPackedInt();

                                type.mParameters.Add(templateInfo);
                            }

                            Debug.TypeDef($"{idx} Read type parameters end");
                        }

                        break;
                    }

                    case "FSTR":
                    case "FST1": {
                        while (reader.BaseStream.Position < subSection.Position + subSection.Length) {
                            var fieldStr = reader.ReadNullTerminatedString();
                            fieldStrings.Add(fieldStr);
                            Debug.TypeDef("field str: " + fieldStr);
                        }

                        break;
                    }

                    case "TBOD":
                    case "TBDY": {
                        while (reader.BaseStream.Position < subSection.Position + subSection.Length) {
                            var type = ReadTypeIndex();
                            if (type == null)
                                continue;

                            type.ParentType = ReadTypeIndex();
                            type.Flags = (HkTypeFlags)reader.ReadPackedInt();

                            if ((type.Flags & HkTypeFlags.HasFormatInfo) != 0) {
                                type.mFormatInfo = (int)reader.ReadPackedInt();
                            }

                            if ((type.Flags & HkTypeFlags.HasSubType) != 0) {
                                type.mSubType = ReadTypeIndex();
                                if (type.SubType != null) {
                                    Debug.TypeDef($"Type Read: SubType {type.mSubType.Name}");
                                }
                            }

                            if ((type.Flags & HkTypeFlags.HasVersion) != 0) {
                                type.mVersion = (int)reader.ReadPackedInt();
                            }

                            if ((type.Flags & HkTypeFlags.HasByteSize) != 0) {
                                type.mByteSize = (int)reader.ReadPackedInt();
                                Debug.TypeDef($"Type Read: ByteSize {type.mByteSize}");
                                type.mAlignment = (int)reader.ReadPackedInt();
                            }

                            if ((type.Flags & HkTypeFlags.HasUnknownFlags) != 0)
                                type.mUnknownFlags = (int)reader.ReadPackedInt();

                            if ((type.Flags & HkTypeFlags.HasFields) != 0) {
                                // int fieldCount = ( int ) reader.ReadPackedInt();
                                var b = (int)reader.ReadByte();
                                // For "hkPropertyId", the first byte is "C3", weird.
                                // The full data:
                                // C3 00 01 12 25 00 1A 16 00 2B 08 00 20
                                // If we ignore "C3 00", everything works like a charm
                                if (b >= 0x40) {
                                    Debug.TypeDef("Type Read: fieldCount > 0x40");
                                }

                                if (b == 0xC3) {
                                    Debug.TypeDef("Type Read: C3 in fieldCount");
                                    b = reader.ReadByte();
                                    if (b == 0) {
                                        b = (int)reader.ReadPackedInt();
                                    }
                                }

                                int fieldCount = (int)(b & 0x3F);
                                Debug.TypeDef($"Typedef {type.Name} ({fieldCount})");
                                type.mFields.Capacity = fieldCount;

                                for (int i = 0; i < fieldCount; i++) {
                                    var nameIdx = (int)reader.ReadPackedInt();
                                    var flags = (HkFieldFlags)reader.ReadPackedInt();
                                    var field = new HkField {
                                        Name = fieldStrings[nameIdx],
                                        Flags = flags,
                                        ByteOffset = (int)reader.ReadPackedInt(),
                                    };

                                    var fieldTypeIdx = reader.ReadPackedInt();
                                    field.Type = ReadTypeIndex(fieldTypeIdx);
                                    type.mFields.Add(field);
                                    Debug.TypeDef(
                                        $"    {field.Name}: {field.Type?.Name}, Flags: ({(int)field.Flags}) (offset {field.ByteOffset}) (typeidx: {fieldTypeIdx})");

                                    if (i == 0 && field.ByteOffset != 0 && field.ByteOffset % 8 != 0) {
                                        Debug.TypeDef("WARNING: Type first field offset % 8 != 0");
                                    }
                                }
                            }

                            if ((type.Flags & HkTypeFlags.HasInterfaces) != 0) {
                                int interfaceCount = (int)reader.ReadPackedInt();

                                type.mInterfaces.Capacity = interfaceCount;
                                for (int i = 0; i < interfaceCount; i++) {
                                    var itfce = new HkInterface {
                                        Type = ReadTypeIndex(),
                                        Flags = (int)reader.ReadPackedInt()
                                    };
                                    type.mInterfaces.Add(itfce);
                                }
                            }
                        }

                        break;
                    }

                    case "THSH": {
                        int hashCount = (int)reader.ReadPackedInt();
                        for (int i = 0; i < hashCount; i++)
                            ReadTypeIndex().Hash = reader.ReadInt32();

                        break;
                    }

                    case "TPAD":
                        break;

                    default:
                        throw new InvalidDataException($"Unexpected signature: {subSection.Signature}");
                }
            }

            return types;

            HkType ReadTypeIndex(long index = -1) {
                if (index == -1) {
                    index = reader.ReadPackedInt();
                }

                if (index == 0)
                    return null;

                return types[(int)index - 1];
            }
        }
    }
}