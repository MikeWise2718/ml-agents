using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Policies;
using MLAgents.Sensors;
using TMPro;
using Barracuda;

public class CmvAgent : Agent
{
    public GameObject ground;
    public GameObject area;
    public GameObject redGoal;
    public List<CmvAgent> otherAgents;
    //public bool useVectorObs;
    public RayPerceptionSensorComponent3D rayPer;

    Material groundMaterial;
    Renderer groundRenderer;
    //public CmvAcademy academy;
    CmvAgMan cmvAgMan;
    public CmvAgentBody cmvagbod;

    int selection;
    int nmoves;
    public TextMeshPro bannertmp = null;
    public GameObject bannergo = null;
    public GameObject avatar = null;
    public bool showStartMarker = false;
    public SpaceType sType = SpaceType.Continuous;
    public int obsSize = 0;


    public void SetupAgentSpaceType(SpaceType reqstype)
    {
        Debug.Log($"SetupAgentSpaceType for {name}");
        var bphp = GetComponent<BehaviorParameters>();

        var bp = bphp.brainParameters;
        bp.vectorActionSpaceType = reqstype;
        this.sType = reqstype;
        switch (reqstype)
        {
            case SpaceType.Continuous:
                {
                    bp.vectorObservationSize = obsSize;
                    bp.vectorActionSize = new int[] { 2 };
                    bp.vectorActionDescriptions = new string[] { "rotation", "translation" };
                    break;
                }
            case SpaceType.Discrete:
                {
                    bp.vectorObservationSize = obsSize;
                    bp.vectorActionSize = new int[] { 4 };
                    bp.vectorActionDescriptions = new string[] { "rotate-right", "rotate-left", "move-forward", "move-backward" };
                    break;
                }
        }
        var nn = ScriptableObject.CreateInstance<NNModel>();
        if (cmvAgMan.cmvSettings.runType== CmvSettings.RunType.BrainEquiped)
        {
            //var modelData = new NNModelData(); // don't do it like this....
            var modelData = ScriptableObject.CreateInstance<NNModelData>();
            var bpath = cmvAgMan.cmvSettings.GetBrainPath();
            modelData.Value = System.IO.File.ReadAllBytes(bpath);
            nn.modelData = modelData;
            //LazyInitialize(); // gets called when component is added in OnEnable
            SetModel("CrowdMove", nn, InferenceDevice.CPU);
        }
    }
    public void SetupAgent(CmvAgMan cmvAgMan)
    {
        Debug.Log($"SetupAgent for {name}");
        this.cmvAgMan = cmvAgMan;
        SetupAgentSpaceType(SpaceType.Continuous);
        //LazyInitialize();// gets called when component is added in OnEnable

        area = cmvAgMan.area;
        ground = cmvAgMan.ground;
        redGoal = cmvAgMan.redGoal;
        //useVectorObs = true;

        // Create body
        avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        avatar.name = "body";
        avatar.transform.parent = transform;
        cmvagbod = avatar.AddComponent<CmvAgentBody>();
        var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var clder = visor.GetComponent<Collider>();
        clder.enabled = false;
        visor.transform.parent = avatar.transform;
        visor.transform.localScale = new Vector3(0.95f, 0.25f, 0.5f);
        visor.transform.position   = new Vector3(0,   0.5f, -0.25f);
        CmvAgentBody.SetColorOfGo(visor, Color.black);
        maxStep = cmvAgMan.cmvSettings.maxstep;

        cmvagbod.Init(this);

        groundRenderer = ground.GetComponent<Renderer>();
        groundMaterial = groundRenderer.material;
        cmvagbod.InitializeAgentBody();

    }
    private void Awake()
    {
        Debug.Log("CmvAgent Awake");
    }
 
    public override void Initialize()
    {
        Debug.Log($"Called Initialize Override on {name}");
        // called by Agent.OnEnableHelper that is called when the agent is enabled or becomes active
        base.Initialize();
        //academy = FindObjectOfType<CmvAcademy>();


        if (showStartMarker)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = transform;
            Destroy(sphere.GetComponent<SphereCollider>());
        }


