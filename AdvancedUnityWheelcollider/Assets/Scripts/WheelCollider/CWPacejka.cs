using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CWPacejka : MonoBehaviour
{
    //const float D3_EPSILON = 0.00001f;

    public float cFz = 1.6f;
    public float camberAngle = 0f;
    public float sampleStep = 0.5f;

    public float autoY = 1f;
    public float autoB2 = 1f;

    public float a0 = 1.5f, a1 = -22f, a2 = 1011f, a3 = 1078f, a4 = 1.82f, a5 = 0.208f, a6 = 0f, a7 = -0.3f, a8, a9, a10, a11, a112, a12, a13, a14, a15, a16, a17 = 0.5f;
    public float b0 = 1.65f, b1 = -21f, b2 = 1144f, b3 = 49f, b4 = 226f, b5 = -0.1f, b6, b7 = 0.1f, b8 = -5f, b9, b10, b11, b12, b13 = 0f;
    public float c0, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16, c17 = 0f;

    float camber = 0f;
    float sideSlip = 0f;
    float slipPercentage = 0f;
    float Fz = 0f;

    public float S_B;
    public float S_C;
    public float S_D;
    public float S_E;

    //float Fx, Fy, Mz = 0f;
    //float longStiffness, latStiffness = 0f;
    //float maxForce = 0f;

    public float SimpleCalculateF(float _fz, float _slip)
    {
        float F = _fz * 1000f * S_D * Mathf.Sin(S_C * Mathf.Atan(S_B * _slip - S_E * (S_B * _slip - Mathf.Atan(S_B * _slip))));
        return F;
    }

    public float CalcLateralF(float _fz, float _slip, float _camber)
    {
        //Inputs

        // Vertical force in kN
        float Fz = _fz;
        // Slip angle in degrees
        float slip = _slip;
        // Camber angle in degrees
        float y = _camber;

        float C = a0;
        float D = Fz * (a1 * Fz + a2) * (1f - a15 * y * y);
        float BCD = a3 * Mathf.Sin(Mathf.Atan(Fz / a4) * 2f) * (1f - a5 * Mathf.Abs(y));
        float B = BCD / (C * D);
        float H = a8 * Fz + a9 + a10 * y;
        float E = (a6 * Fz + a7) * (1f - (a16 * y + a17) * Mathf.Sign(slip + H));
        float V = a11 * Fz + a12 + (a13 * Fz + a14) * y * Fz;
        float Bx1 = B * (slip + H);

        /*
        D = 1f;
        C = 1.9f;
        Bx1 = 10f;
        E = 0.97f;*/


        float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;
        return F; // In N
    }

    public float CalcLongitudinalF(float _fz, float _slip)
    {
        //Inputs

        // Vertical force in kN
        float Fz = _fz;
        // Slip ration in percentage
        float slip = _slip;

        float C = b0;
        float D = Fz * (b1 * Fz + (b2 * autoB2));
        float BCD = (b3 * Fz * Fz + b4 * Fz) * Mathf.Exp(-b5 * Fz);
        float B = BCD / (C * D);
        float H = b9 * Fz + b10;
        float E = (b6 * Fz * Fz + b7 * Fz + b8) * (1f - b13 * Mathf.Sign(slip + H));
        float V = b11 * Fz + b12;
        float Bx1 = B * (slip + H);



        float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;
        return F * autoY; // In N
    }

    float cnt = 0f;

    public float scaleX = 400f;

    private void Update()
    {
        cnt += Time.deltaTime;
        
        /*if (cnt >= 0.2f ||Input.GetKeyDown(KeyCode.R))
        {
            cnt = 0f;
            //Debug.Log(CalcLongitudinalF(cFz, sampleStep));

            float xMin = -100f;
            float xMax = 100f;
            float range = xMax - xMin;
            float x = xMin;
            sampleStep = range / scaleX;


            while (x < xMax)
            {
                GraphManager.Graph.Plot("Longitude", CalcLongitudinalF(cFz, x), Color.green, new Rect(new Vector2(10f, 130f), new Vector2(1000f, 250f)));
                GraphManager.Graph.Plot("Lateral", CalcLateralF(cFz, x * 10f / 100f, 0f), Color.green, new Rect(new Vector2(10f, 130f + 250f), new Vector2(1000f, 250f)));
                //GraphManager.Graph.Plot("Longitude", x, Color.green, new Rect(new Vector2(10f, 60f), new Vector2(500f, 100f)));
                //GraphManager.Graph.Plot("Lateral", x, Color.green, new Rect(new Vector2(10f, 60f + 110f), new Vector2(500f, 100f)));

                x += sampleStep;
            }
        }*/
    }
}
