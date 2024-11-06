using System.Collections.Generic;
using System.Xml;
using Havoc.Objects;

namespace Havoc.IO.Tagfile.Xml
{
    public interface IHkXmlTagfileWriter
    {
        void Write( XmlWriter writer, IHkObject rootObject );
        void Write(XmlWriter writer, List<IHkObject> rootObject);
    }
}