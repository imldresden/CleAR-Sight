// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactARInteractionManager : MonoBehaviour, ITouchable
{
    InputHandler inputHandler;
    InteractionHandler interactionHandler;
    public Material penMat;
    public Material cursorMat;
    bool isSelectionMode = false;
    bool isDrawingMode = true;
    public GameObject panel;
    public Collider Collider => throw new System.NotImplementedException();

    void Start()
    {
        Debug.Log("Test, wann startet das hier?");
        interactionHandler = GameObject.Find("Panel").GetComponent<InteractionHandler>();
        inputHandler = GameObject.Find("Panel").GetComponent<InputHandler>();
    }

    public void OnDoubleTap(TouchEvent touch)
    {
        Debug.Log("Button double tapped");
       // throw new System.NotImplementedException();
    }

    public void OnHold(TouchEvent touch)
    {
        Debug.Log("Button hold");
        // throw new System.NotImplementedException();
    }

    public void OnTap(TouchEvent touchEvent)
    {
        Debug.Log("Tap on Panel Button");
        if (inputHandler.Mode == InputHandler.Modi.Select)
        {
            // Drawing Mode
            //inputHandler.Mode = InputHandler.Modi.InSitu;
            interactionHandler.ActivateInsituAnnotations();
            this.gameObject.GetComponent<MeshRenderer>().material = cursorMat;            
            isDrawingMode = true;
            isSelectionMode = false;
            Debug.Log("Drawing mode enabled!");
        }
        else if (inputHandler.Mode == InputHandler.Modi.InSitu && interactionHandler.contactAR)
        {
            // Selection Mode
            interactionHandler.DeactivateInsituAnnotations();
            inputHandler.Mode = InputHandler.Modi.Select;
            this.gameObject.GetComponent<MeshRenderer>().material = penMat;
            isSelectionMode = true;
            isDrawingMode = false;          
            Debug.Log("Selection mode enabled!");
        }

       // touchEvent.Consume();
    }

    public void OnTouchDown(TouchEvent touch)
    {
       Debug.Log("Touch down!");
      //  touch.Capture(this);
       // touch.Consume();
    }

    public void OnTouchMove(TouchEvent touch)
    {
        Debug.Log("Button move");
        //throw new System.NotImplementedException();
    }

    public void OnTouchUp(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnEnter()
    {
        inputHandler.Mode = InputHandler.Modi.Select;
        this.gameObject.GetComponent<MeshRenderer>().material = penMat;
        isSelectionMode = true;
        isDrawingMode = false;
    }

    // Update is called once per frame
    void Update()
    {
       // Debug.Log(panel.GetComponent<InputHandler>().Mode);
    }
}
