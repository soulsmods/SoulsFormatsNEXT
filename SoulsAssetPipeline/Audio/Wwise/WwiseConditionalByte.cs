using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseConditionalByte : WwiseObjectBase<WwiseConditionalByte>
    {
        public bool IsPresent;
        public byte Value;
        public string ConditionField;
        public List<int> CheckValues = null;
        public WwiseConditionalByte()
        {

        }

        public WwiseConditionalByte(string conditionField)
        {
            ConditionField = conditionField;
        }

        public WwiseConditionalByte(string conditionField, params int[] checkValues)
        {
            ConditionField = conditionField;
            CheckValues = checkValues.ToList();
        }

        internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            var cond = parent.GetFieldAnyIntValue(ConditionField);
            if ((CheckValues != null && CheckValues.Contains(cond)) || (CheckValues == null && cond != 0))
            {
                Value = br.ReadByte();
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
