﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CWWheelTorqueDistr : MonoBehaviour
{
    [SerializeField]
    private CWPacejka pacejka;
    [SerializeField]
    private bool ABS;
    [SerializeField]
    private float ABS_minAngVel = 13f;
    [SerializeField]
    private bool TC;
    [SerializeField]
    private float TC_minAngVel = 1f;
    [SerializeField]
    private float TC_minVel = 1f;

    private CWWheel wheel;
    private Rigidbody rb;

    public bool debugMessages = false;

    private float currentPower = 0f;
    private float currentBrake = 0f;
    private float slipLong = 0f;
    private float wheelMass = 0f;
    private float wheelRadius = 0f;
    private float J = 0f;
    private float W = 0f;
    private float E = 0f;
    private float phi = 0f;
    private float thetaDelta = 0f;
    private float fWheel = 0f;
    private float optimalForwardSlip = 0f;
    private float optimalBackwardSlip = 0f;

    public bool hasMotor = false;
    private float lastAcc = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (pacejka == null)
        {
            pacejka = GameObject.FindObjectOfType<CWPacejka>();
        }
        wheel = GetComponent<CWWheel>();
        rb = GetComponentInParent<Rigidbody>();

        wheelMass = wheel.WheelMass;
        wheelRadius = wheel.Radius;
        J = wheelMass * wheelRadius * wheelRadius * 0.5f;
    }

    public float angVelThreshStop = 0.1f;
    public float slipForwThreshStop = 0.1f;
    public float slowdownFac = 0.01f;
    public float slowdownFacDamper = 100f;
    private void FixedUpdate()
    {
        float currentSlip = wheel.PacejkaSlipLong * 100f;
        //if (debugMessages) GraphManager.Graph.Plot("Longitude", currentSlip, Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));
        pacejka_C = pacejka.b0;
        pacejka_D = wheel.VerticalTireLoad * (pacejka.b1 * wheel.VerticalTireLoad + pacejka.b2);
        pacejka_BCD = (pacejka.b3 * wheel.VerticalTireLoad * wheel.VerticalTireLoad + pacejka.b4 * wheel.VerticalTireLoad) * Mathf.Exp(-pacejka.b5 * wheel.VerticalTireLoad);
        pacejka_H = pacejka.b9 * wheel.VerticalTireLoad + pacejka.b10;
        pacejka_B = pacejka_BCD / (pacejka_C * pacejka_D);
        pacejka_E = (pacejka.b6 * wheel.VerticalTireLoad * wheel.VerticalTireLoad + pacejka.b7 * wheel.VerticalTireLoad + pacejka.b8) * (1f - pacejka.b13 * Mathf.Sign(currentSlip + pacejka_H));


        float maxX = pacejka_BCD / pacejka_C;
        if (currentSlip > 0f)
        {
            optimalForwardSlip = getMax(maxX, 2000f, 20);
            //Debug.Log("Max is at: " + optimalForwardSlip);
        }
        else if (currentSlip < 0f)
        {
            optimalBackwardSlip = getMin(-maxX, 2000f, 20);
            //if (debugMessages) Debug.Log("Min is at: " + optimalBackwardSlip);
            //if (debugMessages) Debug.Log("slip is: " + currentSlip);
        }




        slipLong = wheel.SlipLongRaw;
        if (float.IsInfinity(slipLong) || float.IsNaN(slipLong))
        {
            slipLong = 100000f;
        }
        else if(float.IsNegativeInfinity(slipLong))
        {
            slipLong = -100000f;
        }
        slipLong = Mathf.Clamp(slipLong, -20f, 20f);
        
        if (currentBrake != 0f)
        {
            E = 0.5f * J * wheel.angularVelocity * wheel.angularVelocity;


            W = Mathf.Abs(currentBrake) * wheelRadius * 1f;

            thetaDelta = Mathf.Sqrt(2f * (W / J));

            //if (debugMessages) Debug.Log("Braking: " + thetaDelta.ToString());
            //if (debugMessages) GraphManager.Graph.Plot("Longitude2", currentBrake, Color.green, new Rect(new Vector2(10f, 60f + 0f), new Vector2(1000f, 100f)));


            if (ABS == false || currentSlip >= 0f || wheel.AngularVelocity <= ABS_minAngVel || (currentSlip < 0f && currentSlip > optimalBackwardSlip))
            {
                if (Mathf.Abs(wheel.AngularVelocity) >= Mathf.Abs(thetaDelta))
                {
                    wheel.AngularVelocity -= thetaDelta * Mathf.Sign(wheel.AngularVelocity);
                    if (debugMessages) Debug.Log("Braking: " + wheel.AngularVelocity.ToString());
                }
                else
                {
                    wheel.AngularVelocity = 0f;
                }
            }
            else
            {
                calculateNoPower();
            }


        }
        else
        {
            if (float.IsNaN(slipLong) == false)
            {
                E = 0.5f * J * wheel.angularVelocity * wheel.angularVelocity;
                phi = wheel.AngularVelocity * Time.fixedDeltaTime;

                if (currentPower != 0f)
                {
                    W = Mathf.Abs(currentPower) * wheelRadius * 1f;

                    thetaDelta = Mathf.Sqrt(2f * (W / J));

                    if (TC == false || wheel.AngularVelocity < TC_minAngVel || rb.velocity.magnitude < TC_minVel || (currentSlip > 0f && currentSlip < optimalForwardSlip))
                    {
                        wheel.AngularVelocity += thetaDelta * Mathf.Sign(currentPower);
                    }
                    else
                    {
                        calculateNoPower();
                    }
                }
                else if (currentPower == 0f)
                {
                    calculateNoPower();


                }
            }
        }


        

    }

    private float oldWheelHubVel = 0f;

    private void calculateNoPower()
    {
        //if (debugMessages) GraphManager.Graph.Plot("Longitude", Mathf.Abs(slipLong), Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));
        if (false && Mathf.Abs(wheel.AngularVelocity) < angVelThreshStop && Mathf.Abs(slipLong) < slipForwThreshStop)
        {
            float newAngVel = wheel.AngularVelocity - Mathf.Sign(wheel.AngularVelocity) * Time.fixedDeltaTime * 1f;
            if (Mathf.Sign(newAngVel) != Mathf.Sign(wheel.AngularVelocity))
            {
                newAngVel = 0f;
            }

            wheel.AngularVelocity = newAngVel;
            if (debugMessages) Debug.Log("No power");
        }
        else
        {
            fWheel = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, wheel.PacejkaSlipLong);

            float slipDiff = wheel.SlipForwardDifference;
            //if (debugMessages) GraphManager.Graph.Plot("Longitude", slipDiff, Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));

            //if (float.IsNaN(fWheel) == false)
            //{
            //if (debugMessages) Debug.Log("slip: " + wheel.SlipForward * Mathf.Sign(wheel.PacejkaSlipLong));




            float slowdownSlowdoanFac = 1f;
            if (wheel.LongitudinalHubVelocity < 7f)
            {
                slowdownSlowdoanFac = 7f - wheel.LongitudinalHubVelocity;
            }
            float manipulatedSlowdown = slowdownFac * slowdownSlowdoanFac;
            if (manipulatedSlowdown < 400f)
            {
                manipulatedSlowdown = 400f;
            }




            manipulatedSlowdown = slowdownFac;
            float pOut = (0f - slipDiff) * manipulatedSlowdown;

            float wheelAngDamper = (oldWheelHubVel - wheel.AngularVelocity) * slowdownFacDamper;



            pOut += wheelAngDamper;


            /*if (float.IsNaN(pOut) || float.IsInfinity(pOut) || float.IsNegativeInfinity(pOut)
                || Mathf.Abs(pOut) > 3000f)
            {
                Debug.LogError("Cor");
                wheel.AngularVelocity += lastAcc * Time.fixedDeltaTime;
                if (float.IsNaN(pOut) == false && float.IsInfinity(pOut) == false && float.IsNegativeInfinity(pOut) == false)
                {
                    lastAcc = Mathf.Clamp(pOut, -3000f, 3000f);
                }
            }
            else
            {*/


            float wheelAcc = (pOut) / wheelMass;

            oldWheelHubVel = wheel.AngularVelocity;
            wheel.AngularVelocity += wheelAcc * Time.fixedDeltaTime;
            //if (debugMessages) GraphManager.Graph.Plot("Longitude", wheel.AngularVelocity, Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));

            if (debugMessages) Debug.Log("No power");

            lastAcc = wheelAcc;
                //}


                //wheel.AngularVelocity += fWheel * slowdownFac;

                /*W = fWheel * wheelRadius * phi * slowdownFac;

                float newE = E - W;// * Mathf.Sign(slipLong);
                if (newE < 0f)
                {
                    newE = 0f;
                }

                float newAngVel = Mathf.Sqrt((newE * 2f) / J) * Mathf.Sign(wheel.angularVelocity);

                //thetaDelta = Mathf.Sqrt(2f * (W / J));

                wheel.AngularVelocity = newAngVel;*/
            //}
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hasMotor)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                ApplyPower(0.05f * 3f * 0.05f);
            }
            else
            {
                ApplyPower(0f);
            }
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            //ApplyPower(-0.05f);
            Brake(0.2f);
        }
        else
        {
            Brake(0f);
        }

    }


    float pacejka_C;
    float pacejka_D;
    float pacejka_BCD;
    float pacejka_H;
    float pacejka_B;
    float pacejka_E;

    public void Brake(float brakePower)
    {
        currentBrake = brakePower;
    }

    public void ApplyPower(float availablePower)
    {

        currentPower = availablePower;



        //GraphManager.Graph.Plot("B", currentSlip, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));

        //GraphManager.Graph.Plot("B", pacejka_B, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(1000f, 100f)));
        //GraphManager.Graph.Plot("C", pacejka_C, Color.green, new Rect(new Vector2(10f, 60f + 110f), new Vector2(1000f, 100f)));
        //GraphManager.Graph.Plot("D", pacejka_D, Color.green, new Rect(new Vector2(10f, 60f + 110f * 2f), new Vector2(1000f, 100f)));
        //GraphManager.Graph.Plot("E", pacejka_E, Color.green, new Rect(new Vector2(10f, 60f + 110f * 3f), new Vector2(1000f, 100f)));

        //Debug.Log("B: " + pacejka_B);
        //Debug.Log("C: " + pacejka_C);
        //Debug.Log("D: " + pacejka_D);
        //Debug.Log("E: " + pacejka_E);

        //float slipMaxForward = pacejka_H + (1f / pacejka_B * Mathf.Sqrt(pacejka_E - 1f));
        //float slipMaxBackwards = pacejka_H - (1f / pacejka_B * Mathf.Sqrt(pacejka_E - 1f));
        //Debug.Log("Slip: " + currentSlip + " Max: " + slipMaxForward);

    }

    private float getMin(float x, float delta, int steps)
    {
        if (steps <= 0)
        {
            return x;
        }

        float here = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x);
        float left = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x - delta);
        float right = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x + delta);

        if (here < left && here < right)
        {
            return getMin(x, delta * 0.5f, steps - 1);
        }
        else if (left < here && left < right)
        {
            return getMin(x - delta, delta * 0.5f, steps - 1);
        }
        else
        {
            return getMin(x + delta, delta * 0.5f, steps - 1);
        }
    }

    private float getMax(float x, float delta, int steps)
    {
        if (steps <= 0)
        {
            return x;
        }

        float here = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x);
        float left = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x - delta);
        float right = pacejka.CalcLongitudinalF(wheel.VerticalTireLoad, x + delta);
        
        if (here > left && here > right)
        {
            return getMax(x, delta * 0.5f, steps - 1);
        }
        else if (left > here && left > right)
        {
            return getMax(x - delta, delta * 0.5f, steps - 1);
        }
        else
        {
            return getMax(x + delta, delta * 0.5f, steps - 1);
        }
    }
}
