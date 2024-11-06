using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseIDList : WwiseObjectBase<WwiseIDList>, IList<uint>
    {
        private List<uint> list;
        private string ListCountField;
        public WwiseIDList()
        {

        }

        public WwiseIDList(string listCountField)
        {
            ListCountField = listCountField;
        }

        internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            int listCount = parent.GetFieldAnyIntValue(ListCountField);
            list = br.ReadUInt32s(listCount).ToList();
            return true;
        }

        internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(uint item)
        {
            return ((IList<uint>)list).IndexOf(item);
        }

        public void Insert(int index, uint item)
        {
            ((IList<uint>)list).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<uint>)list).RemoveAt(index);
        }

        public uint this[int index] { get => ((IList<uint>)list)[index]; set => ((IList<uint>)list)[index] = value; }

        public void Add(uint item)
        {
            ((ICollection<uint>)list).Add(item);
        }

        public void Clear()
        {
            ((ICollection<uint>)list).Clear();
        }

        public bool Contains(uint item)
        {
            return ((ICollection<uint>)list).Contains(item);
        }

        public void CopyTo(uint[] array, int arrayIndex)
        {
            ((ICollection<uint>)list).CopyTo(array, arrayIndex);
        }

        public bool Remove(uint item)
        {
            return ((ICollection<uint>)list).Remove(item);
        }

        public int Count => ((ICollection<uint>)list).Count;

        public bool IsReadOnly => ((ICollection<uint>)list).IsReadOnly;

        public IEnumerator<uint> GetEnumerator()
        {
            return ((IEnumerable<uint>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }
    }
}
