using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoulsFormats;

public class GXListDef : List<GXListDef.EXParamDef>
{
    public class EXParamDef
    {
        public string ID { get; set; }
        public int Unk04 { get; set; }
        public string Category { get; set; } = null;
        public List<ValueDef> Items { get; set; } = new List<ValueDef>();
    }
    
    public enum ValueType
    {
        Unknown = 0,
        Int = 1,
        Float = 2,
        Enum = 3, // int with specific values
        Bool = 4, // int (0 - 1)
    }

    public class ValueDef
    {
        public string Name { get; set; }
        public ValueType Type { get; set; }
        public float Min { get; set; } = 0;
        public float Max { get; set; } = 0;
        public Dictionary<int, string> Enum { get; set; } = null;
    }

    public static GXListDef Read(string filename)
    {
        return JsonSerializer.Deserialize<GXListDef>(File.OpenRead(filename));
    }
}