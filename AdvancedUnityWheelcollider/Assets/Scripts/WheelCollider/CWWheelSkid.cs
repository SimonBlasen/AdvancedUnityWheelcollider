using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CWWheelSkid : MonoBehaviour
{
    [SerializeField]
    private float forwardSlipFactor = 1f;
    [SerializeField]
    private float sideSlipFactor = 1f;
    [SerializeField]
    private float slipThreshold = 1f;
    [SerializeField]
    private float slipFactor = 1f;
    [SerializeField]
    private Material skidMaterial;

    [HideInInspector]
    public Skidmarks skidmarksController = null;

    private int lastIndex = -1;
    private CWWheel wheel;

    // Start is called before the first frame update
    void Start()
    {
        GameObject skidObj = new GameObject("Skidmarks");
        skidmarksController = skidObj.AddComponent<Skidmarks>();
        skidmarksController.skidmarksMaterial = skidMaterial;

        if (skidmarksController == null)
        {
            Debug.LogError("No Skidmarks found in scene");
        }

        wheel = GetComponent<CWWheel>();
    }

    // Update is called once per frame
    void Update()
    {
        if (skidmarksController != null && wheel.IsGrounded)
        {
            float slipSum = wheel.SlipForward * forwardSlipFactor + wheel.SlipSidewards * sideSlipFactor;

            if (slipSum > slipThreshold)
            {
                float skidAmount = (slipSum - slipThreshold) * slipFactor;

                lastIndex = skidmarksController.AddSkidMark(wheel.GroundHit, wheel.GroundNormal, skidAmount, lastIndex);
            }
            else
            {
                lastIndex = -1;
            }
        }
        else
        {
            lastIndex = -1;
        }
    }
}
