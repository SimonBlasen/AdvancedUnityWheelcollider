using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelColliderAdv : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    public Rigidbody carRigidbody;
    [SerializeField]
    public Transform wheelMesh;
    [SerializeField]
    private CWPacejka customPacejka;

    [Space]

    [Header("Suspension")]
    [SerializeField]
    private float restLength = 0.5f;
    [SerializeField]
    private float springTravel = 0.5f;
    [SerializeField]
    private float springStiffness = 50000f;
    [SerializeField]
    private float springDamping = 4000f;

    [Space]

    [Header("Wheel")]
    [SerializeField]
    private float wheelRadius = 0.3f;
    [SerializeField]
    private float wheelMass = 15f;


    [Space]

    [Header("Control Options")]

    //[SerializeField]
    //private CWPacejka pacejka;
    [SerializeField]
    private bool ABS = false;
    [SerializeField]
    private bool TC = false;
    [SerializeField]
    private bool hasMotor = false;

    public bool debugMessages = false;



    private CWWheel cwWheel;
    private CWWheelTorqueDistr cwWheelTorqueDistr;

    public CWWheel CWWheel
    {
        get
        {
            return cwWheel;
        }
    }

    public CWWheelTorqueDistr CWWheelTorqueDistr
    {
        get
        {
            return cwWheelTorqueDistr;
        }
    }




    // Start is called before the first frame update
    void Start()
    {
        if (customPacejka == null)
        {
            Debug.LogWarning("You need to assign a Pacejka. Using a default one");

            GameObject pacObj = new GameObject("Pacejka");
            pacObj.AddComponent<CWPacejka>();
            customPacejka = pacObj.GetComponent<CWPacejka>();
        }

        if (carRigidbody == null)
        {
            Debug.LogWarning("No Rigidbody was assigned. Chose one in parent");

            carRigidbody = GetComponentInParent<Rigidbody>();
            if (carRigidbody == null)
            {
                Debug.LogError("No Rigidbody was found. You need a rigidbody in one of the parent objects");
            }
        }

        if (wheelMesh == null)
        {
            //Debug.LogWarning("No wheel mesh was found");
        }


        cwWheel = new CWWheel(this, transform, carRigidbody, customPacejka, wheelMesh, restLength, springTravel, springStiffness, springDamping, wheelRadius, wheelMass);
        cwWheelTorqueDistr = new CWWheelTorqueDistr(this, cwWheel, carRigidbody, customPacejka, hasMotor);
        cwWheel.torqueDistr = cwWheelTorqueDistr;

        cwWheelTorqueDistr.debugMessages = debugMessages;

    }

    private void FixedUpdate()
    {
        cwWheelTorqueDistr.FixedUpdate();
        cwWheel.FixedUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        cwWheelTorqueDistr.Update();
        cwWheel.Update();
    }



    public bool TractionControl
    {
        get
        {
            return TC;
        }
        set
        {
            TC = value;
        }
    }
    public bool AntiBlockSystem
    {
        get
        {
            return ABS;
        }
        set
        {
            ABS = value;
        }
    }

    public float SteerAngle
    {
        get
        {
            return cwWheel.SteerAngle;
        }
        set
        {
            cwWheel.SteerAngle = value;
        }
    }
}
