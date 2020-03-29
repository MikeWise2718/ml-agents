using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents.Sensors;

public class CmvAgentBody : MonoBehaviour
{
    public CmvAgent parentAgent;
    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Init(CmvAgent parentAgent)
    {
        this.parentAgent = parentAgent;
        rb = gameObject.AddComponent<Rigidbody>();
        //rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezePositionY;
        rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
        rb.mass = 25;
        rb.drag = 2;
        var tag = "agent";
        gameObject.tag = tag;
        for(int i=0; i<transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            child.tag = tag;
        }
    }
    public void InitializeAgentBody()
    {
        //rayPer = parentAgent.GetComponent<RayPerception>();
        //rayPer = rayPerComp.CreateSensor();
        rb.velocity = Vector3.zero;
    }
    public void AddMovement(Vector3 rotVek, Vector3 forceVek, ForceMode forceMode)
    {
        transform.Rotate(rotVek, Time.deltaTime * 150f);
        //var nrotVek = rotVek * Time.deltaTime * 150f;
        //var rfvek = Quaternion.Euler(nrotVek) * forceVek;
        //Debug.Log("nrotVek:"+nrotVek.ToString("f1")+" forceVek:" + forceVek.ToString("f1") + "  rfvek:" + rfvek.ToString("f1"));
        rb.AddForce(forceVek, ForceMode.VelocityChange);
        parentAgent.transform.position = transform.position;
    }
    public void AgentReset()
    {
        rb.velocity = Vector3.zero;
        transform.position = parentAgent.transform.position;
    }

    public RayPerceptionInterpreter rpi = null;
    public class RayPerceptionInterpreter
    {
        string aname;
        float rayDistance;
        float[] rayAngles;
        string[] detectableObjects;
        int pbufSize;
        Dictionary<string, bool> classHit;
        Dictionary<string, float> classRay;
        Dictionary<float, string> rayHit;
        Dictionary<float, float> rayHitDist;
        public RayPerceptionInterpreter(string aname, float rayDistance, float[] rayAngles, string[] detectableObjects)
        {
            this.aname = aname;
            this.rayDistance = rayDistance;
            this.rayAngles = rayAngles;
            this.detectableObjects = detectableObjects;
            pbufSize = rayAngles.Length * (detectableObjects.Length + 2);
        }
        public void InitDicts()
        {
            classHit = new Dictionary<string, bool>();
            foreach (var cls in detectableObjects) classHit[cls] = false;
            classRay = new Dictionary<string, float>();
            rayHit = new Dictionary<float, string>();
            rayHitDist = new Dictionary<float, float>();
        }
        public bool Perceive(List<float> values)
        {
            var ok = true;
            InitDicts();
            if (values.Count != pbufSize)
            {
                ok = false;
                Debug.LogError($"{aname} bad perceivebuffer of length:{values.Count} expected:{pbufSize}");
                return ok;
            }
            int i = 0;
            foreach (var rang in rayAngles)
            {
                var hitcls = "";
                foreach (var cls in detectableObjects)
                {
                    if (values[i] > 0)
                    {
                        classHit[cls] = true;
                        classRay[cls] = rang;
                        rayHit[rang] = cls;
                        hitcls = cls;
                    }
                    i++;
                }
                if (hitcls != "" && values[i] == 1)
                {
                    ok = false;
                    Debug.LogError($"{aname} perceivebuffer inconsistency for ray-ang:{rang} hitcls:{hitcls} but coded to not hit");
                }
                else if (hitcls == "" && values[i] == 0)
                {
                    ok = false;
                    Debug.LogError($"{aname} perceivebuffer inconsistency for ray-ang:{rang} hitcls empty but code to a hit - maybe hit untagged object");
                }
                i++;
                rayHitDist[rang] = rayDistance * values[i];
                i++;
            }
            return ok;
        }
        public bool HitDetected(string cls)
        {
            return classHit[cls];
        }
        public string GetHitObs()
        {
            var rv = "";
            var nhit = 0;
            foreach (var cls in detectableObjects)
            {
                if (classHit[cls])
                {
                    if (nhit > 0) rv += ",";
                    rv += cls;
                    nhit++;
                }
            }
            return rv;
        }
        public void DumpRays()
        {
            foreach (var rang in rayAngles)
            {
                if (rayHit.ContainsKey(rang))
                {
                    var msg = "    " + aname + "   " + rang + " hit " + rayHit[rang] + " dist:" + rayHitDist[rang];
                    Debug.Log(msg);
                }
            }
        }
    }
    // size of array is (number of rays*number of classes+2)
    // everything initied to zero
    // then by ray
    // one hot encoding of class if hit by ray
    // then a one added if nothing was hit (to make it easier
    // the distance of that hit is stored in the final float
    float rayDistance = 24f;
    float[] iniRayAngles = { 20f, 60f, 90f, 120f, 160f };
    float rayAngleOffset = -180;
    float[] rayAngles;
    //string[] detectableObjects = { "redGoal", "agent", "wall", "orangeBlock", "redBlock" };
    string[] detectableObjects = { "redGoal", "agent", "wall", "wall" , "wall" };

    void initRayAngles()
    {
        rayAngles = new float[iniRayAngles.Length];
        for( int i=0; i<iniRayAngles.Length; i++)
        {
            rayAngles[i] = iniRayAngles[i] + rayAngleOffset;
        }
    }
    public List<float> Perceive()
    {
        if (rpi == null)
        {
            initRayAngles();
            rpi = new RayPerceptionInterpreter(name, rayDistance, rayAngles, detectableObjects);
        }
        //if (rayPer==null)
        //{
        //    InitializeAgentBody();
        //}
        //var rayobs = rayPer.raySensor.(rayDistance, rayAngles, detectableObjects, 0, 0);
        //var ok = rpi.Perceive(rayobs);
        //if (!ok)
        //{
            // try again for debugger
          //  rayobs = rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0, 0);
        //}
        //return rayobs;
        return new List<float>();
    }
    public void SyncToBody(GameObject go)
    {
        if (go != null)
        {
            go.transform.position = transform.position;
            go.transform.localRotation = transform.localRotation;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("redGoal"))
        {
            parentAgent.ReachedGoal();
        }
        else if (col.gameObject.CompareTag("agent"))
        {
            parentAgent.RegisterCollision();
        }
    }
    static Shader transShader = null;
    public static void SetColorOfGo(GameObject go, Color cclr)
    {

        var mat = go.GetComponent<Renderer>().material;
        if (cclr.a < 1f)
        {

            if (transShader == null)
            {
                transShader = Shader.Find("Transparent/Diffuse");
            }
            if (transShader != null)
            {
                mat.shader = transShader;
            }
        }
        mat.SetColor("_Color", cclr);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
