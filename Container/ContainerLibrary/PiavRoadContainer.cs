using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PiavRoadContainer : MonoBehaviour
{
    //*
    private void Awake()
    {
        _segments     = new List<Segment>();
        _lanes        = new List<Lane>();
        _roads = new List<Road>();
        _parkingSpaces = new List<ParkingSpace>();
        _cars = new List<PiavRoadAgent>();

        var xmlDoc = new XmlDocument();

        if (File.Exists(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "Info.xml"))
        {
            xmlDoc.Load(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "Info.xml");

            foreach (XmlNode n in xmlDoc.FirstChild.ChildNodes)
            {
                if (n.Name.Equals("Roads"))
                {
                    foreach (XmlNode road in n.ChildNodes) LoadRoadNode(road);
                    LoadCoreContacts();
                }
                else if (n.Name.Equals("Contacts"))
                {
                    foreach (XmlNode contact in n.ChildNodes) LoadContactNode(contact);
                }
                else if (n.Name.Equals("Stops"))
                {
                    foreach (XmlNode stop in n.ChildNodes) LoadStopNode(stop);
                }
                else if (n.Name.Equals("Parking"))
                {
                    foreach (XmlNode park in n.ChildNodes) LoadParkingNode(park);
                }
            }
        }
        else
        {
            Debug.LogError("RoadContainer : No XML file to load.");
        }

        for (int i = 0; i < _segments.Count; i++) _segments[i].SelfIndex = i;

        BuildDualGraph();
        LinkDualGraph();

        foreach (var agent in (PiavRoadAgent[])FindObjectsOfType(typeof(PiavRoadAgent))) _cars.Add(agent);
    }

    private void Update()
    {
        foreach (var car in _cars)
        {
            if (car.CurrentPosition.HasValue)
            {
                var positionDelta          = car.transform.position - car.CurrentPosition;
                car.CurrentAbsoluteSpeed   = positionDelta.Value.magnitude / Time.deltaTime;
                car.CurrentMovingDirection = positionDelta.Value.normalized;
            }

            var posWithoutY = car.transform.position;
            posWithoutY.y = 0;
            car.CurrentPosition        = posWithoutY;
            car.CurrentFacingDirection = car.transform.forward;

            var closestSegment = FindClosestSegmentWithinLaneWidth(car.CurrentPosition.Value, LaneType.Vehicle);
                if (closestSegment != null && (car.DesiredIncomingPath == null ||car.DesiredIncomingPath.Count==0))
                {
                    car.CurrentSegment = closestSegment.Item1;
                    car.CurrentRoot    = closestSegment.Item2;
                }

            car.CurrentDirectionalSpeed = car.CurrentSegment != null ?
                                              car.CurrentAbsoluteSpeed *
                                              Vector3.Dot(car.CurrentFacingDirection.Value,
                                                          RoadUtilities.CalculateFirstDerivative(car.CurrentSegment.Polynomial, car.CurrentRoot).normalized) *
                                              (car.CurrentSegment.ParentLane.Direction ? -1 : 1) :
                                              car.CurrentAbsoluteSpeed;
        }
    }

    private void BuildDualGraph()
    {
        var tempDualGraph = new List<DualVertex>[_segments.Count];

        for (int i = 0; i < _segments.Count; i++)
        {
            var divergenceRoots = new List<double>();

            foreach (var c in _segments[i].Contacts) divergenceRoots.Add(c.OriginRoot);
            foreach (var c in _segments[i].IncomingContacts) divergenceRoots.Add(c.TargetRoot);

            divergenceRoots.Sort();

            int countToDelete = 0;

            for (int j = 0; j < divergenceRoots.Count - 1; j++)
            {
                if (divergenceRoots[j + 1] - divergenceRoots[j] < 0.01)
                {
                    divergenceRoots[j] = 0;
                    countToDelete++;
                }
            }

            divergenceRoots.Sort();

            while (countToDelete > 0)
            {
                divergenceRoots.RemoveAt(0);
                countToDelete--;
            }

            tempDualGraph[i] = new List<DualVertex>();

            for (int j = 0; j < divergenceRoots.Count - 1; j++) tempDualGraph[i].Add(new DualVertex(divergenceRoots[j], divergenceRoots[j + 1], _segments[i]));
        }

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].ParentLane.Direction)
                for (int j = 0; j < tempDualGraph[i].Count - 1; j++)
                {
                    tempDualGraph[i][j].Predecessors.Add(tempDualGraph[i][j+1]);
                    tempDualGraph[i][j+1].Followers.Add(tempDualGraph[i][j]);
                }
            else
                for (int j = 0; j < tempDualGraph[i].Count - 1; j++)
                {
                    tempDualGraph[i][j].Followers.Add(tempDualGraph[i][j + 1]);
                    tempDualGraph[i][j + 1].Predecessors.Add(tempDualGraph[i][j]);
                }
        }
        for (int i = 0; i < _segments.Count; i++)
        {
            //Debug.Log("*** From " + _segments[i].Id);

            foreach (var c in _segments[i].Contacts)
            {
                //Debug.Log(_segments[c.Target.SelfIndex].Id);

                var originVertice = FindOriginVertice(tempDualGraph[i], i, c.OriginRoot);
                var targetVertice = FindTargetVertice(tempDualGraph[c.Target.SelfIndex], c.Target.SelfIndex, c.TargetRoot);

                if (originVertice != null && targetVertice != null)
                {
                    originVertice.Followers.Add(targetVertice);
                    targetVertice.Predecessors.Add(originVertice);
                }
            }
        }
        foreach (var collection in tempDualGraph)
        {
            foreach (var dv in collection) dv.Distance = RoadUtilities.GetArcLengthBetween(dv.ParentSegmentPart.Item1.ArcLength, dv.ParentSegmentPart.Item2, dv.ParentSegmentPart.Item3);
            DualGraph.AddRange(collection);
        }
    }

    private void PrintDualGraph()
    {
        for (int i = 0; i < DualGraph.Count; i++)
        {
            string s = "DV " +
                       i +
                       " " +
                       DualGraph[i].ParentSegmentPart.Item1.Id +
                       " " +
                       DualGraph[i].ParentSegmentPart.Item2 +
                       " " +
                       DualGraph[i].ParentSegmentPart.Item3 + " " + DualGraph[i].Distance + "\nFl : ";

            foreach (var fol in DualGraph[i].Followers) s   += DualGraph.IndexOf(fol) + " ";
            s                                               += "\nPr : ";
            foreach (var pr in DualGraph[i].Predecessors) s += DualGraph.IndexOf(pr) + " ";
            Debug.Log(s);
        }
    }

    private void LinkDualGraph()
    {
        foreach (var dv in DualGraph) dv.ParentSegmentPart.Item1.DualVertices.Add(dv);
    }

    private DualVertex FindOriginVertice(List<DualVertex> list, int i, double originRoot)
    {
        foreach (var tdv in list)
        {
            if (_segments[i].ParentLane.Direction && Math.Abs(originRoot - tdv.ParentSegmentPart.Item2) < 0.011 ||
                !_segments[i].ParentLane.Direction && Math.Abs(originRoot - tdv.ParentSegmentPart.Item3) < 0.011)
                return tdv;
        }
        //Debug.LogError("Origin Vertice Not Found");
        return null;
    }

    private DualVertex FindTargetVertice(List<DualVertex> list, int i, double targetRoot)
    {
        foreach (var tdv in list)
        {


            if (_segments[i].ParentLane.Direction && Math.Abs(targetRoot - tdv.ParentSegmentPart.Item3) < 0.011 ||
                !_segments[i].ParentLane.Direction && Math.Abs(targetRoot - tdv.ParentSegmentPart.Item2) < 0.011)
                return tdv;
        }
        //Debug.LogError("Target Vertice Not Found");
        return null;
    }

    private void LoadRoadNode(XmlNode n)
    {
        if (n.Attributes != null)
        {
            _roads.Add(
                      new Road
                      {
                          Id = long.Parse(n.Attributes["idr"].Value, CultureInfo.InvariantCulture),
                          ChildLanes = new Lane[n.ChildNodes.Count]
                      });


            for (int i = 0; i < n.ChildNodes.Count; i++)
            {
                if (n.ChildNodes[i].Attributes != null)
                {
                    _lanes.Add(
                              new Lane()
                              {
                                  Id = long.Parse(n.ChildNodes[i].Attributes["idl"].Value, CultureInfo.InvariantCulture),
                                  ParentRoad = _roads[_roads.Count - 1],
                                  ChildSegments = new Segment[n.ChildNodes[i].ChildNodes.Count],

                                  Direction = bool.Parse(n.ChildNodes[i].Attributes["reverse"].Value),
                                  SpeedLimit = double.Parse(n.ChildNodes[i].Attributes["splimit"].Value),
                                  MyType = (LaneType)int.Parse(n.ChildNodes[i].Attributes["type"].Value),
                                  Width = double.Parse(n.ChildNodes[i].Attributes["width"].Value, CultureInfo.InvariantCulture)
                              });
                    _roads[_roads.Count - 1].ChildLanes[i] = _lanes[_lanes.Count - 1];

                    for (int j = 0; j < n.ChildNodes[i].ChildNodes.Count; j++)
                    {
                        if (n.ChildNodes[i].ChildNodes[j].Attributes != null)
                        {
                            _segments.Add(
                                         new Segment()
                                         {
                                             Id = long.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["id"].Value, CultureInfo.InvariantCulture),
                                             ParentLane = _lanes[_lanes.Count - 1],
                                             Polynomial = new[,]
                                             {
                                                 {
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["ax"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["ay"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["az"].Value, CultureInfo.InvariantCulture)
                                                 },
                                                 {
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["bx"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["by"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["bz"].Value, CultureInfo.InvariantCulture)
                                                 },
                                                 {
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["cx"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["cy"].Value, CultureInfo.InvariantCulture),
                                                     double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["cz"].Value, CultureInfo.InvariantCulture)
                                                 }
                                             },
                                             ArcLength = new[]
                                             {
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["al"].Value, CultureInfo.InvariantCulture),
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["bl"].Value, CultureInfo.InvariantCulture),
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["cl"].Value, CultureInfo.InvariantCulture)
                                             },
                                             InvArcLength = new[]
                                             {
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["la"].Value, CultureInfo.InvariantCulture),
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["lb"].Value, CultureInfo.InvariantCulture),
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["lc"].Value, CultureInfo.InvariantCulture)
                                             }
                                         });
                            _lanes[_lanes.Count - 1].ChildSegments[j] = _segments[_segments.Count - 1];
                        }
                    }
                }
            }
            //Debug.Log("Loaded Road " + Roads[Roads.Count - 1].Id + " " + Roads[Roads.Count - 1].ChildLanes.Length + " " + Segments.Count);
        }
    }

    private Segment CreateContactSegment(LaneType tp, double sl, double wd, Vector3 start, Vector3 end)
    {
        var myPoly = new[,]
        {
            {
                0.0,
                0.0,
                0.0
            },
            {
                end.x - start.x,
                end.y - start.y,
                end.z - start.z
            },
            {
                start.x,
                start.y,
                start.z
            }
        };

        var arcL = Math.Sqrt(Math.Pow(myPoly[1, 0] + myPoly[2, 0], 2) + Math.Pow(myPoly[1, 1] + myPoly[2, 1], 2) + Math.Pow(myPoly[1, 2] + myPoly[2, 2], 2));


        return new Segment()
        {
            Id         = 0,
            ParentLane = new Lane()
            {
                Id            = 0,
                ParentRoad    = null,
                ChildSegments = new Segment[1],
                Direction     = false,
                MyType        = tp,
                SpeedLimit    = sl,
                Width         = wd
            },
            Polynomial = myPoly,
            ArcLength  = new[]
            {
                0.0,
                0.0,
                arcL
            },
            InvArcLength = new[]
            {
                0.0,
                0.0,
                1 / arcL
            }
        };
    }

    private void LoadCoreContacts()
    {
        foreach (var road in _roads)
        {
            for(int i = 0; i < road.ChildLanes.Length; i++)
            {
                if (road.ChildLanes[i].MyType == LaneType.Vehicle)
                {
                    if (i < road.ChildLanes.Length - 1 && road.ChildLanes[i + 1].MyType == LaneType.Vehicle)
                        for (int j = 0; j < road.ChildLanes[i].ChildSegments.Length; j++)
                        {
                            var lc1 = new LaneContact(road.ChildLanes[i].ChildSegments[j], road.ChildLanes[i + 1].ChildSegments[j]);
                            road.ChildLanes[i].ChildSegments[j].LaneContacts.Add(lc1);

                            var lc2 = new LaneContact(road.ChildLanes[i + 1].ChildSegments[j], road.ChildLanes[i].ChildSegments[j]);
                            road.ChildLanes[i + 1].ChildSegments[j].LaneContacts.Add(lc2);
                        }

                    for (int j = 0; j < road.ChildLanes[i].ChildSegments.Length - 1; j++)
                    {
                        if (road.ChildLanes[i].Direction)
                        {
                            var pc = new PointContact(0, 1, road.ChildLanes[i].ChildSegments[j + 1], road.ChildLanes[i].ChildSegments[j]);
                            road.ChildLanes[i].ChildSegments[j + 1].Contacts.Add(pc);
                            road.ChildLanes[i].ChildSegments[j].IncomingContacts.Add(pc);
                        }
                        else
                        {
                            var pc = new PointContact(1, 0, road.ChildLanes[i].ChildSegments[j], road.ChildLanes[i].ChildSegments[j + 1]);
                            road.ChildLanes[i].ChildSegments[j + 1].IncomingContacts.Add(pc);
                            road.ChildLanes[i].ChildSegments[j].Contacts.Add(pc);
                        }
                    }
                }
            }
        }
    }

    private void LoadContactNode(XmlNode node)
    {
            if (node.Attributes != null)
            {
                var originRoad = FindRoadById(long.Parse(node.Attributes["srm"].Value, CultureInfo.InvariantCulture));
                var targetRoad = FindRoadById(long.Parse(node.Attributes["erm"].Value, CultureInfo.InvariantCulture));

                var startState = (ContactStart)int.Parse(node.Attributes["ss"].Value, CultureInfo.InvariantCulture);
                var endState   = (ContactEnd)int.Parse(node.Attributes["es"].Value, CultureInfo.InvariantCulture);

                if (targetRoad != null && originRoad != null)
                    switch (endState)
                    {
                        case ContactEnd.Start:

                            if (originRoad.ChildLanes.Length == targetRoad.ChildLanes.Length)
                                switch (startState)
                                {
                                    case ContactStart.Start:
                                        LoadStartStartContact(originRoad, targetRoad);

                                        break;
                                    case ContactStart.End:
                                        LoadEndStartContact(originRoad, targetRoad);

                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            else
                                Debug.LogError("*-to-start contact between roads of different lane dimensions");

                            break;

                        case ContactEnd.End:

                            if (originRoad.ChildLanes.Length == targetRoad.ChildLanes.Length)
                                switch (startState)
                                {
                                    case ContactStart.Start:
                                        LoadStartEndContact(originRoad, targetRoad);

                                        break;
                                    case ContactStart.End:
                                        LoadEndEndContact(originRoad, targetRoad);

                                        break;
                                    default:

                                        throw new NotImplementedException();
                                }
                            else
                                Debug.LogError("*-to-end contact between roads of different lane dimensions");

                            break;

                        case ContactEnd.Side:

                            switch (startState)
                            {
                                case ContactStart.Start:
                                    LoadStartSideContact(originRoad, targetRoad);
                                    break;
                                case ContactStart.End:
                                    LoadEndSideContact(originRoad, targetRoad);
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            break;
                    }
                else
                    Debug.LogError("Bad road index in contact XML");
            }
            else
            {
                throw new NullReferenceException();
            }
    }

    private void MakeIntermediateContact(Vector3 origin, Vector3 target, Lane parentLane, PointContact cnt)
    {
        var cS = CreateContactSegment(
                                      parentLane.MyType,
                                      parentLane.SpeedLimit,
                                      parentLane.Width,
                                      origin,
                                      target);
        _segments.Add(cS);

        var c1 = new PointContact(
                                  cnt.OriginRoot,
                                  0,
                                  cnt.Origin,
                                  cS);

        cnt.Origin.Contacts.Add(c1);
        cS.IncomingContacts.Add(c1);

        var c2 = new PointContact(
                                  1,
                                  cnt.TargetRoot,
                                  cS,
                                  cnt.Target);

        cS.Contacts.Add(c2);
        cnt.Target.IncomingContacts.Add(c2);
    }

    private void LoadStartStartContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
            {
                var originPoint = RoadUtilities.Calculate(originRoad.ChildLanes[i].ChildSegments[0].Polynomial, 0);
                var targetPoint = RoadUtilities.Calculate(targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0].Polynomial, 0);

                if (originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             0,
                                             originRoad.ChildLanes[i].ChildSegments[0],
                                             targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        originRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                        targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i], c);
                    }
                }
                else if (!originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             0,
                                             targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0],
                                             originRoad.ChildLanes[i].ChildSegments[0]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0].Contacts.Add(c);
                        originRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c);
                    }
                }
            }
        }
    }

    private void LoadEndStartContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
            {
                var originPoint = RoadUtilities.Calculate(originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Polynomial, 1);
                var targetPoint = RoadUtilities.Calculate(targetRoad.ChildLanes[i].ChildSegments[0].Polynomial, 0);

                if (originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             1,
                                             targetRoad.ChildLanes[i].ChildSegments[0],
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        targetRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                        originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i],c);
                    }
                }
                else if (!originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             1,
                                             0,
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             targetRoad.ChildLanes[i].ChildSegments[0]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);
                        targetRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[i], c);
                    }
                }
            }
        }
    }

    private void LoadStartEndContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
            {
                var originPoint = RoadUtilities.Calculate(originRoad.ChildLanes[i].ChildSegments[0].Polynomial, 0);
                var targetPoint = RoadUtilities.Calculate(targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1].Polynomial, 1);

                if (originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             1,
                                             originRoad.ChildLanes[i].ChildSegments[0],
                                             targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        originRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                    targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[i], c);
                    }
                }
                else if (!originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             1,
                                             0,
                                             targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             originRoad.ChildLanes[i].ChildSegments[0]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);
                    originRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c);
                    }
                }
            }
        }
    }

    private void LoadEndEndContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
            {
                var originPoint = RoadUtilities.Calculate(originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Polynomial, 1);
                var targetPoint = RoadUtilities.Calculate(targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                                                    .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1].ChildSegments.Length - 1 - i].Polynomial, 1);

                if (originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].Direction)
                {
                    var c = new PointContact(
                                             1,
                                             1,
                                             targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                                       .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1].ChildSegments.Length - 1 - i],
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                  .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1].ChildSegments.Length - 1 - i]
                                  .Contacts.Add(c);
                        originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                    }
                else
                {
                    MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c);
                }
            }
                else if (!originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             1,
                                             1,
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                                       .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].ChildSegments.Length - 1]);

                    if ((originPoint - targetPoint).magnitude < 0.5f)
                    {
                        originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);

                    targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                              .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].ChildSegments.Length - 1]
                              .Contacts.Add(c);
                    }
                    else
                    {
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i], c);
                    }
                }
            }
        }
    }

    private void LoadEndSideContact(Road originRoad, Road targetRoad)
    {
        foreach (var targetLane in targetRoad.ChildLanes)
        {
            if (targetLane.MyType == LaneType.Vehicle)
                foreach (var originLane in originRoad.ChildLanes)
                {
                    if (originLane.MyType == LaneType.Vehicle)
                    {
                        var originPoint = RoadUtilities.Calculate(originLane.ChildSegments[originLane.ChildSegments.Length - 1].Polynomial, 1);
                        var targetSegmentRoot    = FindClosestSegmentInLane(targetLane, originPoint);

                        if (targetSegmentRoot != null && targetSegmentRoot.Item1 != null)
                            if (originLane.Direction)
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(targetSegmentRoot.Item1.ArcLength, targetSegmentRoot.Item1.InvArcLength, targetSegmentRoot.Item2, targetLane.Direction ? 4 : -4);

                                var cS = CreateContactSegment(targetLane.MyType, targetLane.SpeedLimit, targetLane.Width,
                                                              RoadUtilities.Calculate(targetSegmentRoot.Item1.Polynomial, finalTargetRoot.Item2), originPoint);
                                _segments.Add(cS);

                                var c1         = new PointContact(finalTargetRoot.Item2, 0,
                                                                  targetSegmentRoot.Item1, cS);
                                targetSegmentRoot.Item1.Contacts.Add(c1);
                                cS.IncomingContacts.Add(c1);

                                var c2 = new PointContact(1, 1,
                                                          cS, originLane.ChildSegments[originLane.ChildSegments.Length - 1]);
                                cS.Contacts.Add(c2);
                                originLane.ChildSegments[originLane.ChildSegments.Length - 1].IncomingContacts.Add(c2);
                            }
                            else
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(targetSegmentRoot.Item1.ArcLength, targetSegmentRoot.Item1.InvArcLength, targetSegmentRoot.Item2, targetLane.Direction ? -4 : 4);

                                var cS = CreateContactSegment(targetLane.MyType, targetLane.SpeedLimit, targetLane.Width,
                                                              originPoint, RoadUtilities.Calculate(targetSegmentRoot.Item1.Polynomial, finalTargetRoot.Item2));
                                _segments.Add(cS);

                                var c1         = new PointContact(1, 0, originLane.ChildSegments[originLane.ChildSegments.Length - 1], cS);
                                originLane.ChildSegments[originLane.ChildSegments.Length - 1].Contacts.Add(c1);
                                cS.IncomingContacts.Add(c1);

                                var c2 = new PointContact(1, finalTargetRoot.Item2, cS, targetSegmentRoot.Item1);
                                cS.Contacts.Add(c2);
                                targetSegmentRoot.Item1.IncomingContacts.Add(c2);
                            }
                    }
                }
        }
    }

    private void LoadStartSideContact(Road originRoad, Road targetRoad)
    {
        foreach (var targetLane in targetRoad.ChildLanes)
        {
            if (targetLane.MyType == LaneType.Vehicle)
                foreach (var originLane in originRoad.ChildLanes)
                {
                    if (originLane.MyType == LaneType.Vehicle)
                    {
                        var originPoint = RoadUtilities.Calculate(originLane.ChildSegments[0].Polynomial, 0);
                        var targetSegmentRoot = FindClosestSegmentInLane(targetLane, originPoint);

                        if (targetSegmentRoot != null && targetSegmentRoot.Item1 != null)
                            if (!originLane.Direction)
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(targetSegmentRoot.Item1.ArcLength, targetSegmentRoot.Item1.InvArcLength, targetSegmentRoot.Item2, targetLane.Direction ? 4 : -4);

                                var cS = CreateContactSegment(targetLane.MyType, targetLane.SpeedLimit, targetLane.Width,
                                                              RoadUtilities.Calculate(targetSegmentRoot.Item1.Polynomial, finalTargetRoot.Item2), originPoint);
                                _segments.Add(cS);

                                var c1 = new PointContact(finalTargetRoot.Item2, 0, targetSegmentRoot.Item1, cS);
                                targetSegmentRoot.Item1.Contacts.Add(c1);
                                cS.IncomingContacts.Add(c1);

                                var c2 = new PointContact(1, 0, cS, originLane.ChildSegments[0]);
                                cS.Contacts.Add(c2);
                                originLane.ChildSegments[0].IncomingContacts.Add(c2);
                            }
                            else
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(targetSegmentRoot.Item1.ArcLength, targetSegmentRoot.Item1.InvArcLength, targetSegmentRoot.Item2, targetLane.Direction ? -4 : 4);

                                var cS = CreateContactSegment(targetLane.MyType, targetLane.SpeedLimit, targetLane.Width,
                                                              originPoint, RoadUtilities.Calculate(targetSegmentRoot.Item1.Polynomial, finalTargetRoot.Item2));
                                _segments.Add(cS);

                                var c1         = new PointContact(0, 0, originLane.ChildSegments[0], cS);
                                originLane.ChildSegments[0].Contacts.Add(c1);
                                cS.IncomingContacts.Add(c1);

                                var c2 = new PointContact(1, finalTargetRoot.Item2, cS, targetSegmentRoot.Item1);
                                cS.Contacts.Add(c2);
                                targetSegmentRoot.Item1.IncomingContacts.Add(c2);
                            }
                    }
                }
        }
    }

    private void LoadStopNode(XmlNode node)
    {
        if (node.Attributes != null)
        {
            var road = FindRoadById(long.Parse(node.Attributes["rm"].Value, CultureInfo.InvariantCulture));
            var x    = float.Parse(node.Attributes["x"].Value, CultureInfo.InvariantCulture);
            var y    = float.Parse(node.Attributes["y"].Value, CultureInfo.InvariantCulture);
            var z    = float.Parse(node.Attributes["z"].Value, CultureInfo.InvariantCulture);


            var anchor = new Vector3(x, y, z);
            var res = FindClosestSegmentInRoad(road, anchor, LaneType.Vehicle);

            if (res != null && res.Item1 != null)
            {
                var stp = new StoppingPoint()
                {
                    OriginId      = road.Id,
                    ParentSegment = res.Item1,
                    Root          = res.Item2,
                    Type          = (StoppingType)int.Parse(node.Attributes["type"].Value, CultureInfo.InvariantCulture)
                };

                switch (stp.Type)
                {
                    case StoppingType.StopSign:
                        stp.Sm = new StopSignSm();
                        break;

                    case StoppingType.TrafficLight:

                        stp.Sm = new TrafficLightsSm(
                                                     float.Parse(node.Attributes["start"].Value, CultureInfo.InvariantCulture),
                                                     float.Parse(node.Attributes["cycle"].Value, CultureInfo.InvariantCulture));
                        break;

                    case StoppingType.Vehicle:
                        break;
                }
                stp.ParentSegment.Stops.Add(stp);
            }

        }
        else
        {
            throw new NullReferenceException();
        }
    }

    private void LoadParkingNode(XmlNode node)
    {
        if (node.Attributes != null)
        {
            //Debug.Log("new park");
            var x = float.Parse(node.Attributes["x"].Value, CultureInfo.InvariantCulture);
            var y = float.Parse(node.Attributes["y"].Value, CultureInfo.InvariantCulture);
            var z = float.Parse(node.Attributes["z"].Value, CultureInfo.InvariantCulture);
            var xd = float.Parse(node.Attributes["xd"].Value, CultureInfo.InvariantCulture);
            var yd = float.Parse(node.Attributes["yd"].Value, CultureInfo.InvariantCulture);
            var zd = float.Parse(node.Attributes["zd"].Value, CultureInfo.InvariantCulture);

            var road = FindRoadById(long.Parse(node.Attributes["rm"].Value, CultureInfo.InvariantCulture));

            var anchor = new Vector3(x,y,z);
            var res = FindClosestSegmentInRoad(road, anchor, LaneType.Vehicle);

            if (res != null && res.Item1 != null)
            {
                var ps = new ParkingSpace()
                {
                    ParentSegment = res.Item1,
                    Root          = res.Item2,
                    Position      = new Vector3(x, y, z),
                    Direction     = new Vector3(xd, yd, zd),
                    MyType        = (ParkingType)int.Parse(node.Attributes["type"].Value, CultureInfo.InvariantCulture)
                };


                ps.ParentSegment.ParkingSpaces.Add(ps);
                _parkingSpaces.Add(ps);
            }
        }
        else
        {
            throw new NullReferenceException();
        }
    }

    private Road FindRoadById(long id)
    {
        foreach (var road in _roads) if (road.Id == id) return road;
        return null;
    }

    public Tuple<Segment,double> FindClosestSegmentInRoad(Road road, Vector3 pos, LaneType type)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        foreach (var lane in road.ChildLanes)
        {
            if (lane.MyType == type)
                foreach (var segment in lane.ChildSegments)
                {
                    var possibleRoot = RoadUtilities.GetClosestRoot(segment.Polynomial, pos);

                    if (possibleRoot.Item2 < minimalDistance)
                    {
                        minimalDistance = possibleRoot.Item2;
                        closestSegment  = segment;
                        closestRoot     = possibleRoot.Item1;
                    }
                }
        }

        return new Tuple<Segment, double>(closestSegment,closestRoot);
    }

    public Tuple<Segment, double> FindClosestSegmentInLane(Lane lane, Vector3 pos)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        foreach (var segment in lane.ChildSegments)
        {
            var possibleRoot = RoadUtilities.GetClosestRoot(segment.Polynomial, pos);

            if (possibleRoot.Item2 < minimalDistance)
            {
                minimalDistance = possibleRoot.Item2;
                closestSegment  = segment;
                closestRoot     = possibleRoot.Item1;
            }
        }

        return new Tuple<Segment, double>(closestSegment, closestRoot);
    }

    public Tuple<Segment, double> FindClosestSegment(Vector3 pos, LaneType type)
    {
        if (_segments != null)
        {
            Segment closestSegment  = null;
            double  closestRoot     = double.NaN;
            double  minimalDistance = double.MaxValue;

            foreach (var segment in _segments)
            {
                if (segment.ParentLane.MyType == type)
                {
                    var possibleRoot = RoadUtilities.GetClosestRoot(segment.Polynomial, pos);

                    if (possibleRoot.Item2 < minimalDistance)
                    {
                        minimalDistance = possibleRoot.Item2;
                        closestSegment  = segment;
                        closestRoot     = possibleRoot.Item1;
                    }
                }
            }

            return new Tuple<Segment, double>(closestSegment, closestRoot);
        }

        return null;
    }

    public Tuple<Segment, double> FindClosestSegmentWithinLaneWidth(Vector3 pos, LaneType type)
    {
        if (_segments != null)
        {
            Segment closestSegment  = null;
            double  closestRoot     = double.NaN;
            double  minimalDistance = double.MaxValue;

            foreach (var segment in _segments)
            {
                if (segment.ParentLane.MyType == type)
                {
                    var possibleRoot = RoadUtilities.GetClosestRoot(segment.Polynomial, pos);

                    if (possibleRoot.Item2 < minimalDistance && possibleRoot.Item2 < segment.ParentLane.Width / 2)
                    {
                        minimalDistance = possibleRoot.Item2;
                        closestSegment  = segment;
                        closestRoot     = possibleRoot.Item1;
                    }
                }
            }

            return new Tuple<Segment, double>(closestSegment, closestRoot);
        }

        return null;
    }

    public Tuple<Segment, double> FindRandomSegmentAndRoot()
    {
        return new Tuple<Segment, double>(_segments[(int)(UnityEngine.Random.value * _segments.Count)],UnityEngine.Random.value);
    }

    internal Tuple<Segment, double> FindClosestAdjacentSegment(Vector3 pos, Segment originSegment)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        var selfRoot = RoadUtilities.GetClosestRoot(originSegment.Polynomial, pos);
        if(selfRoot.Item1 > 0 && selfRoot.Item1 < 1)

        foreach (var contact in originSegment.Contacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact.Target.Polynomial, pos);

            if (possibleRoot.Item2 < minimalDistance)
            {
                minimalDistance = possibleRoot.Item2;
                closestSegment  = contact.Target;
                closestRoot     = possibleRoot.Item1;
            }
        }

        foreach (var contact in originSegment.IncomingContacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact.Origin.Polynomial, pos);

            if (possibleRoot.Item2 < minimalDistance)
            {
                minimalDistance = possibleRoot.Item2;
                closestSegment  = contact.Target;
                closestRoot     = possibleRoot.Item1;
            }
        }

        return new Tuple<Segment, double>(closestSegment, closestRoot);
    }

    private void OnDrawGizmos()
    {
        if(_showRoadGizmos) DrawRoadGizmos();
        if (_showRequestedGizmos) DrawRequestedGizmos();
        if (_showDualGizmos) DrawDualGizmos();
    }

    private void DrawRoadGizmos()
    {
        if (_segments != null && _segments.Count != 0)
            foreach (var s in _segments)
            {
                Gizmos.color = s.ParentLane.MyType == LaneType.Vehicle ? Color.white: Color.cyan;
                var st       = RoadUtilities.Calculate(s.Polynomial, 0);
                var en       = RoadUtilities.Calculate(s.Polynomial, 1);
                Gizmos.DrawLine(st, en);

                foreach (var c in s.Contacts)
                {
                        Gizmos.color = Color.red;
                        var v1       = RoadUtilities.Calculate(c.Origin.Polynomial, c.OriginRoot);
                        var v2       = RoadUtilities.Calculate(c.Target.Polynomial, c.TargetRoot);
                        Gizmos.DrawLine(v1, v2);
                }

                foreach (var c in s.LaneContacts)
                {
                    Gizmos.color = Color.blue;
                    var v1       = RoadUtilities.Calculate(c.Origin.Polynomial, 0.5);
                    var v2       = RoadUtilities.Calculate(c.Target.Polynomial, 0.5);
                    Gizmos.DrawSphere(v1, 0.4f);
                    Gizmos.DrawSphere(v2, 0.4f);
                    Gizmos.DrawLine(v1, v2);
                }

                foreach (var stp in s.Stops)
                {
                    Gizmos.color = stp.Sm.GetState() ? Color.red : Color.green;
                    Gizmos.DrawSphere(RoadUtilities.Calculate(stp.ParentSegment.Polynomial, stp.Root), 0.6f);
                }
                /*
                Gizmos.color = Color.yellow;
                for (int i = 0; i < 11; i++)
                {
                    double posRoot = i / 10.0;
                    Gizmos.DrawSphere(RoadUtilities.Calculate(s.Polynomial, posRoot), 0.2f);
                }
                */
            }
    }

    private void DrawDualGizmos()
    {
        Gizmos.color = Color.white;

        if (_segments != null && _segments.Count != 0 && DualGraph.Count != 0)
            foreach (var dv in DualGraph)
            {
                var st = RoadUtilities.Calculate(dv.ParentSegmentPart.Item1.Polynomial, dv.ParentSegmentPart.Item2);
                var en = RoadUtilities.Calculate(dv.ParentSegmentPart.Item1.Polynomial, dv.ParentSegmentPart.Item3);
                Gizmos.DrawLine(st, en);
            }
    }

    private void DrawRequestedGizmos()
    {
        foreach (var t in GizmosRequests)
        {
            Gizmos.color = t.Item3;
            foreach (var v in t.Item1) Gizmos.DrawSphere(v, 0.5f);
        }
        GizmosRequests.Clear();
    }

    #region Variables

    [SerializeField] internal bool _showRoadGizmos;
    [SerializeField] internal bool _showRequestedGizmos;
    [SerializeField] internal bool _showDualGizmos;

    internal List<ParkingSpace> _parkingSpaces;
    internal List<Road> _roads;
    internal List<Lane> _lanes;
    internal List<Segment> _segments;
    internal List<PiavRoadAgent> _cars;

    [HideInInspector] public List<DualVertex> DualGraph = new List<DualVertex>();
    [HideInInspector] public List<Tuple<List<Vector3>, Type, Color>> GizmosRequests = new List<Tuple<List<Vector3>, Type, Color>>();

    #endregion
}

public class DualVertex
{
    internal DualVertex(double startRt, double endRt, Segment parent)
    {
        ParentSegmentPart = new Tuple<Segment, double, double>(parent,startRt,endRt);
    }

    public List<DualVertex> Predecessors = new List<DualVertex>();
    public List<DualVertex> Followers    = new List<DualVertex>();

    public double                               Distance;
    public Tuple<Segment, double, double> ParentSegmentPart;

    public double AccumulatedDistance;
    public DualVertex PathPredecessor;

    #region Overrides of Object

    /// <inheritdoc />
    public override string ToString()
    {
        return "Dual Vertice " + ParentSegmentPart.Item1.Id + " (" + ParentSegmentPart.Item2 + " - " + ParentSegmentPart.Item3 + ")";
    }

    #endregion
}

internal enum ContactStart
{
    Start, End
}

internal enum ContactEnd
{
    Start, End, Side
}
