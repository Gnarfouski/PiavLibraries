using System;
using System.Xml;

internal static class XmlMethods
{
    internal static XmlDocument CreateCoreXmlDocument()
    {
        var     xmlDoc   = new XmlDocument();
        XmlNode rootNode = xmlDoc.CreateElement("Road XML Data");
        xmlDoc.AppendChild(rootNode);

        var creationDate   = xmlDoc.CreateAttribute("date");
        creationDate.Value = DateTime.Now.ToShortDateString();

        if (rootNode.Attributes != null)
            rootNode.Attributes.Append(creationDate);
        else
            throw new NullReferenceException();

        return xmlDoc;
    }
}
