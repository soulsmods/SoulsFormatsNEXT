using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public abstract class WwiseObjectBase<T> : IWwiseObject
        where T : WwiseObjectBase<T>, new()
    {
        internal virtual bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            return false;
        }

        internal virtual bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
        {
            return false;
        }


        static FieldInfo[] fieldsCache;
        static Dictionary<string, FieldInfo> fieldsByName = new Dictionary<string, FieldInfo>();

        protected object GetFieldValue(string fieldName)
        {
            BuildFieldsCacheIfNeeded();

            if (fieldsByName.ContainsKey(fieldName))
                return fieldsByName[fieldName].GetValue(this);
            return null;
        }

        public int GetFieldAnyIntValue(string fieldName)
        {
            BuildFieldsCacheIfNeeded();
            fieldsCache = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.MetadataToken).ToArray();
            fieldsByName = fieldsCache.ToDictionary(f => f.Name, f => f);
            if (fieldsByName.ContainsKey(fieldName))
            {
                var fieldValue = fieldsByName[fieldName].GetValue(this);
                switch (fieldValue)
                {
                    case int asInt: return asInt;
                    case uint asUint: return (int)asUint;
                    case ushort asUshort: return asUshort;
                    case short asShort: return asShort;
                    case byte asByte: return asByte;
                    case sbyte asSbyte: return asSbyte;
                    case bool asBool: return asBool ? 1 : 0;
                    case WwiseVarint asWwiseVarint: return asWwiseVarint.Value;
                }
            }
            throw new ArgumentException($"Field {fieldName} does not exist or is not an integer.");
        }

        private void BuildFieldsCacheIfNeeded()
        {
            if (fieldsCache == null || fieldsByName == null || fieldsCache.Length == 0 || fieldsByName.Count == 0)
            {
                fieldsCache = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.MetadataToken).ToArray();
                fieldsByName = fieldsCache.ToDictionary(f => f.Name, f => f);
            }
        }

        public void ReadField(BinaryReaderEx br, string fieldName)
        {
            BuildFieldsCacheIfNeeded();

            var f = fieldsByName[fieldName];
            var fieldValue = f.GetValue(this);

            if (f == null || fieldValue == null)
            {
                Console.WriteLine("fuck");
            }

            switch (fieldValue)
            {
                case int:
                    f.SetValue(this, br.ReadInt32());
                    break;
                case uint:
                    f.SetValue(this, br.ReadUInt32());
                    break;
                case ushort:
                    f.SetValue(this, br.ReadUInt16());
                    break;
                case short:
                    f.SetValue(this, br.ReadInt16());
                    break;
                case float:
                    f.SetValue(this, br.ReadSingle());
                    break;
                case byte:
                    f.SetValue(this, br.ReadByte());
                    break;
                case sbyte:
                    f.SetValue(this, br.ReadSByte());
                    break;
                case bool:
                    f.SetValue(this, br.ReadByte() != 0);
                    break;
                case WwiseVarint:
                    f.SetValue(this, WwiseVarint.Read(br));
                    break;
                case IWwiseObject:
                    ((IWwiseObject)fieldValue).Read(br, this);
                    f.SetValue(this, fieldValue);
                    break;
            }
        }

        public void Read(BinaryReaderEx br, IWwiseObject parent)
        {
            if (CustomRead(br, parent))
                return;

            BuildFieldsCacheIfNeeded();
            foreach (var f in fieldsCache)
            {
                ReadField(br, f.Name);
            }
        }

        public void Write(BinaryWriterEx bw, IWwiseObject parent)
        {
            if (CustomWrite(bw, parent))
                return;
            throw new NotImplementedException();
        }
    }
}
