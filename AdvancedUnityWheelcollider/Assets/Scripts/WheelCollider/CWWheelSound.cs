﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CWWheelSound : MonoBehaviour
{
    [SerializeField]
    private AudioClip audioTireSqueal;
    [SerializeField]
    private float forwardSlipFactor = 1f;
    [SerializeField]
    private float sideSlipFactor = 1f;
    [SerializeField]
    private float slipThreshold = 1f;
    [SerializeField]
    private float slipFactor = 1f;

    private AudioSource audioSource;
    private WheelColliderAdv wheel;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        wheel = GetComponent<WheelColliderAdv>();
        audioSource.clip = audioTireSqueal;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (wheel.CWWheel.IsGrounded)
        {
            float slipSum = wheel.CWWheel.SlipForward * forwardSlipFactor + wheel.CWWheel.SlipSidewards * sideSlipFactor;

            if (slipSum > slipThreshold)
            {
                float skidAmount = (slipSum - slipThreshold) * slipFactor;

                skidAmount = Mathf.Clamp(skidAmount, 0f, 1f);
                audioSource.volume = skidAmount;
            }
            else
            {
                audioSource.volume = 0f;
            }
        }
        else
        {
            audioSource.volume = 0f;
        }
    }
}
