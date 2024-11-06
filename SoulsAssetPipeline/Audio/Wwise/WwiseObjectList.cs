using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseObjectList<T> : WwiseObjectBase<WwiseObjectList<T>>, IList<T>
        where T : WwiseObjectBase<T>, new()
    {
        private List<T> list;
        private string ListCountField;
        public WwiseObjectList()
        {

        }

        public WwiseObjectList(string listCountField)
        {
            ListCountField = listCountField;
        }

        internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
        {
            int listCount = parent.GetFieldAnyIntValue(ListCountField);
            list = new List<T>();
            for (int i = 0; i < listCount; i++)
            {
                var t = new T();
                t.Read(br, parent);
                list.Add(t);
            }
            return true;
        }

        internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)list).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)list).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)list).RemoveAt(index);
        }

        public T this[int index] { get => ((IList<T>)list)[index]; set => ((IList<T>)list)[index] = value; }

        public void Add(T item)
        {
            ((ICollection<T>)list).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)list).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)list).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)list).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)list).Remove(item);
        }

        public int Count => ((ICollection<T>)list).Count;

        public bool IsReadOnly => ((ICollection<T>)list).IsReadOnly;

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }
    }
}
