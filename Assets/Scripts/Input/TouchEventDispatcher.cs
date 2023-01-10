// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IMLD.Unity.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace IMLD.Touch
{
    /// <summary>
    /// This Unity component receives touch events sent over UDP with the help of a <see cref="TouchUdpListener"/>.
    /// It then dispatches these events by sending them over the network, injecting them into the event pipeline or printing them to the debug output.
    /// </summary>
    public class TouchEventDispatcher : MonoBehaviour
    {
        /// <summary>
        /// Determines if received touch events should be injected into the Unity input event pipeline.
        /// </summary>
        [Tooltip("Determines if received touch events should be injected into the Unity input event pipeline.")]
        public bool InjectTouchEvents = false;

        /// <summary>
        /// The UDP port to listen for touch data on.
        /// </summary>
        [Tooltip("The UDP port to listen for touch data on.")]
        public int Port = 53000;

        /// <summary>
        /// Determines if received touch events should be printed to Unity's debug output.
        /// </summary>
        [Tooltip("Determines if received touch events should be printed to Unity's debug output.")]
        public bool PrintDebugOutput = false;

        /// <summary>
        /// Determines if received touch events should be sent over the network.
        /// </summary>
        [Tooltip("Determines if received touch events should be sent over the network.")]
        public bool SendTouchEventsOverNetwork = false;

        /// <summary>
        /// Sets the filter for touches based on their sender.
        /// Each touch from the <see cref="TouchUdpListener"/> has a sender. If the sender does not mach this string, it is considered "filtered".
        /// Filtered touches are still reported but have a flag set to show this status. It is up to the application logic to decide how to handle them.
        /// </summary>
        [Tooltip("Sets the filter for touches based on their identifier.")]
        public string TouchSenderFilter;

        private TouchMessage message;

        private ConcurrentQueue<TouchStruct> queue;

        private TouchUdpListener touchListener;

        private Touchscreen touchscreen;

        /// <summary>
        /// Event invoked for "touch down" events.
        /// </summary>
        public event TouchUdpListener.TouchDownEventHandler TouchDownEvent;

        /// <summary>
        /// Event invoked for "touch move" events.
        /// </summary>
        public event TouchUdpListener.TouchMoveEventHandler TouchMoveEvent;

        /// <summary>
        /// Event invoked for "touch up" events.
        /// </summary>
        public event TouchUdpListener.TouchUpEventHandler TouchUpEvent;

        /// <summary>
        /// Injects the specified <see cref="TouchStruct"/> into Unity's event pipeline.
        /// </summary>
        /// <param name="touch"></param>
        ///
        public InputHandler inputHandler;
        float screenHeight;
        float screenWidth;

        private void InjectTouchEvent(TouchStruct touch)
        {
            UnityEngine.InputSystem.TouchPhase touchPhase = UnityEngine.InputSystem.TouchPhase.None;
            byte flag = 1;

            switch (touch.State)
            {
                case 0:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Began;
                    break;

                case 1:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Moved;
                    break;

                case 2:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Ended;
                    break;
            }

            if (touch.Sender == TouchSenderFilter)
            {
                flag = 0;
            }

            InputSystem.QueueStateEvent(touchscreen,
            new TouchState
            {
                phase = touchPhase,
                touchId = touch.Id,
                position = new Vector2(touch.X / 4096f * screenWidth, touch.Y / 4096f * screenHeight),
                startPosition = new Vector2(touch.StartX / 4096f * screenWidth, touch.StartY / 4096f * screenHeight),
                flags = flag
            });
           // Debug.Log(flag);
        }

        /// <summary>
        /// Logs the specified <see cref="TouchStruct"/> to Unity's debug log.
        /// </summary>
        /// <param name="touch"></param>
        private void LogToDebug(TouchStruct touch)
        {
            Debug.Log(touch.Sender + " " + touch.Id + ": " + touch.State + " [" + touch.X + ", " + touch.Y + "]");
        }

        /// <summary>
        /// Sends the specified <see cref="TouchStruct"/> over network.
        /// </summary>
        /// <param name="touch"></param>
        private void SendTouchEvent(TouchStruct touch)
        {
            UnityEngine.InputSystem.TouchPhase touchPhase = UnityEngine.InputSystem.TouchPhase.None;

            switch (touch.State)
            {
                case 0:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Began;
                    break;

                case 1:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Moved;
                    break;

                case 2:
                    touchPhase = UnityEngine.InputSystem.TouchPhase.Ended;
                    break;
            }

            message.Touch = new TouchState
            {
                phase = touchPhase,
                touchId = touch.Id,
                position = new Vector2(touch.X / 4096f * screenWidth, touch.Y / 4096f * screenHeight),
                startPosition = new Vector2(touch.StartX / 4096f * screenWidth, touch.StartY / 4096f * screenHeight),
                delta = new Vector2(touch.deltaX / 4096f * screenWidth, touch.deltaY / 4096f * screenHeight),
            };

            NetworkManager.Instance.SendMessage(message.Pack());
        }

        /// <summary>
        /// The start method is called before the first frame update and sets up and starts the <see cref="TouchUdpListener"/>.
        /// </summary>
        private void Start()
        {
            screenHeight = inputHandler.screenHeight;
            screenWidth = inputHandler.screenWidth;

            // create virtual touch screen device
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            message = new TouchMessage();

            queue = new ConcurrentQueue<TouchStruct>();

            // create touch listener on defined UDP port and register event callbacks
            touchListener = new TouchUdpListener(Port);
            touchListener.TouchDownEvent += TouchDownHandler;
            touchListener.TouchMoveEvent += TouchMoveHandler;
            touchListener.TouchUpEvent += TouchUpHandler;

            // run touch listener on thread pool
            Task.Run(() => touchListener.Run());
        }

        private void TouchDownHandler(object sender, TouchStruct touch)
        {
            queue.Enqueue(touch);
        }

        private void TouchMoveHandler(object sender, TouchStruct touch)
        {
            queue.Enqueue(touch);
        }

        private void TouchUpHandler(object sender, TouchStruct touch)
        {
            queue.Enqueue(touch);
        }

        /// <summary>
        /// The update method runs once per frame and processes all queued touch events.
        /// </summary>
        private void Update()
        {
            HandleTouches();
        }

        private void LateUpdate()
        {
            HandleTouches();
        }

        private void HandleTouches()
        {
            TouchStruct touch;
            while (!queue.IsEmpty)
            {
                bool success = queue.TryDequeue(out touch);
                if (success)
                {
                    // invoke event callbacks
                    switch (touch.State)
                    {
                        case 0:
                            TouchDownEvent?.Invoke(this, touch);
                            break;

                        case 1:
                            TouchMoveEvent?.Invoke(this, touch);
                            break;

                        case 2:
                            TouchUpEvent?.Invoke(this, touch);
                            break;
                    }

                    // inject touch event into the input system
                    if (InjectTouchEvents == true)
                    {
                        InjectTouchEvent(touch);
                    }

                    // send touch over network
                    if (SendTouchEventsOverNetwork == true && NetworkManager.Instance != null)
                    {
                        SendTouchEvent(touch);
                    }

                    // print debug output
                    if (PrintDebugOutput)
                    {
                        LogToDebug(touch);
                    }
                }
            }
        }
    }
}