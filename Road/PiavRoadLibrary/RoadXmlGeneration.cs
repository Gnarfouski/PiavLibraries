using System;
using System.Globalization;
using System.Xml;

internal static class RoadXmlGeneration
{
    /// <summary>Class containing individual functions for the navigation xml file.
    /// </summary>

    internal static void GenerateRoadXml(XmlDocument core, PiavRoadInterface target)
    {
        foreach (XmlNode node in core.FirstChild.ChildNodes)
        {
            if(node.Name.Equals("Roads"))
                MakeRoadNode(core, node, (uint)target.GetInstanceID(), target);
            else if (node.Name.Equals("Contacts"))
                if (target._roadContacts != null)
                    foreach (var rc in target._roadContacts) MakeContactNode(core, node, rc, (uint)target.GetInstanceID());
        }
    }

    private static void MakeRoadNode(XmlDocument core, XmlNode parent, long roadMeshName, PiavRoadInterface target)
    {
        XmlNode mainNode = core.CreateElement("RM" + roadMeshName);
        parent.AppendChild(mainNode);

        var aidr = core.CreateAttribute("idr");
        var anblanes    = core.CreateAttribute("nblanes");
        var anbsegments = core.CreateAttribute("nbsegments");

        aidr.Value = roadMeshName.ToString();
        anblanes.Value    = target._lanePolynoms.GetLength(0).ToString();
        anbsegments.Value = target._lanePolynoms.GetLength(1).ToString();

        if (mainNode.Attributes != null)
        {
            mainNode.Attributes.Append(anblanes);
            mainNode.Attributes.Append(anbsegments);
            mainNode.Attributes.Append(aidr);
        }

        for (var j = 0; j < target._lanePolynoms.GetLength(0); j++) MakeLaneNode(core, mainNode, roadMeshName * 100 + j + 1, target._laneWidths[j], target._laneDirections[j], target, j);
    }

    private static void MakeLaneNode(XmlDocument core, XmlNode parent, long id, double width, bool reverse, PiavRoadInterface target, int j)
    {
        XmlNode laneNode = core.CreateElement("Lane");
        parent.AppendChild(laneNode);

        for (var i = 0; i < target._lanePolynoms.GetLength(1); i++)
        {
            MakeSegmentNode(core, laneNode, id * 1000 + i + 1, target._lanePolynoms[j, i],
                            target._arcLengthRegressionValues[j, i, 0, 0],
                            target._arcLengthRegressionValues[j, i, 0, 1],
                            target._arcLengthRegressionValues[j, i, 0, 2],
                            target._arcLengthRegressionValues[j, i, 1, 0],
                            target._arcLengthRegressionValues[j, i, 1, 1],
                            target._arcLengthRegressionValues[j, i, 1, 2]);
        }

        var aidl       = core.CreateAttribute("idl");
        var awidth     = core.CreateAttribute("width");
        var adirection = core.CreateAttribute("reverse");
        var asplimit   = core.CreateAttribute("splimit");
        var atype = core.CreateAttribute("type");

        aidl.Value       = id.ToString(CultureInfo.InvariantCulture);
        awidth.Value     = width.ToString(CultureInfo.InvariantCulture);
        adirection.Value = reverse.ToString(CultureInfo.InvariantCulture);
        asplimit.Value   = target._speedLimit.ToString(CultureInfo.InvariantCulture);
        atype.Value = ((int)target._laneTypes[j]).ToString();

        if (laneNode.Attributes != null)
        {
            laneNode.Attributes.Append(aidl);
            laneNode.Attributes.Append(awidth);
            laneNode.Attributes.Append(adirection);
            laneNode.Attributes.Append(asplimit);
            laneNode.Attributes.Append(atype);
        }
        else
        {
            throw new NullReferenceException();
        }
    }

