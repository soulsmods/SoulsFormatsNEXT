using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseConditionalObject<T> : WwiseObjectBase<WwiseConditionalObject<T>>
        where T : WwiseObjectBase<T>, new()
    {
        public bool IsPresent;
        public T Value;
        public string ConditionField;
        public List<int> CheckValues = null;
        public WwiseConditionalObject()
        {

        }

        public WwiseConditionalObject(string conditionField)
        {
            ConditionField = conditionField;
        }

        public WwiseConditionalObject(string conditionField, params int[] checkValues)
        {
            ConditionField = conditionField;
            CheckValues = checkValues.ToList();
        }

        internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            var cond = parent.GetFieldAnyIntValue(ConditionField);
            if ((CheckValues != null && CheckValues.Contains(cond)) || (CheckValues == null && cond != 0))
            {
                var t = new T();
                t.Read(br, parent);
                Value = t;
                IsPresent = true;
            }
            else
            {
                IsPresent = false;
            }
            return true;
        }

        internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
        {
            throw new NotImplementedException();
        }

    }
}
