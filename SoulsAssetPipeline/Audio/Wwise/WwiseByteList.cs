using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseByteList : WwiseObjectBase<WwiseIDList>, IList<byte>
    {
        private List<byte> list;
        private string ListCountField;
        public WwiseByteList()
        {

        }

        public WwiseByteList(string listCountField)
        {
            ListCountField = listCountField;
        }

        internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            int listCount = parent.GetFieldAnyIntValue(ListCountField);
            list = br.ReadBytes(listCount).ToList();
            return true;
        }

        internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(byte item)
        {
            return ((IList<byte>)list).IndexOf(item);
        }

        public void Insert(int index, byte item)
        {
            ((IList<byte>)list).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<byte>)list).RemoveAt(index);
        }

        public byte this[int index] { get => ((IList<byte>)list)[index]; set => ((IList<byte>)list)[index] = value; }

        public void Add(byte item)
        {
            ((ICollection<byte>)list).Add(item);
        }

        public void Clear()
        {
            ((ICollection<byte>)list).Clear();
        }

        public bool Contains(byte item)
        {
            return ((ICollection<byte>)list).Contains(item);
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            ((ICollection<byte>)list).CopyTo(array, arrayIndex);
        }

        public bool Remove(byte item)
        {
            return ((ICollection<byte>)list).Remove(item);
        }

        public int Count => ((ICollection<byte>)list).Count;

        public bool IsReadOnly => ((ICollection<byte>)list).IsReadOnly;

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }
    }
}
