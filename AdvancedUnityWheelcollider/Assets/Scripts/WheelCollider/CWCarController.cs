using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CWCarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private WheelColliderAdv[] frontWheels;
    [SerializeField]
    private Transform com;
    [SerializeField]
    private TextMeshProUGUI textSpeed;

    [Header("Car Specs")]
    [SerializeField]
    private float wheelBase;
    [SerializeField]
    private float rearTrack;
    [SerializeField]
    private float turnRadius;

    [Header("Inputs")]
    [SerializeField]
    private float steerInput;

    private float ackermannAngleLeft;
    private float ackermannAngleRight;

    void Start()
    {
    }

    void Update()
    {
        if (textSpeed != null)
        {
            textSpeed.text = (GetComponent<Rigidbody>().velocity.magnitude * 3.6f).ToString();
        }
        GetComponent<Rigidbody>().centerOfMass = com.localPosition;

        //Debug.Log(GetComponent<Rigidbody>().velocity.magnitude);

        steerInput = Input.GetAxis("Horizontal");

        if (steerInput > 0f) // Turning right
        {
            ackermannAngleLeft = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + (rearTrack / 2f))) * steerInput;
            ackermannAngleRight = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - (rearTrack / 2f))) * steerInput;
        }
        else if (steerInput < 0f) // Turning Left
        {
            ackermannAngleLeft = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius  - (rearTrack / 2f))) * steerInput;
            ackermannAngleRight = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + (rearTrack / 2f))) * steerInput;
        }
        else
        {
            ackermannAngleLeft = 0f;
            ackermannAngleRight = 0f;
        }

        frontWheels[0].SteerAngle = ackermannAngleLeft;
        frontWheels[1].SteerAngle = ackermannAngleRight;
    }
}
