using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CWWheel : MonoBehaviour
{
    public const float gAcc = 9.81f;
    public float EPSILON = 0.001f;

    [Header("References")]
    [SerializeField]
    public Transform wheelMesh;
    [SerializeField]
    private CWPacejka pacejka;

    [Space]

    [Header("Suspension")]
    [SerializeField]
    private float restLength;
    [SerializeField]
    private float springTravel;
    [SerializeField]
    private float springStiffness;
    [SerializeField]
    private float springDamping;
    [SerializeField]
    private float maxWheelVelocity;
    [SerializeField]
    private float maxWheelVelocityAcc;
    [SerializeField]
    private float antiRollStiffness;
    [SerializeField]
    private bool applyForceGround = true;
    [SerializeField]
    private bool smoothStep = false;

    [Space]

    [Header("Wheel")]
    [SerializeField]
    private float wheelRadius;
    [SerializeField]
    private float wheelMass = 10f;
    [SerializeField]
    private float slowCorrectionStart = 1f;
    [SerializeField]
    private float slowCorrectionForce = 0.5f;
    [SerializeField]
    private CWWheel antiRollbarWheel;
    [SerializeField]
    private float wheelSphereJumpThreshold = 0.3f;
    [SerializeField]
    private float wheelSphereJumpMaxAngle = 5f;
    [SerializeField]
    private float wheelSphereJumpMinAngleNormals = 5f;
    [SerializeField]
    private float wheelSphereJumpMinGroundTime = 0.3f;
    [SerializeField]
    private float wheelSphereJumpNormalDistanceMin = 0.3f;
    [SerializeField]
    private float wheelMaxForce = 10f;

    public bool debugMessages = false;



    private float maxLength;
    private float minLength;
    private float springLength;
    private float springForce;
    private float springLengthOld;
    private float damperForce;
    private float springVelocity;
    private float springLengthRay;
    private float springVelocityRay;
    private float springForceRay;
    private float damperForceRay;
    private float suspensionForceRay;
    private float springVelocityOld;
    private float antiRollSpringForce;

    private Vector3 suspensionForce;
    private Vector3 wheelVelocity;
    private float fx;
    private float fy;

    private float wheelAngle;


    public float angularVelocity;
    private float wheelHubVelocityLongitudinal;
    private float tireTreadLongitudinalVelocity;
    private float slipVelocityLongitudinal;
    private float slipLongitudinal;
    private float slipLongitudinalRaw;
    private float verticalTireLoad;
    private float slipAngle;
    private float slip90Angle;
    private float camberAngle = 0f;
    private float antiRollDifference = 0f;

    private float oldWheelSphereDistance = -1f;

    private bool wheelIsGrounded = false;

    private Vector3 normalGround;
    private Vector3 groundHit;

    private float wheelTurnFac = 180f / Mathf.PI;

    private float groundedForTime = 0f;



    private Transform wheelSphere;
    private Rigidbody rb;
    private CWWheelTorqueDistr torqueDistr;

    //public Transform wheelVelocityDebug;



    void Start()
    {
        if (pacejka == null)
        {
            pacejka = GameObject.FindObjectOfType<CWPacejka>();
        }
        rb = GetComponentInParent<Rigidbody>();
        torqueDistr = GetComponent<CWWheelTorqueDistr>();

        minLength = restLength - springTravel;
        maxLength = restLength + springTravel;

        springLength = restLength;
        springLength = Mathf.Clamp(springLength, minLength, maxLength);
        springLengthOld = springLength;

        if (smoothStep)
        {
            GameObject instSphere = null;
            if (wheelSphere == null)
            {
                instSphere = new GameObject("(Inst) Car Sphere");
            }
            else
            {
                instSphere = wheelSphere.gameObject;
            }
            CapsuleCollider sc = instSphere.AddComponent<CapsuleCollider>();
            sc.radius = 0.5f;
            sc.direction = 0;
            sc.height = 3f;
            wheelSphere = instSphere.transform;
            wheelSphere.localScale = new Vector3(wheelRadius * 2f, wheelRadius * 2f, wheelRadius * 2f);

            wheelSphere.position = new Vector3(0f, -10000f, 0f);

        }
    }

    private void Update()
    {
        wheelAngle = Mathf.Lerp(wheelAngle, SteerAngle, Time.deltaTime * 10f);
        transform.localRotation = Quaternion.Euler(transform.localRotation.x, wheelAngle, transform.localRotation.z);

        Debug.DrawRay(transform.position, -transform.up * (springLength + wheelRadius), Color.green);

        wheelMesh.localPosition = new Vector3(0f, -springLength, 0f);
        wheelMesh.Rotate(Vector3.right, angularVelocity * Time.deltaTime * wheelTurnFac, Space.Self);

        if (debugMessages)
        {
            /*if (Input.GetKeyDown(KeyCode.A))
            {
                springLength = minLength;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                springLength = maxLength;
            }*/
        }

        //antiRollDifference = antiRollbarWheel.SpringLength - SpringLength;

        if (IsGrounded == false)
        {
            groundedForTime = 0f;
        }
        else
        {
            groundedForTime += Time.deltaTime;
        }

        //if (debugMessages) GraphManager.Graph.Plot("Longitude", SlipForward, Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));
        //if (debugMessages) GraphManager.Graph.Plot("Longitudeds", SlipSidewards, Color.green, new Rect(new Vector2(10f, 130f + 260f), new Vector2(1000f, 250f)));
    }

    private bool wentInOnce = false;
    public float fcSide = 1f;
    void FixedUpdate()
    {
        //angularVelocity += Input.GetAxis("Vertical");
        //if (debugMessages) GraphManager.Graph.Plot("Longitude", angularVelocity, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));

        


        RaycastHit hit;
        if (Physics.Raycast(new Ray(transform.position, -transform.up), out hit, maxLength + wheelRadius))
        {
            normalGround = hit.normal;
            groundHit = hit.point;

            springLengthRay = hit.distance - wheelRadius;
            springLengthRay = Mathf.Clamp(springLengthRay, minLength, maxLength);

            springVelocityRay = (springLengthOld - springLengthRay) / Time.fixedDeltaTime;
            springVelocityRay = Mathf.Clamp(springVelocityRay, -maxWheelVelocity, maxWheelVelocity);
            //if (debugMessages) GraphManager.Graph.Plot("Longitude", springVelocityOld - springVelocityRay, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));
            /*if (Mathf.Abs(springVelocityOld - springVelocityRay) > maxWheelVelocityAcc)
            {
                if (debugMessages) Debug.Log("Capped");
                springVelocityRay = Mathf.MoveTowards(springVelocityOld, springVelocityRay, maxWheelVelocityAcc);
                springLengthRay = springLengthOld - springVelocityRay * Time.fixedDeltaTime;
            }*/

            antiRollSpringForce = antiRollStiffness * antiRollDifference;

            springForceRay = springStiffness * (restLength - springLengthRay);
            damperForceRay = springDamping * springVelocityRay;

            if (antiRollSpringForce > 0f)
            {
                antiRollSpringForce = 0f;
            }

            suspensionForceRay = (springForceRay + damperForceRay + antiRollSpringForce);

            // If it would pull the car down to the ground
            if (suspensionForceRay < -EPSILON)
            {
                //if (debugMessages) Debug.Log("Car would be pulled");
                //springLength = hit.distance - wheelRadius;
                //springLengthOld = springLength;
                calculateNoHit(springLengthRay);
            }
            else
            {
                wheelIsGrounded = true;
                Vector3 groundVelocity = Vector3.zero;
                if (hit.collider.GetComponent<Rigidbody>() != null)
                {
                    groundVelocity = hit.collider.GetComponent<Rigidbody>().GetPointVelocity(hit.point);
                }


                springLength = springLengthRay;
                springVelocity = springVelocityRay;

                //if (debugMessages) GraphManager.Graph.Plot("Longitude", springVelocity, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));
                springForce = springForceRay;
                damperForce = damperForceRay;



                //if (debugMessages) Debug.Log("Force calc");
                wheelVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(hit.point) - groundVelocity);
                //fx = Input.GetAxis("Vertical") * springForceRay;
                //fy = wheelVelocity.x * springForceRay;
                //if (debugMessages) GraphManager.Graph.Plot("Longitude", wheelVelocity.z, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));

                //wheelVelocityDebug.up = Vector3.up;
                //wheelVelocityDebug.forward = wheelVelocity;

                // Forces calculation


                wheelHubVelocityLongitudinal = wheelVelocity.z;
                tireTreadLongitudinalVelocity = angularVelocity * wheelRadius;
                slipVelocityLongitudinal = tireTreadLongitudinalVelocity - wheelHubVelocityLongitudinal;
                slipLongitudinalRaw = slipVelocityLongitudinal;
                slipLongitudinal = slipVelocityLongitudinal / Mathf.Abs(wheelHubVelocityLongitudinal);
                verticalTireLoad = suspensionForceRay;
                verticalTireLoad = verticalTireLoad / 1000f;

                if (torqueDistr.hasMotor == false && Mathf.Abs(slipLongitudinal) < 1f)
                {
                    //if (debugMessages) Debug.Log("Set angular vel");


                    // Adjusts the angular velocity to the ground velocity on small difference. But is done in CWWheelTorqueDistr now
                    //angularVelocity = wheelHubVelocityLongitudinal / wheelRadius;

                    tireTreadLongitudinalVelocity = angularVelocity * wheelRadius;
                    slipVelocityLongitudinal = tireTreadLongitudinalVelocity - wheelHubVelocityLongitudinal;
                    slipLongitudinalRaw = slipVelocityLongitudinal;
                    slipLongitudinal = slipVelocityLongitudinal / Mathf.Abs(wheelHubVelocityLongitudinal);
                }


                /*Vector2 wheelVelocity2D = new Vector2(wheelVelocity.x, wheelVelocity.z);
                Vector2 selfForward2D = new Vector2(transform.forward.x, transform.forward.z);
                Vector2 selfRight2D = new Vector2(transform.right.x, transform.right.z);

                slipAngle = Vector2.Angle(selfForward2D, wheelVelocity2D);
                slip90Angle = Vector2.Angle(selfRight2D, wheelVelocity2D);
                if (slip90Angle > 90f)
                {
                    slipAngle = -slipAngle;
                }*/



                // TODO approximate slipAngle at low velocities
                slipAngle = Mathf.Atan(wheelVelocity.x / wheelVelocity.z * Mathf.Sign(wheelVelocity.z)) * 180f / Mathf.PI;
                //slipAngle *= 1f - Mathf.Abs( Mathf.Clamp(slipLongitudinal, -1f, 1f));

                //slipAngle *= (1f / (Mathf.Abs(slipLongitudinal) * 100f + 2f)) + 0.5f;
                float c = 0.2f;
                //slipAngle *= (1f / (Mathf.Abs(slipLongitudinal) * 10f + 1f / (1f - c))) + c;

                //Debug.Log(wheelVelocity.ToString() + "__" + slipVelocityLongitudinal.ToString());

                Vector2 wheelVelocity2D = new Vector2(wheelVelocity.x, wheelVelocity.z);

                if (wheelVelocity2D.y < slowCorrectionStart)
                {
                    //Debug.Log("Corrected");
                    float correctionImpact = (slowCorrectionStart - wheelVelocity2D.y) / (slowCorrectionStart - slowCorrectionForce);

                    float corrected = Mathf.Clamp(wheelVelocity2D.x, -3f, 3f);

                    if (float.IsNaN(slipVelocityLongitudinal))
                    {
                        Debug.LogError("Cant be");
                        //slipVelocityLongitudinal = slowCorrectionForce * 0.05f;
                    }

                    float correctedLongitudinal = slipVelocityLongitudinal / Mathf.Max(slowCorrectionForce * 0.05f, Mathf.Abs(wheelHubVelocityLongitudinal));

                    if (wheelVelocity2D.y < slowCorrectionForce)
                    {
                        slipAngle = corrected;
                        //slipVelocityLongitudinal = correctedLongitudinal;
                    }
                    else
                    {
                        slipAngle = Mathf.Lerp(slipAngle, corrected, correctionImpact);
                        //slipVelocityLongitudinal = Mathf.Lerp(slipVelocityLongitudinal, correctedLongitudinal, correctionImpact);
                    }
                }

                camberAngle = 0f;

                if (debugMessages) GraphManager.Graph.Plot("Longitude", Mathf.Abs(slipAngle), Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));
                if (true || rb.velocity.magnitude > 0f)
                {
                    fx = pacejka.CalcLongitudinalF(verticalTireLoad, slipLongitudinal * 100f);
                    fy = pacejka.CalcLateralF(verticalTireLoad, slipAngle, camberAngle);
                    //fx = pacejka.SimpleCalculateF(verticalTireLoad, slipLongitudinal * 100f);
                    //fy = pacejka.SimpleCalculateF(verticalTireLoad, slipAngle);
                }



                //if (debugMessages) GraphManager.Graph.Plot("Longitude2", slipLongitudinal, Color.green, new Rect(new Vector2(10f, 60f + 0f), new Vector2(1000f, 100f)));
                //if (debugMessages) GraphManager.Graph.Plot("Longitude242", slipAngle, Color.green, new Rect(new Vector2(10f, 60f + 110f), new Vector2(1000f, 100f)));
                //if (debugMessages) GraphManager.Graph.Plot("Longitude242dd", wheelVelocity.x, Color.green, new Rect(new Vector2(10f, 60f + 110f * 2f), new Vector2(1000f, 100f)));




                //Debug.Log("Fz: " + verticalTireLoad);

                // Clamping for max force of tire

                if (Mathf.Abs(fx) + Mathf.Abs(fy) > wheelMaxForce)
                {
                    float signFx = Mathf.Sign(fx);
                    float forceCurrent = Mathf.Abs(fx) + Mathf.Abs(fy);
                    fx = Mathf.Abs(fx) - (forceCurrent - wheelMaxForce);
                    if (fx < 0f)
                    {
                        fx = 0f;
                    }
                    fx = fx * signFx;
                    //fy = fy * (wheelMaxForce / forceCurrent);
                }

                //fx = ((new Vector2(fx, fy)).normalized * wheelMaxForce).x;
                //fy = ((new Vector2(fx, fy)).normalized * wheelMaxForce).y;



                suspensionForce = suspensionForceRay * transform.up;

                if (wentInOnce)
                {
                    float forceImpactNormal = Mathf.Cos(Vector3.Angle(normalGround, transform.up) * Mathf.PI / 180f);
                    forceImpactNormal = Mathf.Clamp(forceImpactNormal, 0f, 1f);

                    Vector3 force = forceImpactNormal * (suspensionForce + fx * transform.forward + fy * -transform.right);
                    if (float.IsNaN(force.x))
                    {
                        force.x = 0f;
                    }
                    if (float.IsNaN(force.y))
                    {
                        force.y = 0f;
                    }
                    if (float.IsNaN(force.z))
                    {
                        force.z = 0f;
                    }
                    rb.AddForceAtPosition(force, hit.point + (applyForceGround ? 0f : 1f) * transform.up * wheelRadius);
                }
                wentInOnce = true;

                springLengthOld = springLengthRay;
            }
        }
        else
        {
            //if (debugMessages) Debug.Log("In-Air calc");
            calculateNoHit(maxLength);
        }
        springVelocityOld = springVelocity;


        if (smoothStep)
        {
            RaycastHit frontHit;
            if (Physics.Raycast(new Ray(transform.position + transform.forward * (wheelRadius + 0.17f), -transform.up), out frontHit, maxLength + wheelRadius * 2f, LayerMask.GetMask("Map")))
            {
                if (oldWheelSphereDistance != -1f 
                    && Mathf.Abs(oldWheelSphereDistance - frontHit.distance) > wheelSphereJumpThreshold 
                    && Mathf.Abs(hit.distance - frontHit.distance) > wheelSphereJumpThreshold
                    && Vector3.Angle(transform.up, normalGround) < wheelSphereJumpMaxAngle
                    && groundedForTime >= wheelSphereJumpMinGroundTime
                    && (Vector3.Angle(normalGround, frontHit.normal) > wheelSphereJumpMinAngleNormals || (Mathf.Abs(Vector3.Dot(hit.point - frontHit.point, frontHit.normal) / frontHit.normal.magnitude) > wheelSphereJumpNormalDistanceMin)))
                {
                    Debug.Log("Set wheel sphere");

                    if (frontHit.distance > oldWheelSphereDistance)
                    {
                        wheelSphere.position = frontHit.point + transform.up * ((frontHit.distance - oldWheelSphereDistance) - wheelRadius);
                        wheelSphere.rotation = transform.rotation;
                    }
                    else
                    {
                        wheelSphere.position = frontHit.point - transform.up * wheelRadius;
                        wheelSphere.rotation = transform.rotation;
                    }
                }

                oldWheelSphereDistance = frontHit.distance;
            }
            else
            {
                /*if (oldWheelSphereDistance != -1f)
                {
                    wheelSphere.position = transform.position - transform.up * (hit.distance + wheelRadius);
                }*/

                oldWheelSphereDistance = -1f;
            }
        }


    }

    private void calculateNoHit(float clampMax)
    {
        wheelIsGrounded = false;
        wentInOnce = false;
        springLength = Mathf.Clamp(springLength, minLength, clampMax);
        springVelocity = (springLength - springLengthOld) / Time.fixedDeltaTime;
        //if (debugMessages) GraphManager.Graph.Plot("Longitude", springVelocityOld - springVelocity, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));
        /*if (Mathf.Abs(springVelocityOld - springVelocity) > maxWheelVelocityAcc)
        {
            if (debugMessages) Debug.Log("Capped");
            springVelocity = Mathf.MoveTowards(springVelocityOld, springVelocity, maxWheelVelocityAcc);
            springLength = springLengthOld - springVelocity * Time.fixedDeltaTime;
        }*/


        //if (debugMessages) GraphManager.Graph.Plot("Longitude", springVelocity, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));
        springVelocity = Mathf.Clamp(springVelocity, -maxWheelVelocity, maxWheelVelocity);


        antiRollSpringForce = antiRollStiffness * antiRollDifference;

        springForce = springStiffness * (restLength - springLength);
        damperForce = springDamping * -springVelocity;

        float forceSum = springForce + damperForce + antiRollSpringForce;

        float gImpact = Mathf.Cos(Vector3.Angle(new Vector3(0f, -1f, 0f), -transform.up) * Mathf.PI / 180f) * gAcc;

        float aWheel = (forceSum / wheelMass) + gImpact;

        float vYWheel = springVelocity + aWheel * Time.fixedDeltaTime;
        //float yWheel = 0.5f * aWheel * Time.fixedDeltaTime * Time.fixedDeltaTime + springVelocity * Time.fixedDeltaTime + springLength;

        springLength += vYWheel * Time.fixedDeltaTime;
        springLengthOld = springLength;
    }

    private void reInit()
    {
        minLength = restLength - springTravel;
        maxLength = restLength + springTravel;

        springLength = restLength;
        springLength = Mathf.Clamp(springLength, minLength, maxLength);
        springLengthOld = springLength;

        if (smoothStep)
        {
            GameObject instSphere;
            CapsuleCollider sc;
            if (wheelSphere == null)
            {
                instSphere = new GameObject("(Inst) Car Sphere");
                sc = instSphere.AddComponent<CapsuleCollider>();
                wheelSphere = instSphere.transform;
            }
            else
            {
                sc = wheelSphere.GetComponent<CapsuleCollider>();
            }
            sc.radius = 0.5f;
            sc.direction = 0;
            sc.height = 3f;
            wheelSphere.localScale = new Vector3(wheelRadius * 2f, wheelRadius * 2f, wheelRadius * 2f);

            wheelSphere.position = new Vector3(0f, -10000f, 0f);

        }
    }

    public float RestLength
    {
        get
        {
            return restLength;
        }
        set
        {
            restLength = value;
            reInit();
        }
    }

    public float SpringTravel
    {
        get
        {
            return springTravel;
        }
        set
        {
            springTravel = value;
            reInit();
        }
    }

    public float Radius
    {
        get
        {
            return wheelRadius;
        }
        set
        {
            wheelRadius = value;
            reInit();
        }
    }

    public float AngularVelocity
    {
        get
        {
            return angularVelocity;
        }
        set
        {
            if (debugMessages)
            {
                Debug.Log("was " + angularVelocity.ToString() + " is " + value.ToString());
            }
            angularVelocity = value;
        }
    }

    public float SpringLength
    {
        get
        {
            return springLength;
        }
    }

    public float SlipForward
    {
        get
        {
            return Mathf.Abs(slipVelocityLongitudinal);
        }
    }

    public float SlipForwardDifference
    {
        get
        {
            return slipVelocityLongitudinal;
        }
    }

    public float SlipSidewards
    {
        get
        {
            return Mathf.Abs(wheelVelocity.x);
        }
    }

    public float PacejkaSlipLater
    {
        get
        {
            return slipAngle;
        }
    }

    public float PacejkaSlipLong
    {
        get
        {
            return slipLongitudinal;
        }
    }

    public float SlipLongRaw
    {
        get
        {
            return slipLongitudinalRaw;
        }
    }

    public Vector3 GroundHit
    {
        get
        {
            return groundHit;
        }
    }

    public Vector3 GroundNormal
    {
        get
        {
            return normalGround;
        }
    }

    public bool IsGrounded
    {
        get
        {
            return wheelIsGrounded;
        }
    }

    public float WheelMass
    {
        get
        {
            return wheelMass;
        }
    }

    public float VerticalTireLoad
    {
        get
        {
            return verticalTireLoad;
        }
    }

    public float CamberAngle
    {
        get
        {
            return camberAngle;
        }
    }

    public float SteerAngle
    { get; set; }
}
