// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindowMode : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject panel;
    public GameObject observedObject;
    InputHandler inputHandler;
    InputHandler.Modi mode;
    bool windowSwitch = false;
   // bool isactive = false;
    void Start()
    {
        panel = GameObject.Find("Panel");
        inputHandler = panel.GetComponent<InputHandler>() as InputHandler;
        mode = inputHandler.Mode;
    }
    /// <summary>
    /// In this <c>Update</c> function a ray is shot from the camera into the scene
    /// Depending on how many and which objects the ray passes through, window mode is activated or not.
    void Update()
    {
        if (inputHandler.Mode == InputHandler.Modi.DrawOnTablet || inputHandler.Mode == InputHandler.Modi.Window) { 
            RaycastHit[] rayHits;
            rayHits = Physics.RaycastAll(transform.position, transform.forward, 100.0f, ~(1<<31));

            if(rayHits.Length > 1)
            {
                Array.Sort(rayHits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
                // First object to be hit must be panel, and second object must be Interactable object
                if (rayHits[0].transform.gameObject.GetComponent<InputHandler>() && rayHits[1].transform.gameObject.GetComponent<ModelInteractable>())
                {
                        activateWindowMode(rayHits[1]);
                }
                else
                {
                        //Debug.Log("Hit1:"+ rayHits[0].transform.gameObject.name+", Hit2:" + rayHits[1].transform.gameObject.name);
                        deactivateWindowMode();
                }
            }
            else 
            {
                    deactivateWindowMode();
            }           
        }

    }

    void activateWindowMode(RaycastHit hit)
    {
        if (!windowSwitch)
        {
            mode = inputHandler.Mode;
            Debug.Log("Window Mode activated");
        }

        inputHandler.Mode = InputHandler.Modi.Window;

        observedObject = hit.transform.gameObject;
        panel.GetComponent<InteractionHandler>().model = observedObject;
        windowSwitch = true;
    }

    void deactivateWindowMode()
    {
        if (windowSwitch)
        {
            panel.GetComponent<InteractionHandler>().model = null;
            inputHandler.Mode = mode;
            windowSwitch = false;
            Debug.Log("Window Mode deactivated");
        }
    }
}