        nmoves = 0;
        Debug.Log("Initialized agent " );
    }
    public override float[] Heuristic()
    {
        Debug.Log($"Called Heuristic override on {name} sType:{sType}");

        switch (sType)
        {
            case SpaceType.Continuous:
                {
                    var r1 = Random.Range(-1f, 1f);
                    var r2 = Random.Range(-1f, 1f);
                    return new float[] { r1, r2 };
                }
            case SpaceType.Discrete:
            {
                    if (Input.GetKey(KeyCode.D))
                    {
                        return new float[] { 3 };
                    }
                    if (Input.GetKey(KeyCode.W))
                    {
                        return new float[] { 1 };
                    }
                    if (Input.GetKey(KeyCode.A))
                    {
                        return new float[] { 4 };
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        return new float[] { 2 };
                    }
                    return new float[] { 0 };
                }
        }
        return new float[] { 0,0 };
    }
    public void CrowdManInit()
    {
        FindOtherAgents();
    }
    public void FindOtherAgents()
    {
        otherAgents = new List<CmvAgent>(FindObjectsOfType<CmvAgent>());
        otherAgents.Remove(this);
        var removeList = new List<CmvAgent>();
        foreach ( var agent in otherAgents)
        {
            if (agent.area.name!=area.name)
            {
                removeList.Add(agent);
            }
        }
        foreach( var agent in removeList)
        {
            otherAgents.Remove(agent);
        }
        Debug.Log("FindOtherAgents - Agent " + name + " has " + otherAgents.Count + " neighbors");
        var aglist = "";
        var ln = otherAgents.Count;
        foreach(var ag in otherAgents)
        {
            aglist += ag.name;
            if (ag!=otherAgents[ln-1])
            {
                aglist += ",";
            }
        }
        Debug.Log("otherAgents:"+aglist);
    }
    int ncollected = 0;
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log($"Called CollectObservations override for {name}");
        //if (useVectorObs)
        //{
        //   // var rayout = RayPerceptionSensor.Perceive(rayPer.GetRayPerceptionInput());
        //    //sensor.AddObservation(rayout.rayOutputs[0]);
        //}
        sensor.AddObservation(0);
        ncollected++;
    }
    public Vector3 GetPos()
    {
        return avatar.transform.position;
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        //Debug.Log("Swapping ground mat to " + mat.name);
        groundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        groundRenderer.material = groundMaterial;
        //Debug.Log("Swapping ground back ");
    }
    string closestString = "";
    public float FindClosestAgentDist()
    {
        closestString = "";
        var mindist = 9e9f;
        var ln = otherAgents.Count;
        var i = 0;
        var pos = GetPos();
        foreach(var agent in otherAgents)
        {
            var dst = Vector3.Distance(agent.GetPos(), pos);
            if (dst<mindist)
            {
                mindist = dst;
            }
            closestString += agent.name + ":" + dst.ToString("f1");
            if (i<=ln-1)
            {
                closestString += ",";
            }
        }
        closestString += " mindist:"+mindist.ToString("f1");
        return mindist;
    }
    float hitFlashTimeMark = -10;
    float flashTime = 0.1f;

    //Color c0 = GraphAlgos.GraphUtil.getcolorbyname("blue", alpha: 1);
    //Color c1 = GraphAlgos.GraphUtil.getcolorbyname("green", alpha: 1);
    //Color c2 = GraphAlgos.GraphUtil.getcolorbyname("red", alpha: 1);
    //Color c3 = GraphAlgos.GraphUtil.getcolorbyname("black", alpha: 1);
    //Color c4 = GraphAlgos.GraphUtil.getcolorbyname("purple", alpha: 1);
    Color c0 = Color.blue;
    Color c1 = Color.green;
    Color c2 = Color.red;
    Color c3 = Color.black;
    Color c4 = Color.magenta;


    Vector3 lastpos = Vector3.zero;
    public void MoveAgent(float[] act)
    {

        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;
        Vector3 fwdDir = avatar.transform.forward;
        Vector3 upDir = avatar.transform.up;
        if (sType== SpaceType.Continuous)
        {
            dirToGo = fwdDir * Mathf.Clamp(act[0], -1f, 1f);
            rotateDir = upDir * Mathf.Clamp(act[1], -1f, 1f);
        }
        else
        {
            int action = Mathf.FloorToInt(act[0]);
            switch (action)
            {
                case 1:
                    dirToGo = fwdDir * 1f;
                    break;
                case 2:
                    dirToGo = fwdDir * -1f;
                    break;
                case 3:
                    rotateDir = upDir * 1f;
                    break;
                case 4:
                    rotateDir = upDir * -1f;
                    break;
            }
        }

        var forceVek = dirToGo*cmvAgMan.cmvSettings.agentRunSpeed;
        var dist = Vector3.Magnitude(avatar.transform.position - lastpos);
        var force = Vector3.Magnitude(forceVek);
        //Debug.Log("Move "+name+" "+sType+" rot:"+rotateDir.ToString("F1")+" force:"+forceVek.ToString("F3")+" dst:"+dist.ToString("f1"));
        if (bannertmp!=null)
        {
            var hitstring = cmvagbod.rpi.GetHitObs();
            bannertmp.text = name + "\n" + hitstring + "\n" +  hitFlashTimeMark.ToString("f1");
        }
        if (avatar!=null)
        {
            Color c = c0;
            var stepCount = StepCount;
            if (Time.time-hitFlashTimeMark<flashTime)
            {
                c = c4;
            }
            else if (stepCount<0.05f*maxStep)
            {
                var lamb = stepCount / (0.05f * maxStep);
                c = Color.Lerp(c0, c1, lamb);
            }
            else
            {
                var lamb = stepCount / (1.0f * maxStep);
                c = Color.Lerp(c1, c2, lamb);
            }
            CmvAgentBody.SetColorOfGo(avatar,c);
        }
        cmvagbod.AddMovement(rotateDir,forceVek, ForceMode.VelocityChange);
        cmvagbod.SyncToBody(bannergo);
        lastpos = avatar.transform.position;
        nmoves++;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        var sva = "";
        for(int i=0; i<vectorAction.Length; i++)
        {
            sva += vectorAction[i].ToString("f2")+"  ";
        }
        Debug.Log($"Called OnActionReceived override for {name} vectorAction:{sva}");

        AddReward(-1f / maxStep);
        var mindist = FindClosestAgentDist();
        if (mindist<3)
        {
            AddReward(-0.5f);
            hitFlashTimeMark = Time.time;
            //StartCoroutine(GoalScoredSwapGroundMaterial(academy.failMaterial, 0.5f)); // happens too often to work well
        }
        MoveAgent(vectorAction);
    }
    public void ReachedGoal()
    {
        SetReward(5.0f);
        StartCoroutine(GoalScoredSwapGroundMaterial(cmvAgMan.cmvSettings.goalScoredMaterial, 1.0f));
        cmvAgMan.cmvSettings.RegisterSuccess(this.nmoves);

        Debug.Log("Found goal - calling done in " + area.name + "   agent:" + name+" moves:"+nmoves);
        EndEpisode();
    }
    public void RegisterCollision()
    {
        SetReward(-1.0f);
        cmvAgMan.cmvSettings.RegisterCollision();

        //Debug.Log("Found goal - calling done in " + area.name + "   agent:" + name+" moves:"+nmoves);
    }

    public override void OnEpisodeBegin()
    {
        if (nmoves>0)
        {
            cmvAgMan.cmvSettings.RegisterFailure(this.nmoves);
        }
        Debug.Log($"Called OnEpisodeBegin override on {name} in {area.name}  nmoves:{nmoves}");
        float agentOffset = -15f;

        selection = Random.Range(0, 2);

        var xpos = 0f + Random.Range(-3f, 3f);
        var ypos = 1f;
        var zpos = agentOffset + Random.Range(-5f, 5f);
        transform.position = new Vector3(xpos, ypos, zpos) + ground.transform.position;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        cmvagbod.AgentReset();
        redGoal.transform.position = new Vector3(0f, 0.5f, 9f) + area.transform.position;
        nmoves = 0;
    }
}
