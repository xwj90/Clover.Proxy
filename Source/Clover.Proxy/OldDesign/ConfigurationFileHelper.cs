using System;
using System.Collections.Generic;
using System.Xml;

namespace Clover.Proxy.OldDesign
{
    /// <summary>
    /// Provides an feature to modify configuration file.
    /// </summary>
    public class ConfigurationFileHelper
    {
        private static Dictionary<string, string> conns;

        public static Dictionary<string, string> ConnectionStrings
        {
            get
            {
                if (conns == null)
                {
                    var xmldoc = new XmlDocument();
                    xmldoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                    conns = new Dictionary<string, string>();
                    foreach (XmlNode node in xmldoc.SelectNodes("/configuration/databaseSettings/*"))
                    {
                        conns[node.Attributes["key"].Value] = node.Attributes["value"].Value;
                    }
                }
                return conns;
            }
        }

        public static void Merge(ref XmlDocument xmlDoc, ref XmlDocument xmlDoc2)
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