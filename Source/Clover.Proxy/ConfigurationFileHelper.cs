using System;
using System.Collections.Generic;
using System.Xml;

namespace Clover.Proxy
{
    /// <summary>
    /// Provides an feature to modify configuration file.
    /// </summary>
    internal static class ConfigurationFileHelper
    {
        internal static void Merge(ref XmlDocument xmlDoc, ref XmlDocument xmlDoc2)
        {
            Merge(ref xmlDoc, ref xmlDoc2, "configuration/appSettings", "add", "key");
            Merge(ref xmlDoc, ref xmlDoc2, "configuration/HibernateMappingAssemblies", "add", "key");
            Merge(ref xmlDoc, ref xmlDoc2, "configuration/databaseSettings", "add", "key");

            XmlNode log4net2 = xmlDoc2.SelectSingleNode("configuration/log4net");

            XmlNode log4net1 = xmlDoc.SelectSingleNode("configuration/log4net");

            if (log4net1 != null && log4net2 != null)
            {
                log4net1.InnerXml = log4net2.InnerXml;
            }
        }

        private static void Merge(ref XmlDocument xml1, ref XmlDocument xml2, string path, string tag, string key)
        {
            foreach (XmlNode node2 in xml2.SelectNodes(path + "/" + tag))
            {
                string xpath = string.Format(path + "/{1}[@{2}='{0}']", node2.Attributes[key].Value, tag, key);

                XmlNode node1 = xml1.SelectSingleNode(xpath);
                if (node1 == null)
                {
                    node1 = xml1.CreateElement(node2.Name);
                    XmlNode pNode = xml1.SelectSingleNode(path);
                    if (pNode != null)
                    {
                        pNode.AppendChild(node1);
                    }
                }
                foreach (XmlAttribute att2 in node2.Attributes)
                {
                    XmlAttribute att1 = node1.Attributes[att2.Name];
                    if (node1.Attributes[att2.Name] == null)
                    {
                        att1 = node1.OwnerDocument.CreateAttribute(att2.Name);
                        node1.Attributes.Append(att1);
                    }
                    att1.Value = att2.Value;
                }
            }
        }
    }
}