    private static void MakeSegmentNode(XmlDocument core, XmlNode parent, long id, QuadraticPolynomial p, double a1, double a2, double a3, double b1, double b2, double b3)
    {
        var     coeffs      = p._coeffs;
        XmlNode segmentNode = core.CreateElement("Segment");

        parent.AppendChild(segmentNode);

        var at2X        = core.CreateAttribute("ax");
        var at2Y        = core.CreateAttribute("ay");
        var at2Z        = core.CreateAttribute("az");
        var at1X        = core.CreateAttribute("bx");
        var at1Y        = core.CreateAttribute("by");
        var at1Z        = core.CreateAttribute("bz");
        var at0X        = core.CreateAttribute("cx");
        var at0Y        = core.CreateAttribute("cy");
        var at0Z        = core.CreateAttribute("cz");
        var aarcLen3    = core.CreateAttribute("al");
        var aarcLen2     = core.CreateAttribute("bl");
        var aarcLen = core.CreateAttribute("cl");
        var ainvArcLen3 = core.CreateAttribute("la");
        var ainvArcLen2  = core.CreateAttribute("lb");
        var ainvArcLen = core.CreateAttribute("lc");
        var aid         = core.CreateAttribute("id");

        at2X.Value        = coeffs[0, 0].ToString("n4", CultureInfo.InvariantCulture);
        at2Y.Value        = coeffs[0, 1].ToString("n4", CultureInfo.InvariantCulture);
        at2Z.Value        = coeffs[0, 2].ToString("n4", CultureInfo.InvariantCulture);
        at1X.Value        = coeffs[1, 0].ToString("n4", CultureInfo.InvariantCulture);
        at1Y.Value        = coeffs[1, 1].ToString("n4", CultureInfo.InvariantCulture);
        at1Z.Value        = coeffs[1, 2].ToString("n4", CultureInfo.InvariantCulture);
        at0X.Value        = coeffs[2, 0].ToString("n4", CultureInfo.InvariantCulture);
        at0Y.Value        = coeffs[2, 1].ToString("n4", CultureInfo.InvariantCulture);
        at0Z.Value        = coeffs[2, 2].ToString("n4", CultureInfo.InvariantCulture);
        aarcLen3.Value = a1.ToString("n6", CultureInfo.InvariantCulture);
        aarcLen2.Value    = a2.ToString("n6", CultureInfo.InvariantCulture);
        aarcLen.Value     = a3.ToString("n6", CultureInfo.InvariantCulture);
        ainvArcLen3.Value = b1.ToString("n6", CultureInfo.InvariantCulture);
        ainvArcLen2.Value = b2.ToString("n6", CultureInfo.InvariantCulture);
        ainvArcLen.Value  = b3.ToString("n6", CultureInfo.InvariantCulture);
        aid.Value         = id.ToString();

        if (segmentNode.Attributes != null)
        {
            segmentNode.Attributes.Append(at2X);
            segmentNode.Attributes.Append(at2Y);
            segmentNode.Attributes.Append(at2Z);
            segmentNode.Attributes.Append(at1X);
            segmentNode.Attributes.Append(at1Y);
            segmentNode.Attributes.Append(at1Z);
            segmentNode.Attributes.Append(at0X);
            segmentNode.Attributes.Append(at0Y);
            segmentNode.Attributes.Append(at0Z);
            segmentNode.Attributes.Append(aarcLen3);
            segmentNode.Attributes.Append(aarcLen2);
            segmentNode.Attributes.Append(aarcLen);
            segmentNode.Attributes.Append(ainvArcLen3);
            segmentNode.Attributes.Append(ainvArcLen2);
            segmentNode.Attributes.Append(ainvArcLen);
            segmentNode.Attributes.Append(aid);
        }
        else
        {
            throw new NullReferenceException();
        }
    }

    private static void MakeContactNode(XmlDocument xmlDoc, XmlNode parent, RoadContact rc, long id)
    {
        if (rc._target != null)
        {
            XmlNode rcn = xmlDoc.CreateElement("Contact");
            parent.AppendChild(rcn);

            var startRm    = xmlDoc.CreateAttribute("srm");
            var endRm      = xmlDoc.CreateAttribute("erm");
            var startState = xmlDoc.CreateAttribute("ss");
            var endState   = xmlDoc.CreateAttribute("es");

            startRm.Value    = id.ToString(CultureInfo.InvariantCulture);
            endRm.Value      = ((uint)rc._target.GetInstanceID()).ToString(CultureInfo.InvariantCulture);
            startState.Value = ((int)rc._start).ToString(CultureInfo.InvariantCulture);
            endState.Value   = ((int)rc._end).ToString(CultureInfo.InvariantCulture);

            if (rcn.Attributes != null)
            {
                rcn.Attributes.Append(startRm);
                rcn.Attributes.Append(endRm);
                rcn.Attributes.Append(startState);
                rcn.Attributes.Append(endState);
            }
            else
            {
                throw new NullReferenceException();
            }
        }
    }

