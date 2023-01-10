// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if ENABLE_WINMD_SUPPORT
//using Neosmartpen.Net;
//using Neosmartpen.Net.Bluetooth;
using System.Threading.Tasks;
//using System.Diagnostics;
#endif
using IMLD.Unity.Core;
using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using IMLD.Unity.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputHandler : MonoBehaviour
{        
    public float screenWidth;
    public float screenHeight;

    /// <summary>
    /// Determines if received touch events should be printed to Unity's debug output.
    /// </summary>
    [Tooltip("Determines if touch events should be printed to Unity's debug output.")]
    public bool PrintDebugOutput = false;

    [HideInInspector]
    public bool scaling = false;

    // Do these have to be public Variables? Could also go with this.transform.getComponent ...
    public Paintable canvas;
    public InteractionHandler interactionHandler;
    public TouchInteractions touchInteractions;

    /// <summary>
    /// The threshold in seconds after which a tap becomes a hold instead.
    /// </summary>
    private const float TAP_TIME_THRESHOLD = 1.0f;

    /// <summary>
    /// The threshold for the distance in pixels on the device over which a tap becomes a series of moves.
    /// </summary>
    private const float TAP_DISTANCE_THRESHOLD = 5.0f;

    /// <summary>
    /// The threshold of the allowed distance in pixels on the device between two taps to be considered a double tap.
    /// </summary>
    private const float DOUBLE_TAP_DISTANCE_THRESHOLD = 50.0f;

    /// <summary>
    /// The threshold in seconds after which two taps are no longer considered a double tap.
    /// </summary>
    private const float DOUBLE_TAP_TIME_THRESHOLD = 0.75f;

    private float panelScaleX;
    private float panelScaleZ;

    private static int firstCount;
    private int tapCount;
    private static int holdCount;
    private static int moveCount;

    private bool activeTouch = false;

    private float timer = 0;

    private Dictionary<int, ITouchable> capturedTouchEvents = new Dictionary<int, ITouchable>();
    private Dictionary<int, TouchEvent> aliveTouchEvents = new Dictionary<int, TouchEvent>();
    private List<TouchEvent> deadTouchEvents = new List<TouchEvent>();
    private List<TouchEvent> recentTaps = new List<TouchEvent>();


    //public List<TouchState> currentTouchInput = new List<TouchState>();    

    public enum Modi
    {
        Draw,
        UI,
        Window, 
        Slice, 
        PickUp,
        DrawOnTablet,
        InSitu,
        Select,
    }
    public enum InputTypes
    {
        First,
        Move,
        Last
    }
    public Modi Mode { get; set; }
    public static InputTypes InputType { get; set; }

    void Start()
    {
        //screenWidth = Screen.width;
        //screenHeight = Screen.height;

        // Screen sizes are manually set in the inspector to 1904 x 896
        // because on different monitors there where differnt outcomes while calculating the touch position

        panelScaleX = transform.localScale.x;
        panelScaleZ = transform.localScale.z;

        firstCount = 0;
        holdCount = 0;
        moveCount = 0;
        tapCount = 0;

        // Drawing on the tablet is the default state of the system
        Mode = Modi.DrawOnTablet;

        // activate touch handling
        EnhancedTouchSupport.Enable();
    }

    /// <summary>
    /// Method <c>FixedUpdate</c> is called 50 times per second. 
    /// Input Device fires multiple TouchPhase.Began, therefore the true first and last input events need to be identified as such.
    /// <c>FixedUpdate</c> also identifies double taps and calls the according functionality.
    /// </summary>
    /// 

    void Update()
    {
        // remove dead touch events from last frame
        foreach (var touchEvent in deadTouchEvents)
        {
            aliveTouchEvents.Remove(touchEvent.Touch.touchId);
        }

        // clear list of dead events from last frame
        deadTouchEvents.Clear();

        // remove old touches from list of recent taps
        for (int i = recentTaps.Count-1; i >= 0; i--)
        {
            if (recentTaps[i].GetAge() > DOUBLE_TAP_TIME_THRESHOLD)
            {
                recentTaps.RemoveAt(i);
            }
        }       

        // process new touch events
        if (Touch.activeTouches.Count > 0)
        {
            foreach(var touch in Touch.activeTouches)
            {
                // only consider the three touch phases that we use in this system
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began &&
                    touch.phase != UnityEngine.InputSystem.TouchPhase.Moved &&
                    touch.phase != UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    continue;
                }

                TouchState touchState = new TouchState();
                touchState.delta = touch.delta;
                touchState.position = touch.screenPosition;
                touchState.phase = touch.phase;
                touchState.startPosition = touch.startScreenPosition;
                touchState.startTime = touch.startTime;
                touchState.isTap = touch.isTap;
                touchState.pressure = touch.pressure;
                touchState.radius = touch.radius;
                touchState.tapCount = (byte)(touch.tapCount);
                touchState.touchId = touch.touchId;
                InputReceived(touchState);
            }
        }
    }

    /// <summary>
    /// <c>InputReceived</c> receives input from the digital pen and identifies whether it is a single or a double tap, since double taps toggle the Bookmark Visibility. 
    /// A touch interaction is identified as a double tap when the second tap arrives within 3 seconds.
    /// Here it is also identified if the received input is the beginning, the end or in the middle of an input gesture. (First/Last/Move Input Type)
    /// </summary>
    public void InputReceived(TouchState touch)
    {
        bool isTap = false;
        bool isDoubleTap = false;
        bool isHold = false;

        // get touch position in the scene
        Vector3 touchPosition = transform.TransformPoint(ConvertCoordinates(touch.position));

        // get camera/head position
        //Vector3 headPosition = CameraCache.Main.transform.position;
        Vector3 headPosition = UserPositionManager.Instance.GetClosestUser(transform.position).position;

        // perform raycast into the scene, from the head position through the touch position
        var rayHits = Physics.RaycastAll(headPosition, touchPosition - headPosition);

        // sort all results in ascending distance
        Array.Sort(rayHits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
        List<RaycastHit> sortedHitList = rayHits.Where(hit => hit.transform.gameObject.GetComponent<ITouchable>() != null).ToList();

        // iterate over all hits, fill list of ordered, hierarchical targets
        List<RaycastHit> targetList = new List<RaycastHit>();        
        foreach(var hit in sortedHitList)
        {
            // only consider game objects that implement the ITouchable interface
            var touchable = hit.transform.gameObject.GetComponent<ITouchable>();            
            if (touchable != null)
            {
                targetList.Add(hit);
            }
        }

        // generate or update touch event
        TouchEvent touchEvent;
        if (aliveTouchEvents.ContainsKey(touch.touchId))
        {
            touchEvent = aliveTouchEvents[touch.touchId];
            touchEvent.Update(touch, touchPosition, targetList);
        }
        else
        {
            touchEvent = new TouchEvent(touch, touchPosition, targetList);
            aliveTouchEvents[touch.touchId] = touchEvent;
        }

        // detect tap/hold/double tap
        if (touchEvent.Touch.phase == UnityEngine.InputSystem.TouchPhase.Ended /*&& touchEvent.GetDistance() < TAP_DISTANCE_THRESHOLD*/)
        {
            Debug.Log("Tap distance: " + touchEvent.GetDistance());
            if (touchEvent.GetDistance() < TAP_DISTANCE_THRESHOLD)
            {
                if (touchEvent.GetAge() > TAP_TIME_THRESHOLD)
                {
                    isHold = true;
                }
                else
                {
                    // test for double tap by checking for recent tap close by
                    foreach (var recentTap in recentTaps)
                    {
                        if (Vector2.Distance(recentTap.Touch.position, touchEvent.Touch.position) < DOUBLE_TAP_DISTANCE_THRESHOLD &&
                            recentTap.GetAge() < DOUBLE_TAP_TIME_THRESHOLD)
                        {
                            // double tap
                            isDoubleTap = true;
                            recentTaps.Remove(recentTap);
                            break;
                        }
                    }

                    // if it is not a double tap, it is a new regular tap
                    if (isDoubleTap == false)
                    {
                        isTap = true;
                        recentTaps.Add(touchEvent);
                    }
                }
            }
            
        }

        // check if touch is already captured, trigger move or up callback
        if (capturedTouchEvents.ContainsKey(touch.touchId))
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                capturedTouchEvents[touch.touchId].OnTouchMove(touchEvent);

                if (PrintDebugOutput)
                {
                    Debug.Log("Touch Move (captured) on " + capturedTouchEvents[touch.touchId].ToString());
                }

                return;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                if (PrintDebugOutput)
                {
                    Debug.Log("Touch Up (captured) on " + capturedTouchEvents[touch.touchId].ToString());
                }

                capturedTouchEvents[touch.touchId].OnTouchUp(touchEvent);
                capturedTouchEvents.Remove(touch.touchId);
            }
        }

        


        // generate touch events (low level touch down, move and up first, then taps etc.)
        foreach (var hit in touchEvent.Targets)
        {
            if (touchEvent.IsConsumed)
            {
                break;
            }

            var touchable = hit.transform.gameObject.GetComponent<ITouchable>();
            if (touchable != null)
            {
                touchEvent.CurrentTarget = hit;
                switch (touchEvent.Touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        if (PrintDebugOutput)
                        {
                            Debug.Log("Touch Down on " + touchable.ToString());
                        }

                        touchable.OnTouchDown(touchEvent);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        if (PrintDebugOutput)
                        {
                            Debug.Log("Touch Move on " + touchable.ToString());
                        }

                        touchable.OnTouchMove(touchEvent);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        if (PrintDebugOutput)
                        {
                            Debug.Log("Touch Up on " + touchable.ToString());
                        }

                        touchable.OnTouchUp(touchEvent);
                        break;
                }

                // generate hold event
                if (isHold)
                {
                    if (PrintDebugOutput)
                    {
                        Debug.Log("Touch Hold on " + touchable.ToString());
                    }

                    touchable.OnHold(touchEvent);
                }

                // generate tap event
                if (isTap)
                {
                    if (PrintDebugOutput)
                    {
                        Debug.Log("Touch Tap on " + touchable.ToString());
                    }

                    touchable.OnTap(touchEvent);
                }

                // generate double tap event
                if (isDoubleTap)
                {
                    if (PrintDebugOutput)
                    {
                        Debug.Log("Touch Double Tap on " + touchable.ToString());
                    }

                    touchable.OnDoubleTap(touchEvent);
                }

                // check if touch was consumed
                if (touchEvent.IsConsumed)
                {
                    break;
                }

                // check if touch was captured
                if (touchEvent.CapturedBy != null)
                {
                    capturedTouchEvents[touchEvent.Touch.touchId] = touchEvent.CapturedBy;
                    break;
                }
            }            
        }

        // if not consumed, trigger legacy panel interactions
        if (touchEvent.IsConsumed == false)
        {
            if (isDoubleTap)
            {
                interactionHandler.ToggleBookmarkVisibility();
            }

            if (touchEvent.Touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                if (Mode == InputHandler.Modi.Window)
                {
                    interactionHandler.RotateScaleOnWindow(touchEvent);
                }
            }

            if (touchEvent.Touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {

                if (touchEvent.GetAge() > TAP_TIME_THRESHOLD)
                {
                    Vector2 diffPos = touchEvent.History.First().position - touchEvent.History.Last().position;
                    float diff = Math.Abs(diffPos.x) + Math.Abs(diffPos.y);
                    if (diff < 15)
                    {
                        Debug.Log("Hold");
                        touchInteractions.Hold(touchEvent);
                    }
                    else
                    {
                        Debug.Log("Move");
                        touchInteractions.Move(touchEvent);
                    }
                }
                else
                {
                    if (isTap)
                    {
                        Debug.Log("Tap");
                        touchInteractions.Tap(touchEvent);
                    }
                    else
                    {
                        Debug.Log("Move");
                        touchInteractions.Move(touchEvent);
                    }
                }

                if (scaling)
                {
                    scaling = false;
                }

            }

            if (Mode == Modi.DrawOnTablet || Mode == Modi.InSitu)
            {
                Vector3 inputVector = ConvertCoordinates(touchEvent.Touch.position);
                switch (touchEvent.Touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        interactionHandler.DrawStroke(inputVector, InputTypes.First);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        interactionHandler.DrawStroke(inputVector, InputTypes.Move);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        interactionHandler.DrawStroke(inputVector, InputTypes.Last);
                        break;
                }
            }
        }

        if (touchEvent.Touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            deadTouchEvents.Add(touchEvent);
        }        

    }

    /// <summary>
    /// method <c>ConvertCoordinates</c> receives either touch or NeoSmartPen input and converts it into coordinates on the virtual plane in unity,
    /// so that virtual annotations can be correctly displayed.
    /// </summary>
    public Vector3 ConvertCoordinates(object input)
    {
        if (input.GetType() == typeof(Vector2))
        {                       
            Vector2 mouseInput = (Vector2)input;

            // currently, the positions from the raspberry os originate in the top left corner, thus, we convert them to bottom left first
            //mouseInput.y = (mouseInput.y * -1) + screenHeight; // ToDo: Do we need to use scaleScreenX here?

            // Conversion if Input comes from touch screen, bottom left origin of coordinate system
            float scaleY = panelScaleZ;
            float halfScreenX = screenWidth / 2;
            float halfScreenY = screenHeight / 2;

            float scaleScreenX = screenWidth / panelScaleX;
            float scaleScreenY = screenHeight / scaleY;

            // Switch from bottom left to middle origin coordinate system, scale to panel local dimensions
            float x = (mouseInput.x - halfScreenX) / scaleScreenX;
            float y = (mouseInput.y - halfScreenY) / scaleScreenY;

            Vector3 localCoords = new Vector3();

            // The size of a plane is by default 10 x 10 units in unity, while still being 1x1 in plane units
            // so for the scale and position to be correct, it has to be divided by scale / 10 after flipping coordinate systems

            localCoords.x = x / (panelScaleX / 10);
            localCoords.y = 0.0f;
            localCoords.z = y / (scaleY / 10);

            //// Wolfgang: currently we are not using the plane mesh
            //localCoords.x = x / panelScaleX;
            //localCoords.y = 0.0f;
            //localCoords.z = y / scaleY;

            return localCoords;
        }        
        return new Vector3(0, 0, 0);

    }
}
