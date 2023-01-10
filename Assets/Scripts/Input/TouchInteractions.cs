// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;

public class TouchInteractions : MonoBehaviour
{
    public List<TouchGestures> gestures;

    InteractionHandler interactionHandler;
    InputHandler inputHandler;    
    Paintable canvas;

    GameObject panel;

    private float rightBorder;
    private float lowerBorder;

    Vector3 upperRightButton;

    public enum TouchGestures
    {
        Tap,
        Hold,
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown,
        Rotate,
        Scale
    }

    void Start()
    {
        panel = GameObject.Find("Panel");        
        interactionHandler = panel.GetComponent<InteractionHandler>() as InteractionHandler;
        inputHandler = panel.GetComponent<InputHandler>() as InputHandler;
        canvas = panel.GetComponent<Paintable>() as Paintable;

        gestures = new List<TouchGestures>();

        // Determine width of lower and right interaction stripes
        rightBorder = inputHandler.screenWidth - (inputHandler.screenWidth / 10);
        lowerBorder = inputHandler.screenHeight / 10;

        // ca 3cm x 3cm
        upperRightButton = new Vector3(inputHandler.screenWidth - 110, inputHandler.screenHeight - 110);


    }

    public void SetBorders(float width, float height) {
        rightBorder = width - (width / 10);
        lowerBorder = height / 10;
    }
    /// <summary>
    /// Method <c>Tap</c> defines the interactions to be executed when performing a "tap" gesture on the panel, depending on the mode of the panel.
    /// Tap can initiate: Creating and deleting a bookmark during slicing, Releasing the volume during PickUp
    /// </summary>
    public void Tap(TouchEvent touchEvent)
    {
        gestures.Add(TouchGestures.Tap);

        if (inputHandler.Mode == InputHandler.Modi.Slice)
        {
            if (!interactionHandler.DeleteBookMark())
            {
                interactionHandler.CreateBookMark();
            }
        }
        if (inputHandler.Mode == InputHandler.Modi.PickUp)
        {
            interactionHandler.Release();
        }              
    }
    /// <summary>
    /// Method <c>Hold</c> defines the interactions to be executed when performing a "hold" gesture on the panel, depending on the mode of the panel.
    /// Hold can initiate: Releasing annotation (if combined with SwipeUp in Annotation mode), Clearing the scene from annotations (if combined with SwipeLeft in Annotation mode),
    /// Freezing and Unfreezing the slicing plane in slice mode.
    /// </summary>
    public void Hold(TouchEvent touchEvent)
    {
        if (inputHandler.Mode == InputHandler.Modi.DrawOnTablet || inputHandler.Mode == InputHandler.Modi.InSitu)
        {
            if (gestures.LastOrDefault() == TouchGestures.SwipeUp && touchEvent.Touch.position.x > rightBorder)
            {
                // undo lines for both gestures

                interactionHandler.UndoLastStroke();
                Debug.Log("delete");
                interactionHandler.UndoLastStroke();
                Debug.Log("delete");
                //canvas.Undo();
                interactionHandler.ReleaseAnnotation();
            }
            if (gestures.LastOrDefault() == TouchGestures.SwipeLeft && touchEvent.Touch.position.y < lowerBorder)
            {
                interactionHandler.ClearStrokes();
            } 
        }

        gestures.Add(TouchGestures.Hold);

        if (inputHandler.Mode == InputHandler.Modi.Slice)
        {
            SlicingPanel slicingPlane = GameObject.Find("SlicingPlane(Clone)").GetComponent<SlicingPanel>() as SlicingPanel;
            if (!slicingPlane.freeze)
            {
                Debug.Log("Freezing Slicing Plane ... ");
                interactionHandler.FreezeSlicingPlane();
            }
            else
            {
                interactionHandler.UnfreezeSlicingPlane();
            }
        }
    }

    /// <summary>
    /// Method <c>Move</c> defines the interactions to be executed when performing a dragging gesture on the panels interaction stripes, depending on the mode of the panel.
    /// Additionally, it identifies the directions of the swipe gestures on the interactions stripes
    /// </summary>
    public void Move(TouchEvent touchEvent)
    {
        TouchState first = touchEvent.History.First();
        TouchState last = touchEvent.History.Last();

        if (inputHandler.Mode == InputHandler.Modi.DrawOnTablet || inputHandler.Mode == InputHandler.Modi.InSitu) 
        {
            // Swipe left on lower interaction stripe
            if (first.position.x > last.position.x)
            {                
                if (first.position.y < lowerBorder && last.position.y < lowerBorder)
                {
                    Debug.Log("Swipe Left on Lower Stripe! ");

                    // Undo Twice to also undo the line created by the swiping motion
                    interactionHandler.UndoLastStroke();
                    Debug.Log("delete");
                    interactionHandler.UndoLastStroke();
                    Debug.Log("delete");
                    gestures.Add(TouchGestures.SwipeLeft);
                }
            }

            // Swipe Up on right interaction stripe
            if (first.position.y < last.position.y)
            {
                
                if (first.position.x > rightBorder && last.position.x > rightBorder)
                {
                    Debug.Log("Swipe Up on Right Stripe! ");

                    // Double Swipe Up
                    if (gestures.LastOrDefault() == TouchGestures.SwipeUp && inputHandler.Mode == InputHandler.Modi.DrawOnTablet)
                    {
                        Debug.Log("Double Swipe Up! In-Situ activated! ");
                        interactionHandler.UndoLastStroke();
                        interactionHandler.UndoLastStroke();
                        //canvas.Undo();

                        interactionHandler.ActivateInsituAnnotations();
                    }
                    if (inputHandler.Mode == InputHandler.Modi.Slice)
                    {
                        Debug.Log("Swipe while Slicing, showing CT Scan");
                        interactionHandler.ShowCTScan();
                    }

                    gestures.Add(TouchGestures.SwipeUp);
                }
            }
            // Swipe down on right interaction stripe
            else
            {             
                           
                if (first.position.x > rightBorder && last.position.x > rightBorder)
                {
                    Debug.Log("Swipe Down on Right Stripe! ");

                    // Double Swipe Down
                    if (gestures.LastOrDefault() == TouchGestures.SwipeDown && inputHandler.Mode == InputHandler.Modi.InSitu)
                    {
                        Debug.Log("Double Swipe Down! In-Situ Deactivated ");

                        interactionHandler.UndoLastStroke();
                        interactionHandler.UndoLastStroke();
                        //canvas.Undo();

                        interactionHandler.DeactivateInsituAnnotations();
                    }

                    gestures.Add(TouchGestures.SwipeDown);
                }
            }
        }
        if (inputHandler.Mode == InputHandler.Modi.Slice)
        {
            if (first.position.y < last.position.y)
            {

                if (first.position.x > rightBorder && last.position.x > rightBorder)
                {
                    Debug.Log("Swipe Up on Right Stripe! ");

                    // Double Swipe Up                   
                    if (inputHandler.Mode == InputHandler.Modi.Slice)
                    {
                        Debug.Log("Swipe while Slicing, showing CT Scan");
                        interactionHandler.ShowCTScan();
                    }

                    gestures.Add(TouchGestures.SwipeUp);
                }
            }
        }
    }
}