    //*
    internal static void GenerateStopItemXml(XmlDocument xmlDoc, XmlNode stops, PiavStoppingData sd)
    {
        XmlNode s = xmlDoc.CreateElement("Stop");
        stops.AppendChild(s);

        var rm = xmlDoc.CreateAttribute("rm");
        var startT = xmlDoc.CreateAttribute("start");
        var cycleT = xmlDoc.CreateAttribute("cycle");
        var type = xmlDoc.CreateAttribute("type");

        var x = xmlDoc.CreateAttribute("x");
        var y = xmlDoc.CreateAttribute("y");
        var z = xmlDoc.CreateAttribute("z");

        rm.Value = ((uint)sd._target.GetInstanceID()).ToString(CultureInfo.InvariantCulture);
        startT.Value = sd._start.ToString(CultureInfo.InvariantCulture);
        cycleT.Value = sd._cycle.ToString(CultureInfo.InvariantCulture);
        type.Value = ((int)sd._type).ToString(CultureInfo.InvariantCulture);

        x.Value = sd.transform.position.x.ToString(CultureInfo.InvariantCulture);
        y.Value = sd.transform.position.y.ToString(CultureInfo.InvariantCulture);
        z.Value = sd.transform.position.z.ToString(CultureInfo.InvariantCulture);

        if (s.Attributes != null)
        {
            s.Attributes.Append(rm);
            s.Attributes.Append(x);
            s.Attributes.Append(y);
            s.Attributes.Append(z);
            s.Attributes.Append(startT);
            s.Attributes.Append(cycleT);
            s.Attributes.Append(type);
        }
        else
        {
            throw new NullReferenceException();
        }
    }
    //*/

    //*
    internal static void GenerateParkingSpaceXml(XmlDocument xmlDoc, XmlNode parent, PiavParkingSpaceData ps)
    {
        XmlNode park = xmlDoc.CreateElement("Parking");
        parent.AppendChild(park);

        var pType = xmlDoc.CreateAttribute("type");
        var ax = xmlDoc.CreateAttribute("x");
        var ay = xmlDoc.CreateAttribute("y");
        var az = xmlDoc.CreateAttribute("z");
        var axd = xmlDoc.CreateAttribute("xd");
        var ayd = xmlDoc.CreateAttribute("yd");
        var azd = xmlDoc.CreateAttribute("zd");
        var road = xmlDoc.CreateAttribute("rm");

        pType.Value = ((int)ps._spaceType).ToString(CultureInfo.InvariantCulture);
        ax.Value = ps.transform.position.x.ToString(CultureInfo.InvariantCulture);
        ay.Value = ps.transform.position.y.ToString(CultureInfo.InvariantCulture);
        az.Value = ps.transform.position.z.ToString(CultureInfo.InvariantCulture);
        axd.Value = ps.transform.forward.x.ToString(CultureInfo.InvariantCulture);
        ayd.Value = ps.transform.forward.y.ToString(CultureInfo.InvariantCulture);
        azd.Value = ps.transform.forward.z.ToString(CultureInfo.InvariantCulture);
        road.Value = ((uint)ps._roadInterface.GetInstanceID()).ToString(CultureInfo.InvariantCulture);

        if (park.Attributes != null)
        {
            park.Attributes.Append(road);
            park.Attributes.Append(ax);
            park.Attributes.Append(ay);
            park.Attributes.Append(az);
            park.Attributes.Append(axd);
            park.Attributes.Append(ayd);
            park.Attributes.Append(azd);
            park.Attributes.Append(pType);
        }
        else
        {
            throw new NullReferenceException();
        }
    }
    //*/
}
