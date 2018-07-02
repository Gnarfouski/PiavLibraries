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

        if (File.Exists(Application.dataPath + "/StreamingAssets/" + SceneManager.GetActiveScene().name + "Info.xml"))
        {
            xmlDoc.Load(Application.dataPath + "/StreamingAssets/" + SceneManager.GetActiveScene().name + "Info.xml");

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

        foreach (var car in _cars)
        {
            car.CurrentPosition        = car.transform.position;
            car.CurrentFacingDirection = car.transform.forward;
            car.CurrentAbsoluteSpeed   = 0;
            var closestSegments                                 = FindAllSegmentsWithinLaneWidth(car.CurrentPosition, LaneType.Vehicle, car.CurrentFacingDirection);
            if (closestSegments.Count != 0) car.CurrentSegments = closestSegments;
            else car.CurrentSegments.Clear();

            //car.CurrentStops = FindAllStops(car, car.CurrentAbsoluteSpeed * 3,true);
        }
    }

    private void Update()
    {
            for (int i = 0; i < _cars.Count; i++)
            {
                if (_cars[i] == null)
                {
                    _cars.RemoveAt(i);
                    i--;
                }
            }

        foreach (var car in _cars)
        {
            var positionDelta          = car.transform.position - car.CurrentPosition;
            car.CurrentAbsoluteSpeed   = positionDelta.magnitude / Time.deltaTime;

            car.CurrentPosition        = car.transform.position;
            car.CurrentFacingDirection = car.transform.forward;

            var closestSegments = FindAllSegmentsWithinLaneWidth(car.CurrentPosition, LaneType.Vehicle, car.CurrentFacingDirection);
            if (closestSegments.Count != 0) car.CurrentSegments = closestSegments;
            else car.CurrentSegments.Clear();

            //car.CurrentStops = FindAllStops(car, car.CurrentAbsoluteSpeed * 3, true);
        }
    }

    private void LateUpdate()
    {
        foreach (var sg in _segments)
        {
            for(int i = 0; i < sg.Stops.Count; i++)
            {
                if (sg.Stops[i].Type == StoppingType.Vehicle)
                {
                    sg.Stops.RemoveAt(i);
                    i--;
                }
            }
        }

        foreach (var car in _cars)
        {
            foreach (var cs in car.CurrentSegments)
            {
                cs._segment.Stops.Add(new StoppingPoint
                {
                    Type = StoppingType.Vehicle,
                    Origin = car,
                    ParentSegment = cs._segment,
                    Root = cs._root,
                    Sm = new StopSignSm()
                });
            }
        }
    }

    [Serializable]
    internal struct StoppingDataPackage
    {
        internal StoppingPoint[] _points;
        internal double[] _distances;
    }

    [Serializable]
    internal struct Projection
    {
        internal double _remainingDistance;
        internal Segment _targetSegment;
        internal double _targetRoot;
    }

    private StoppingDataPackage FindAllStops(PiavRoadAgent parentAgent, double distance, bool withTraffic)
    {
        if (distance < 0) throw new Exception("Project with positive distances only");

        var resList = new StoppingDataPackage();
        var pathList = new StoppingDataPackage();
        var laneList = new StoppingDataPackage();

        var mas        = parentAgent.CurrentSegments[0];
        var projection = new Projection{ _remainingDistance = distance,  _targetSegment = mas._segment, _targetRoot = mas._root};

        if (parentAgent._desiredIncomingPath != null && parentAgent._desiredIncomingPath.Count > 0)
            foreach (var possibleSegment in parentAgent.CurrentSegments)
            {
                if (possibleSegment._segment.Id == parentAgent._desiredIncomingPath[0]._parentSegment.Id)
                {
                    pathList = GetStopsOnPath(parentAgent, distance, withTraffic, possibleSegment._root, out projection);
                    break;
                }
            }

        if (projection._remainingDistance > 0.1) laneList = GetStopsOnLane(projection._remainingDistance, withTraffic, projection._targetSegment, projection._targetRoot, out projection);

        var pathCount = pathList._points.Length;
        var laneCount = pathList._points.Length;
        var totalCount = pathCount + laneCount;

        resList._points = new StoppingPoint[totalCount];
        resList._distances = new double[totalCount];

        for (int i = 0; i < pathCount; i++)
        {
            resList._points[i] = pathList._points[i];
            resList._distances[i] = pathList._distances[i];
        }

        for (int i = pathCount; i < totalCount; i++)
        {
            resList._points[i]    = laneList._points[i - pathCount];
            resList._distances[i] = laneList._distances[i - pathCount];
        }

        return resList;
    }

    [Serializable]
    internal struct SpeedLimitation
    {
        internal double _value;
        internal Type   _originBehaviorType;
    }

    internal Projection ProjectPathLaneFromOrigin(PiavRoadAgent parentAgent, double distance, bool withTraffic)
    {
        if (distance < 0) throw new Exception("Project with positive distances only");

        var mas = parentAgent.CurrentSegments[0];
        var projection = new Projection{_remainingDistance = distance, _targetSegment = mas._segment, _targetRoot = mas._root};

        if (parentAgent._desiredIncomingPath != null && parentAgent._desiredIncomingPath.Count > 0)
            foreach (var possibleSegment in parentAgent.CurrentSegments)
            {
                if (possibleSegment._segment.Id == parentAgent._desiredIncomingPath[0]._parentSegment.Id)
                {
                    projection = ProjectOnPath(parentAgent, distance, withTraffic, possibleSegment._root);
                    break;
                }
            }

        if (projection._remainingDistance > 0.1) projection = ProjectOnLane(projection._remainingDistance, withTraffic, projection._targetSegment, projection._targetRoot);
        return projection;
    }

    private StoppingDataPackage GetStopsOnLane(double distance, bool withTraffic, Segment startSegment, double startRoot, out Projection endProjection)
    {
        if (distance < 0) throw new Exception("ProjectOnLane called with negative distance");

        var resList = new StoppingDataPackage();

        var currentRoot = startRoot;
        var lastProjection = new Projection{ _remainingDistance = distance * (startSegment.ParentLane.Direction && !withTraffic || !startSegment.ParentLane.Direction && withTraffic ? 1 : -1), _targetSegment = startSegment, _targetRoot = currentRoot};
        var limitCount = 100;

        var sps = new List<StoppingPoint>();
        var dist = new List<double>();

        while (limitCount > 0)
        {
            lastProjection = RoadUtilities.GetProjection(lastProjection._targetSegment, currentRoot, lastProjection._remainingDistance);

            resList._points = new StoppingPoint[lastProjection._targetSegment.Stops.Count];
            resList._distances = new double[lastProjection._targetSegment.Stops.Count];

            foreach (var t in lastProjection._targetSegment.Stops)
            {
                if (t.Root >= Math.Min(currentRoot, lastProjection._targetRoot) && t.Root <= Math.Max(currentRoot, lastProjection._targetRoot))
                {
                    var tempProjection = RoadUtilities.GetProjectionTo(
                                                                       lastProjection._targetSegment.ArcLength, lastProjection._targetSegment.InvArcLength, lastProjection._targetSegment, currentRoot, t.Root, lastProjection._remainingDistance);
                    sps.Add(t);
                    dist.Add(distance - tempProjection._remainingDistance);
                }
            }

            if (Math.Abs(lastProjection._remainingDistance) <= 0.001)
            {
                endProjection = new Projection{_remainingDistance = lastProjection._remainingDistance, _targetSegment = lastProjection._targetSegment, _targetRoot = lastProjection._targetRoot};
                resList._points = sps.ToArray();
                resList._distances = dist.ToArray();
                return resList;
            }

            Segment next = null;

            foreach (var contact in lastProjection._targetSegment.Contacts)
            {
                if (contact.Target.ParentLane.Id == lastProjection._targetSegment.ParentLane.Id)
                {
                    next = contact.Target;

                    break;
                }
            }

            if (next == null)
                foreach (var contact in lastProjection._targetSegment.Contacts)
                {
                    next = contact.Target;
                    break;
                }

            if (next != null)
            {
                lastProjection._targetSegment = next;
                currentRoot = lastProjection._targetSegment.ParentLane.Direction ? 1 : 0;
            }
            else
            {
                endProjection = new Projection{_remainingDistance = lastProjection._remainingDistance, _targetSegment = lastProjection._targetSegment, _targetRoot = lastProjection._targetRoot};
                resList._points    = sps.ToArray();
                resList._distances = dist.ToArray();
                return resList;
            }
            limitCount--;
        }

        endProjection = new Projection {_remainingDistance = lastProjection._remainingDistance, _targetSegment = lastProjection._targetSegment, _targetRoot = lastProjection._targetRoot };
        resList._points    = sps.ToArray();
        resList._distances = dist.ToArray();
        return resList;
    }

    private Projection ProjectOnLane(double distance, bool withTraffic, Segment startSegment, double startRoot)
    {
        if (distance < 0) throw new Exception("ProjectOnLane called with negative distance");

        var currentRoot = startRoot;
        var lastProjection = new Projection
            {_remainingDistance = distance * (startSegment.ParentLane.Direction && !withTraffic || !startSegment.ParentLane.Direction && withTraffic ? 1 : -1), _targetSegment = startSegment, _targetRoot = currentRoot};
        var limitCount = 100;

        while (limitCount > 0)
        {
            lastProjection = RoadUtilities.GetProjection(lastProjection._targetSegment, currentRoot, lastProjection._remainingDistance);


            if (Math.Abs(lastProjection._remainingDistance) <= 0.001) return lastProjection;

            Segment next = null;

            foreach (var contact in lastProjection._targetSegment.Contacts)
            {
                if (contact.Target.ParentLane.Id == lastProjection._targetSegment.ParentLane.Id)
                {
                    next = contact.Target;

                    break;
                }
            }

            if (next == null)
                foreach (var contact in lastProjection._targetSegment.Contacts)
                {
                    next = contact.Target;
                    break;
                }

            if (next != null)
            {
                lastProjection._targetSegment = next;
                currentRoot = lastProjection._targetSegment.ParentLane.Direction ? 1 : 0;
            }
            else
            {
                return lastProjection;
            }
            limitCount--;
        }

        return lastProjection;
    }

    private StoppingDataPackage GetStopsOnPath(PiavRoadAgent parentAgent, double distance, bool withTraffic, double startRoot, out Projection endProjection)
    {
        var resList = new StoppingDataPackage();

        var currentRoot = startRoot;
        var endRoot = parentAgent._desiredIncomingPath[0]._parentSegment.ParentLane.Direction && withTraffic || !parentAgent._desiredIncomingPath[0]._parentSegment.ParentLane.Direction && !withTraffic ?
                          parentAgent._desiredIncomingPath[0]._fromRoot : parentAgent._desiredIncomingPath[0]._toRoot;

        var lastProjection = new Projection{_remainingDistance = distance, _targetSegment = parentAgent._desiredIncomingPath[0]._parentSegment, _targetRoot = currentRoot};

        var sps = new List<StoppingPoint>();
        var dist = new List<double>();

        for (int i = 0; i < parentAgent._desiredIncomingPath.Count; i++)
        {
            foreach (var t in lastProjection._targetSegment.Stops)
            {
                if (t.Root >= Math.Min(currentRoot,endRoot) && t.Root <= Math.Max(currentRoot,endRoot))
                {
                    var tempProjection = RoadUtilities.GetProjectionTo(lastProjection._targetSegment.ArcLength, lastProjection._targetSegment.InvArcLength,lastProjection._targetSegment, currentRoot, t.Root, lastProjection._remainingDistance);

                    sps.Add(t);
                    dist.Add(distance - tempProjection._remainingDistance);
                }
            }
            lastProjection = RoadUtilities.GetProjectionTo(lastProjection._targetSegment.ArcLength, lastProjection._targetSegment.InvArcLength, lastProjection._targetSegment, currentRoot, endRoot, lastProjection._remainingDistance);

            if (Math.Abs(lastProjection._remainingDistance) <= 0.001 || i == parentAgent._desiredIncomingPath.Count - 1)
            {
                endProjection = new Projection{_remainingDistance = lastProjection._remainingDistance, _targetSegment = lastProjection._targetSegment, _targetRoot = lastProjection._targetRoot};
                resList._points = sps.ToArray();
                resList._distances = dist.ToArray();
                return resList;
            }

            lastProjection._targetSegment = parentAgent._desiredIncomingPath[i+1]._parentSegment;

            if (parentAgent._desiredIncomingPath[i+1]._parentSegment.ParentLane.Direction)
            {
                currentRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._toRoot : parentAgent._desiredIncomingPath[i+1]._fromRoot;
                endRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._fromRoot : parentAgent._desiredIncomingPath[i+1]._toRoot;
            }
            else
            {
                currentRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._fromRoot : parentAgent._desiredIncomingPath[i+1]._toRoot;
                endRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._toRoot : parentAgent._desiredIncomingPath[i+1]._fromRoot;
            }
        }

        endProjection = lastProjection;
        resList._points    = sps.ToArray();
        resList._distances = dist.ToArray();
        return resList;
    }

    private Projection ProjectOnPath(PiavRoadAgent parentAgent, double distance, bool withTraffic, double startRoot)
    {

        var currentRoot = startRoot;
        var endRoot = parentAgent._desiredIncomingPath[0]._parentSegment.ParentLane.Direction && withTraffic || !parentAgent._desiredIncomingPath[0]._parentSegment.ParentLane.Direction && !withTraffic ?
                          parentAgent._desiredIncomingPath[0]._fromRoot : parentAgent._desiredIncomingPath[0]._toRoot;

        var lastProjection = new Projection{_remainingDistance = distance, _targetSegment = parentAgent._desiredIncomingPath[0]._parentSegment,_targetRoot = currentRoot};

        for (int i = 0; i < parentAgent._desiredIncomingPath.Count; i++)
        {

            lastProjection = RoadUtilities.GetProjectionTo(lastProjection._targetSegment.ArcLength, lastProjection._targetSegment.InvArcLength, lastProjection._targetSegment, currentRoot, endRoot, lastProjection._remainingDistance);

            if (Math.Abs(lastProjection._remainingDistance) <= 0.001 || i == parentAgent._desiredIncomingPath.Count - 1)
                return lastProjection;

            lastProjection._targetSegment = parentAgent._desiredIncomingPath[i+1]._parentSegment;

            if (parentAgent._desiredIncomingPath[i+1]._parentSegment.ParentLane.Direction)
            {
                currentRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._toRoot : parentAgent._desiredIncomingPath[i+1]._fromRoot;
                endRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._fromRoot : parentAgent._desiredIncomingPath[i+1]._toRoot;
            }
            else
            {
                currentRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._fromRoot : parentAgent._desiredIncomingPath[i+1]._toRoot;
                endRoot = withTraffic ? parentAgent._desiredIncomingPath[i+1]._toRoot : parentAgent._desiredIncomingPath[i+1]._fromRoot;
            }
        }

        return lastProjection;
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

            for (int j = 0; j < divergenceRoots.Count - 1; j++)
                tempDualGraph[i].Add(new DualVertex(i * 10 + j, j == 0 ? 0:divergenceRoots[j], j == divergenceRoots.Count - 2 ? 1:divergenceRoots[j + 1], _segments[i]));

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
            foreach (var dv in collection) dv.Distance = RoadUtilities.GetArcLengthBetween(dv._parentSegmentPart._parentSegment.ArcLength, dv._parentSegmentPart._fromRoot, dv._parentSegmentPart._toRoot);
            _dualGraph.AddRange(collection);
        }
    }

    // ReSharper disable once UnusedMember.Local
    private void PrintDualGraph()
    {
        for (int i = 0; i < _dualGraph.Count; i++)
        {
            string s = "DV " +
                       i +
                       " " +
                       _dualGraph[i]._parentSegmentPart._parentSegment.Id +
                       " " +
                       _dualGraph[i]._parentSegmentPart._fromRoot +
                       " " +
                       _dualGraph[i]._parentSegmentPart._toRoot + " " + _dualGraph[i].Distance + "\nFl : ";

            foreach (var fol in _dualGraph[i].Followers) s   += _dualGraph.IndexOf(fol) + " ";
            s                                               += "\nPr : ";
            foreach (var pr in _dualGraph[i].Predecessors) s += _dualGraph.IndexOf(pr) + " ";
            Debug.Log(s);
        }
    }

    private void LinkDualGraph()
    {
        foreach (var dv in _dualGraph) dv._parentSegmentPart._parentSegment.DualVertices.Add(dv);
    }

    private DualVertex FindOriginVertice(List<DualVertex> list, int i, double originRoot)
    {
        foreach (var tdv in list)
        {
            if (_segments[i].ParentLane.Direction && Math.Abs(originRoot - tdv._parentSegmentPart._fromRoot) < 0.011 ||
                !_segments[i].ParentLane.Direction && Math.Abs(originRoot - tdv._parentSegmentPart._toRoot) < 0.011)
                return tdv;
        }
        //Debug.LogError("Origin Vertice Not Found");
        return null;
    }

    private DualVertex FindTargetVertice(List<DualVertex> list, int i, double targetRoot)
    {
        foreach (var tdv in list)
        {


            if (_segments[i].ParentLane.Direction && Math.Abs(targetRoot - tdv._parentSegmentPart._toRoot) < 0.011 ||
                !_segments[i].ParentLane.Direction && Math.Abs(targetRoot - tdv._parentSegmentPart._fromRoot) < 0.011)
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

        var arcL = Math.Sqrt(Math.Pow(myPoly[1, 0], 2) + Math.Pow(myPoly[1, 1], 2) + Math.Pow(myPoly[1, 2], 2));


        return new Segment()
        {
            Id         = _segments.Count,
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

    private void MakeIntermediateContact(Vector3 origin, Vector3 target, Lane parentLane, Segment originSg, Segment targetSg, double originRt, double targetRt)
    {
        var cS = CreateContactSegment(
                                      parentLane.MyType,
                                      parentLane.SpeedLimit,
                                      parentLane.Width,
                                      origin,
                                      target);
        _segments.Add(cS);

        var c1 = new PointContact(
                                  originRt,
                                  0,
                                  originSg,
                                  cS);

        originSg.Contacts.Add(c1);
        cS.IncomingContacts.Add(c1);

        var c2 = new PointContact(
                                  1,
                                  targetRt,
                                  cS,
                                  targetSg);

        cS.Contacts.Add(c2);
        targetSg.IncomingContacts.Add(c2);
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
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[originRoad.ChildLanes.Length - 1 - i], c.Origin,c.Target,c.OriginRoot,c.TargetRoot);
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
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                        MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                    MakeIntermediateContact(targetPoint, originPoint, originRoad.ChildLanes[i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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
                        MakeIntermediateContact(originPoint, targetPoint, targetRoad.ChildLanes[targetRoad.ChildLanes.Length - 1 - i], c.Origin, c.Target, c.OriginRoot, c.TargetRoot);
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

                        if (targetSegmentRoot._segment != null)
                            if (originLane.Direction)
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(targetSegmentRoot._segment,
                                                                                  targetSegmentRoot._root,
                                                                                  targetLane.Direction ? 4 : -4);
                                MakeIntermediateContact(
                                                        RoadUtilities.Calculate(targetSegmentRoot._segment.Polynomial, finalTargetRoot._targetRoot),
                                                        originPoint,
                                                        originLane,
                                                        targetSegmentRoot._segment,
                                                        originLane.ChildSegments[originLane.ChildSegments.Length - 1],
                                                        finalTargetRoot._targetRoot,
                                                        1);
                            }
                            else
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(
                                                                                  targetSegmentRoot._segment,
                                                                                  targetSegmentRoot._root,
                                                                                  targetLane.Direction ? -4 : 4);
                                MakeIntermediateContact(
                                                        originPoint,
                                                        RoadUtilities.Calculate(targetSegmentRoot._segment.Polynomial, finalTargetRoot._targetRoot),
                                                        targetLane,
                                                        originLane.ChildSegments[originLane.ChildSegments.Length - 1],
                                                        targetSegmentRoot._segment,
                                                        1,
                                                        finalTargetRoot._targetRoot);
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

                        if (targetSegmentRoot._segment != null)
                            if (!originLane.Direction)
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(
                                                                                  targetSegmentRoot._segment,
                                                                                  targetSegmentRoot._root,
                                                                                  targetLane.Direction ? 4 : -4);

                                MakeIntermediateContact(
                                                        RoadUtilities.Calculate(targetSegmentRoot._segment.Polynomial, finalTargetRoot._targetRoot),
                                                        originPoint,
                                                        originLane,
                                                        targetSegmentRoot._segment,
                                                        originLane.ChildSegments[0],
                                                        finalTargetRoot._targetRoot,
                                                        0);
                            }
                            else
                            {
                                var finalTargetRoot = RoadUtilities.GetProjection(
                                                                                  targetSegmentRoot._segment,
                                                                                  targetSegmentRoot._root,
                                                                                  targetLane.Direction ? -4 : 4);
                                MakeIntermediateContact(
                                                        originPoint,
                                                        RoadUtilities.Calculate(targetSegmentRoot._segment.Polynomial, finalTargetRoot._targetRoot),
                                                        originLane,
                                                        originLane.ChildSegments[0],
                                                        targetSegmentRoot._segment,
                                                        0,
                                                        finalTargetRoot._targetRoot);
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

            if (res._segment != null)
            {
                var stp = new StoppingPoint()
                {
                    Origin      = null,
                    ParentSegment = res._segment,
                    Root          = res._root,
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

            if (res._segment != null)
            {
                var ps = new ParkingSpace()
                {
                    ParentSegment = res._segment,
                    Root          = res._root,
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

    [Serializable]
    internal struct PolyPoint
    {
        internal Segment _segment;
        internal double _root;
    }

    internal PolyPoint FindClosestSegmentInRoad(Road road, Vector3 pos, LaneType type)
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

                    if (possibleRoot._distance < minimalDistance)
                    {
                        minimalDistance = possibleRoot._distance;
                        closestSegment  = segment;
                        closestRoot     = possibleRoot._root;
                    }
                }
        }

        return new PolyPoint{_segment = closestSegment,_root = closestRoot};
    }

    internal PolyPoint FindClosestSegmentInLane(Lane lane, Vector3 pos)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        foreach (var segment in lane.ChildSegments)
        {
            var possibleRoot = RoadUtilities.GetClosestRoot(segment.Polynomial, pos);

            if (possibleRoot._distance < minimalDistance)
            {
                minimalDistance = possibleRoot._distance;
                closestSegment  = segment;
                closestRoot     = possibleRoot._root;
            }
        }

        return new PolyPoint{_segment = closestSegment, _root = closestRoot};
    }

    internal PolyPoint FindClosestSegment(Vector3 pos, LaneType type)
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

                    if (possibleRoot._distance < minimalDistance)
                    {
                        minimalDistance = possibleRoot._distance;
                        closestSegment  = segment;
                        closestRoot     = possibleRoot._root;
                    }
                }
            }

            return new PolyPoint{_segment = closestSegment, _root = closestRoot};
        }

        return new PolyPoint();
    }

    [Serializable]
    internal struct ValuedSegment
    {
        internal Segment _segment;
        internal double _root;
        internal double _distance;
        internal double _heuristic;
    }

    // return segment, root, distance
    private List<ValuedSegment> FindAllSegmentsWithinLaneWidth(Vector3 pos, LaneType type, Vector3 direction)
    {
        if (_segments != null)
        {
            var res = new List<ValuedSegment>();

            foreach (var segment in _segments)
            {
                if (segment.ParentLane.MyType == type)
                {
                    var possibleRoot = RoadUtilities.GetClosestRootWithoutYComponent(segment.Polynomial, pos);

                    if (possibleRoot._distance < segment.ParentLane.Width)
                    {
                        var dot = Vector3.Dot(RoadUtilities.CalculateFirstDerivative(segment.Polynomial, possibleRoot._root).normalized, direction.normalized);
                        res.Add(new ValuedSegment{_segment = segment,_root = possibleRoot._root, _distance = possibleRoot._distance,_heuristic = dot/(possibleRoot._distance + 1)*(segment.ParentLane.Direction?-1:1)});
                    }
                }
            }
            res.Sort((t1,t2) => t2._heuristic.CompareTo(t1._heuristic));
            return res;
        }

        return new List<ValuedSegment>();
    }

    internal PolyPoint FindRandomSegmentAndRoot()
    {
        return new PolyPoint{_segment = _segments[(int)(UnityEngine.Random.value * _segments.Count)],_root = UnityEngine.Random.value};
    }

    internal PolyPoint FindClosestAdjacentSegment(Vector3 pos, Segment originSegment)
    {
        Segment closestSegment  = null;
        double  closestRoot     = double.NaN;
        double  minimalDistance = double.MaxValue;

        var selfRoot = RoadUtilities.GetClosestRoot(originSegment.Polynomial, pos);
        if(selfRoot._root > 0 && selfRoot._root < 1)

        foreach (var contact in originSegment.Contacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact.Target.Polynomial, pos);

            if (possibleRoot._distance < minimalDistance)
            {
                minimalDistance = possibleRoot._distance;
                closestSegment  = contact.Target;
                closestRoot     = possibleRoot._root;
            }
        }

        foreach (var contact in originSegment.IncomingContacts)
        {

            var possibleRoot = RoadUtilities.GetClosestRoot(contact.Origin.Polynomial, pos);

            if (possibleRoot._distance < minimalDistance)
            {
                minimalDistance = possibleRoot._distance;
                closestSegment  = contact.Target;
                closestRoot     = possibleRoot._root;
            }
        }

        return new PolyPoint{_segment = closestSegment, _root = closestRoot};
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

        if (_segments != null && _segments.Count != 0 && _dualGraph.Count != 0)
            foreach (var dv in _dualGraph)
            {
                var st = RoadUtilities.Calculate(dv._parentSegmentPart._parentSegment.Polynomial, dv._parentSegmentPart._fromRoot);
                var en = RoadUtilities.Calculate(dv._parentSegmentPart._parentSegment.Polynomial, dv._parentSegmentPart._toRoot);
                Gizmos.DrawLine(st, en);
            }
    }

    private void DrawRequestedGizmos()
    {
        foreach (var t in _gizmosRequests)
        {
            Gizmos.color = t._requestedColor;
            foreach (var v in t._points) Gizmos.DrawSphere(v, 0.5f);
        }
        _gizmosRequests.Clear();
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

    [HideInInspector] internal List<DualVertex> _dualGraph = new List<DualVertex>();
    [HideInInspector] internal List<RequestedGizmo> _gizmosRequests = new List<RequestedGizmo>();

    #endregion

    [Serializable]
    internal struct RequestedGizmo
    {
        internal List<Vector3> _points;
        internal Type _origin;
        internal Color _requestedColor;
    }
}

public class DualVertex
{
    internal DualVertex(int id, double startRt, double endRt, Segment parent)
    {
        Id = id;
        _parentSegmentPart = new SegmentPart{_parentSegment = parent,_fromRoot = startRt,_toRoot = endRt};
    }

    public int Id;
    public List<DualVertex> Predecessors = new List<DualVertex>();
    public List<DualVertex> Followers    = new List<DualVertex>();

    public double                               Distance;
    internal SegmentPart _parentSegmentPart;

    public double AccumulatedDistance;
    public DualVertex PathPredecessor;

    #region Overrides of Object

    /// <inheritdoc />
    public override string ToString()
    {
        return "Dual Vertice " + _parentSegmentPart._parentSegment.Id + " (" + _parentSegmentPart._fromRoot + " - " + _parentSegmentPart._toRoot + ")";
    }

    #endregion

    [Serializable]
    internal struct SegmentPart
    {
        internal Segment _parentSegment;
        internal double _fromRoot;
        internal double _toRoot;
    }
}

internal enum ContactStart
{
    Start, End
}

internal enum ContactEnd
{
    Start, End, Side
}
