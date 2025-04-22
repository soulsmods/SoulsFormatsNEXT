using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SoulsFormats
{
    public partial class PARAMDBP
    {
        /// <summary>
        /// Serializes and deserializes params and dbps to xml.
        /// </summary>
        public class XmlSerializer
        {
            #region Serialize

            /// <summary>
            /// Serialize a dbp to an xml.
            /// </summary>
            /// <param name="dbp">A dbp to serialize.</param>
            /// <param name="xw">An xml writer.</param>
            /// <param name="name">The name of the dbp if applicable.</param>
            public static void SerializeDbp(PARAMDBP dbp, XmlWriter xw, string name = "")
            {
                xw.WriteStartElement("dbp");
                xw.WriteElementString("BigEndian", dbp.BigEndian.ToString());
                xw.WriteElementString("Name", name);
                xw.WriteStartElement("Fields");
                foreach (var field in dbp.Fields)
                {
                    xw.WriteStartElement("Field");
                    xw.WriteElementString("Name", field.Name);
                    xw.WriteElementString("Format", field.Format);
                    xw.WriteElementString("Type", field.Type.ToString());
                    xw.WriteElementString("Default", field.Default.ToString());
                    xw.WriteElementString("Minimum", field.Minimum.ToString());
                    xw.WriteElementString("Maximum", field.Maximum.ToString());
                    xw.WriteElementString("Increment", field.Increment.ToString());
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.Dispose();
            }

            /// <summary>
            /// Serialize a param that has an applied dbp to an xml.
            /// </summary>
            /// <param name="param">A param to serialize.</param>
            /// <param name="xw">An xml writer.</param>
            /// <param name="name">The name of the param if applicable.</param>
            public static void SerializeParam(DBPPARAM param, XmlWriter xw, string name = "")
            {
                if (!param.DbpApplied)
                    throw new InvalidDataException("Dbp has not been applied.");

                xw.WriteStartElement("dbpparam");
                xw.WriteElementString("Name", name);
                xw.WriteStartElement("Cells");
                foreach (var cell in param.Cells)
                {
                    xw.WriteStartElement("Cell");
                    xw.WriteElementString("Name", cell.Name);
                    xw.WriteElementString("Format", cell.Format);
                    xw.WriteElementString("Type", cell.Type.ToString());
                    xw.WriteElementString("Default", cell.Default.ToString());
                    xw.WriteElementString("Minimum", cell.Minimum.ToString());
                    xw.WriteElementString("Maximum", cell.Maximum.ToString());
                    xw.WriteElementString("Increment", cell.Increment.ToString());
                    xw.WriteElementString("Value", cell.Value.ToString());
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.Dispose();
            }

            #endregion

            #region Deserialize

            /// <summary>
            /// Deserialize an xml to a dbp.
            /// </summary>
            /// <param name="xml">An xml document.</param>
            /// <returns>A new dbp.</returns>
            public static PARAMDBP DeserializeDbp(XmlDocument xml)
            {
                var dbp = new PARAMDBP();
                bool bigendian = bool.Parse(xml.SelectSingleNode("dbp/BigEndian").InnerText);
                var fieldsNode = xml.SelectSingleNode("dbp/Fields");
                foreach (XmlNode fieldNode in fieldsNode.SelectNodes("Field"))
                {
                    var field = new Field();
                    string name = fieldNode.SelectSingleNode("Name").InnerText;
                    string format = fieldNode.SelectSingleNode("Format").InnerText;
                    var type = (PARAMDEF.DefType)Enum.Parse(typeof(PARAMDEF.DefType), fieldNode.SelectSingleNode("Type").InnerText);
                    object defaultValue = Field.ConvertToDefType(fieldNode.SelectSingleNode("Default").InnerText, type);
                    object minimum = Field.ConvertToDefType(fieldNode.SelectSingleNode("Minimum").InnerText, type);
                    object maximum = Field.ConvertToDefType(fieldNode.SelectSingleNode("Maximum").InnerText, type);

                    object increment = Field.ConvertToDefType(fieldNode.SelectSingleNode("Increment").InnerText, type);

                    field.Name = name;
                    field.Format = format;
                    field.Type = type;
                    field.Default = defaultValue;
                    field.Minimum = minimum;
                    field.Maximum = maximum;
                    field.Increment = increment;
                    dbp.Fields.Add(field);
                }

                dbp.BigEndian = bigendian;

                return dbp;
            }

            /// <summary>
            /// Deserialize an xml to a param.
            /// </summary>
            /// <param name="xml">An xml document.</param>
            /// <returns>A new param.</returns>
            public static DBPPARAM DeserializeParam(XmlDocument xml)
            {
                var dbp = new PARAMDBP();
                var cellsNode = xml.SelectSingleNode("dbpparam/Cells");
                var paramValues = new List<object>();
                foreach (XmlNode cellNode in cellsNode.SelectNodes("Cell"))
                {
                    var field = new Field();
                    string name = cellNode.SelectSingleNode("Name").InnerText;
                    string format = cellNode.SelectSingleNode("Format").InnerText;
                    var type = (PARAMDEF.DefType)Enum.Parse(typeof(PARAMDEF.DefType), cellNode.SelectSingleNode("Type").InnerText);
                    object defaultValue = Field.ConvertToDefType(cellNode.SelectSingleNode("Default").InnerText, type);
                    object minimum = Field.ConvertToDefType(cellNode.SelectSingleNode("Minimum").InnerText, type);
                    object maximum = Field.ConvertToDefType(cellNode.SelectSingleNode("Maximum").InnerText, type);
                    object increment = Field.ConvertToDefType(cellNode.SelectSingleNode("Increment").InnerText, type);
                    object value = Field.ConvertToDefType(cellNode.SelectSingleNode("Value").InnerText, type);

                    field.Name = name;
                    field.Format = format;
                    field.Type = type;
                    field.Default = defaultValue;
                    field.Minimum = minimum;
                    field.Maximum = maximum;
                    field.Increment = increment;
                    paramValues.Add(value);

                    dbp.Fields.Add(field);
                }

                var param = new DBPPARAM(dbp);
                for (int i = 0; i < paramValues.Count; i++)
                    param.Cells[i].Value = paramValues[i];

                return param;
            }

            #endregion
        }
    }
}