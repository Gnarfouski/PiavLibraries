using System;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class PiavFullRoadXmlRegeneration : MonoBehaviour
{
    private void Update()
    {
        if (DoRegenerate)
        {
            DoRegenerate = false;
            Regenerate();
        }
    }

    internal void Regenerate()
    {
        var xmlDoc = CreateCoreXMLDocument();

        XmlNode roads = xmlDoc.CreateElement("Roads");
        xmlDoc.FirstChild.AppendChild(roads);

        XmlNode contacts = xmlDoc.CreateElement("Contacts");
        xmlDoc.FirstChild.AppendChild(contacts);

        XmlNode stops = xmlDoc.CreateElement("Stops");
        xmlDoc.FirstChild.AppendChild(stops);

        XmlNode parking = xmlDoc.CreateElement("Parking");
        xmlDoc.FirstChild.AppendChild(parking);

        if (FindObjectsOfType(typeof(PiavRoadInterface)) is PiavRoadInterface[] allRoads)
            foreach (var r in allRoads)
            {
                if (r.isActiveAndEnabled)
                {
                    r.LoadRoadMesh();
                    r.RegenerateMesh();
                    r.LoadRegressedFunctions();
                    RoadXmlGeneration.GenerateRoadXml(xmlDoc, r);
                }
            }

        if (FindObjectsOfType(typeof(PiavStoppingData)) is PiavStoppingData[] allStops)
            foreach (var s in allStops)
                if (s.isActiveAndEnabled) RoadXmlGeneration.GenerateStopItemXml(xmlDoc, stops, s);

        if (FindObjectsOfType(typeof(PiavParkingSpaceData)) is PiavParkingSpaceData[] allParks)
            foreach (var p in allParks)
                if (p.isActiveAndEnabled) RoadXmlGeneration.GenerateParkingSpaceXml(xmlDoc, parking, p);

        xmlDoc.Save(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "Info.xml");
    }

    private XmlDocument CreateCoreXMLDocument()
    {
        var     xmlDoc   = new XmlDocument();
        XmlNode rootNode = xmlDoc.CreateElement(SceneManager.GetActiveScene().name);
        xmlDoc.AppendChild(rootNode);

        var creationDate   = xmlDoc.CreateAttribute("date");
        creationDate.Value = DateTime.Now.ToShortDateString();
        if (rootNode.Attributes != null) rootNode.Attributes.Append(creationDate);
        else
            throw new NullReferenceException();

        return xmlDoc;
    }

    public bool DoRegenerate = false;
}