using System.Collections.Generic;

public class PiavCoreAiInput : PiavInput {

    private void Awake()
    {
        _behaviors = new List<AiBehavior>
        {
            new BehaviorLaneFollow(this),
            new BehaviorMaintainSpeed(this),
            new BehaviorRespectRoadSpeedLimit(this),
            new BehaviorReduceSpeedOnSharpTurn(this)
            //new BehaviorAdaptSpeedToSlowerVehicle(this)
            //new BehaviorFindPath(this),
            //new BehaviorFollowPath(this),
            //new BehaviorSetPathTarget(this)
        };
    }

    private void Update()
    {
        if (_parentAgent == null) _parentAgent = GetComponent<PiavRoadAgent>();

        foreach (var behavior in _behaviors) behavior.Update(_parentAgent);
    }

    internal AgentProfile _profile = new AgentProfile();
    private List<AiBehavior> _behaviors;
}
