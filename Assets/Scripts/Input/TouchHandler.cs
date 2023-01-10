// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchHandler : MonoBehaviour, IMixedRealityTouchHandler
{
    void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        /*Debug.Log("Touch Completed");
        eventData.InputData.x;
        eventData.InputData.y;
        eventData.InputData.z;*/
    }

    void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
    {
        Debug.Log("Touch Started");
       // Debug.Log(eventData.InputData.x);
       // Debug.Log(eventData.InputData.y);
      //  Debug.Log(eventData.InputData.z);
    }

    void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        //Debug.Log("Touch Updated");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
