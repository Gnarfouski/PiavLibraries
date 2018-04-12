using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class PiavRoadContainer : MonoBehaviour
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

        foreach (var agent in (PiavRoadAgent[])FindObjectsOfType(typeof(PiavRoadAgent))) _cars.Add(agent);
    }

    private void Update()
    {
        foreach (var car in _cars)
        {
            var positionDelta = car.transform.position - car.CurrentPosition;
            car.CurrentAbsoluteSpeed = positionDelta.magnitude / Time.deltaTime;
            car.CurrentMovingDirection = positionDelta.normalized;

            /*
            var closestSegment = car.CurrentSegment == null ?
                                     FindClosestSegment(car.CurrentPosition, LaneType.Vehicle) :
                                     FindClosestAdjacentSegment(car.CurrentPosition, car.CurrentSegment);
                                     */
            var closestSegment = FindClosestSegment(car.CurrentPosition, LaneType.Vehicle);
           car.CurrentSegment = closestSegment.Item1;
            car.CurrentRoot = closestSegment.Item2;

            car.CurrentFacingDirection = car.transform.forward;

            car.CurrentDirectionalSpeed = car.CurrentSegment != null ?
                                               car.CurrentAbsoluteSpeed *
                                               Vector3.Dot(car.CurrentFacingDirection, RoadUtilities.CalculateFirstDerivative(car.CurrentSegment.Polynomial, car.CurrentRoot).normalized) *
                                               (car.CurrentSegment.ParentLane.Direction ? -1 : 1) :
                                               car.CurrentAbsoluteSpeed;
            car.CurrentPosition = car.transform.position;
        }
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
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["bl"].Value, CultureInfo.InvariantCulture)
                                             },
                                             InvArcLength = new[]
                                             {
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["la"].Value, CultureInfo.InvariantCulture),
                                                 double.Parse(n.ChildNodes[i].ChildNodes[j].Attributes["lb"].Value, CultureInfo.InvariantCulture)
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
                            road.ChildLanes[i].ChildSegments[j].Contacts.Add(lc1);
                            road.ChildLanes[i + 1].ChildSegments[j].IncomingContacts.Add(lc1);

                            var lc2 = new LaneContact(road.ChildLanes[i + 1].ChildSegments[j], road.ChildLanes[i].ChildSegments[j]);
                            road.ChildLanes[i].ChildSegments[j].IncomingContacts.Add(lc2);
                            road.ChildLanes[i + 1].ChildSegments[j].Contacts.Add(lc2);
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
                                Debug.LogError("End-to-end contact between roads of different lane dimensions");

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
                                Debug.LogError("End-to-end contact between roads of different lane dimensions");

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

    private void LoadStartStartContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
                if (originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             0,
                                             originRoad.ChildLanes[i].ChildSegments[0],
                                             targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0]);
                    originRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                    targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0].IncomingContacts.Add(c);
                }
                else if (!originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             0,
                                             targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0],
                                             originRoad.ChildLanes[i].ChildSegments[0]);
                    targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i].ChildSegments[0].Contacts.Add(c);
                    originRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                }
        }
    }

    private void LoadEndStartContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
                if (originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             0,
                                             1,
                                             targetRoad.ChildLanes[i].ChildSegments[0],
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1]);
                    targetRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                    originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                }
                else if (!originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(
                                             1,
                                             0,
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             targetRoad.ChildLanes[i].ChildSegments[0]);
                    originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);
                    targetRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                }
        }
    }

    private void LoadStartEndContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
                if (originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(0,1,
                                             originRoad.ChildLanes[i].ChildSegments[0],
                                             targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1]);
                    originRoad.ChildLanes[i].ChildSegments[0].Contacts.Add(c);
                    targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                }
                else if (!originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(1,0,
                                             targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             originRoad.ChildLanes[i].ChildSegments[0]);
                    targetRoad.ChildLanes[i].ChildSegments[targetRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);
                    originRoad.ChildLanes[i].ChildSegments[0].IncomingContacts.Add(c);
                }
        }
    }

    private void LoadEndEndContact(Road originRoad, Road targetRoad)
    {
        for (int i = 0; i < originRoad.ChildLanes.Length; i++)
        {
            if (originRoad.ChildLanes[i].MyType == LaneType.Vehicle && targetRoad.ChildLanes[i].MyType == LaneType.Vehicle)
                if (originRoad.ChildLanes[i].Direction && !targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].Direction)
                {
                    var c = new PointContact(1, 1,
                                             targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                                       .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1].ChildSegments.Length - 1 - i],
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1]);
                    targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                              .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1].ChildSegments.Length - 1 - i].Contacts.Add(c);
                    originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].IncomingContacts.Add(c);
                }
                else if (!originRoad.ChildLanes[i].Direction && targetRoad.ChildLanes[i].Direction)
                {
                    var c = new PointContact(1,1,
                                             originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1],
                                             targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                                                       .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].ChildSegments.Length - 1]);
                    originRoad.ChildLanes[i].ChildSegments[originRoad.ChildLanes[i].ChildSegments.Length - 1].Contacts.Add(c);

                    targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i]
                              .ChildSegments[targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i].ChildSegments.Length - 1]
                              .Contacts.Add(c);
                }
        }
    }

    private void LoadEndSideContact(Road originRoad, Road targetRoad)
    {
        foreach (var tlane in targetRoad.ChildLanes)
        {
            if (tlane.MyType == LaneType.Vehicle)
                foreach (var olane in originRoad.ChildLanes)
                {
                    if (olane.MyType == LaneType.Vehicle)
                    {
                        var anchor1 = RoadUtilities.Calculate(olane.ChildSegments[olane.ChildSegments.Length - 1].Polynomial, 1);
                        var res1    = FindClosestSegmentInLane(tlane, anchor1);

                        if (res1 != null && res1.Item1 != null)
                            if (olane.Direction)
                            {
                                var finalRoot = RoadUtilities.GetProjection(res1.Item1.ArcLength, res1.Item1.InvArcLength, res1.Item2, tlane.Direction ? 4 : -4);
                                var c         = new PointContact(finalRoot.Item2, 1, res1.Item1, olane.ChildSegments[olane.ChildSegments.Length - 1]);
                                res1.Item1.Contacts.Add(c);
                                olane.ChildSegments[olane.ChildSegments.Length - 1].IncomingContacts.Add(c);
                            }
                            else
                            {
                                var finalRoot = RoadUtilities.GetProjection(res1.Item1.ArcLength, res1.Item1.InvArcLength, res1.Item2, tlane.Direction ? -4 : 4);
                                var c         = new PointContact(1, finalRoot.Item2, olane.ChildSegments[olane.ChildSegments.Length - 1], res1.Item1);
                                olane.ChildSegments[olane.ChildSegments.Length - 1].Contacts.Add(c);
                                res1.Item1.IncomingContacts.Add(c);
                            }
                    }
                }
        }
    }

    private void LoadStartSideContact(Road originRoad, Road targetRoad)
    {
        foreach (var tlane in targetRoad.ChildLanes)
        {
            if (tlane.MyType == LaneType.Vehicle)
                foreach (var olane in originRoad.ChildLanes)
                {
                    if (olane.MyType == LaneType.Vehicle)
                    {
                        var anchor1 = RoadUtilities.Calculate(olane.ChildSegments[0].Polynomial, 0);
                        var res1    = FindClosestSegmentInLane(tlane, anchor1);

                        if (res1 != null && res1.Item1 != null)
                            if (!olane.Direction)
                            {
                                var finalRoot = RoadUtilities.GetProjection(res1.Item1.ArcLength, res1.Item1.InvArcLength, res1.Item2, tlane.Direction ? 4 : -4);
                                var c         = new PointContact(finalRoot.Item2, 0, res1.Item1, olane.ChildSegments[0]);
                                res1.Item1.Contacts.Add(c);
                                olane.ChildSegments[0].IncomingContacts.Add(c);
                            }
                            else
                            {
                                var finalRoot = RoadUtilities.GetProjection(res1.Item1.ArcLength, res1.Item1.InvArcLength, res1.Item2, tlane.Direction ? -4 : 4);
                                var c         = new PointContact(0, finalRoot.Item2, olane.ChildSegments[0], res1.Item1);
                                olane.ChildSegments[0].Contacts.Add(c);
                                res1.Item1.IncomingContacts.Add(c);
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

    internal Tuple<Segment, double> FindClosestAdjacentSegment(Vector3 pos, Segment originSegment)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        var selfRoot = RoadUtilities.GetClosestRoot(originSegment.Polynomial, pos);
        if(selfRoot.Item1 > 0 && selfRoot.Item1 < 1)

        foreach (var contact in originSegment.Contacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact._target.Polynomial, pos);

            if (possibleRoot.Item2 < minimalDistance)
            {
                minimalDistance = possibleRoot.Item2;
                closestSegment  = contact._target;
                closestRoot     = possibleRoot.Item1;
            }
        }

        foreach (var contact in originSegment.IncomingContacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact._origin.Polynomial, pos);

            if (possibleRoot.Item2 < minimalDistance)
            {
                minimalDistance = possibleRoot.Item2;
                closestSegment  = contact._target;
                closestRoot     = possibleRoot.Item1;
            }
        }

        return new Tuple<Segment, double>(closestSegment, closestRoot);
    }

    private void OnDrawGizmos()
    {
        if (_segments != null && _segments.Count != 0)
            foreach (var s in _segments)
            {
                Gizmos.color = Color.white;

                foreach (var c in s.Contacts)
                {
                    //Debug.Log(c.GetType());

                    if (c.GetType() == typeof(PointContact))
                    {
                        Gizmos.color = Color.white;
                        var v1       = RoadUtilities.Calculate(c._origin.Polynomial, ((PointContact)c).OriginRoot);
                        var v2       = RoadUtilities.Calculate(c._target.Polynomial, ((PointContact)c).TargetRoot);
                        Gizmos.DrawSphere(v1, 0.4f);
                        Gizmos.DrawSphere(v2, 0.4f);
                        Gizmos.DrawLine(v1, v2);
                    }
                    else if (c.GetType() == typeof(LaneContact))
                    {
                        Gizmos.color = Color.blue;
                        var v1       = RoadUtilities.Calculate(c._origin.Polynomial, 0.5);
                        var v2       = RoadUtilities.Calculate(c._target.Polynomial, 0.5);
                        Gizmos.DrawSphere(v1, 0.4f);
                        Gizmos.DrawSphere(v2, 0.4f);
                        Gizmos.DrawLine(v1, v2);
                    }
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

    #region Variables

    internal List<ParkingSpace> _parkingSpaces;
    internal List<Road> _roads;
    internal List<Lane> _lanes;
    internal List<Segment> _segments;
    internal List<PiavRoadAgent> _cars;

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
