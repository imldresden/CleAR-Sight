// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IMLD.Touch
{
    public class TouchUdpListener
    {

        public delegate void TouchDownEventHandler(object sender, TouchStruct e);
        public delegate void TouchMoveEventHandler(object sender, TouchStruct e);
        public delegate void TouchUpEventHandler(object sender, TouchStruct e);

        public event TouchDownEventHandler TouchDownEvent;
        public event TouchMoveEventHandler TouchMoveEvent;
        public event TouchUpEventHandler TouchUpEvent;

        public IReadOnlyDictionary<int, TouchStruct> Touches { get; private set; }

        bool abort = false;
        Dictionary<int, TouchStruct> touches = new Dictionary<int, TouchStruct>();
        int eventCounter = 0;
        int port;

        public TouchUdpListener(int port = 3000)
        {
            this.port = port;
            Touches = touches;
        }

        public async Task Run()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            using (var client = new UdpClient(endPoint))
            {
                Console.WriteLine("Listening for touch events.");
                while (!abort)
                {
                    var receivedResult = await client.ReceiveAsync();
                    var resultString = Encoding.ASCII.GetString(receivedResult.Buffer);
                    string[] lines = resultString.Split('\n');

                    TouchStruct touch;
                    foreach (var line in lines) // iterate over each line
                    {
                        bool success = TryParseFromString(line, out touch); // parse touch event data from string
                        if (success)
                        {
                            HandleTouch(touch); // handle the event
                        }
                    }
                }
            }
        }

        private void HandleTouch(TouchStruct touch)
        {
            if (touches.ContainsKey(touch.Id)) // existing touch
            {
                if (touch.State == 0) // state is down for an existing touch -- this should not happen!
                {
                    var oldTouch = touches[touch.Id];
                    oldTouch.State = 2;
                    touches[touch.Id] = touch;

                    TouchUpEvent?.Invoke(this, oldTouch); // send up event for old touch
                    TouchDownEvent?.Invoke(this, touch); // send down event for new touch
                }
                else if (touch.State == 1) // state is moved
                {
                    touches[touch.Id] = touch;
                    TouchMoveEvent?.Invoke(this, touch); // send move event
                }
                else if (touch.State == 2) // state is up
                {
                    touches.Remove(touch.Id);
                    TouchUpEvent?.Invoke(this, touch); // send up event
                }
            }
            else // new touch
            {

                if (touch.State == 0) // state is down
                {
                    touches.Add(touch.Id, touch);
                    TouchDownEvent?.Invoke(this, touch); // send down event
                }
                else if (touch.State == 1) // state is moved -- this should not happen!
                {
                    touches.Add(touch.Id, touch);
                    var fakeTouch = touch;
                    fakeTouch.State = 0;
                    TouchDownEvent?.Invoke(this, fakeTouch); // send missing down event for this touch
                    TouchMoveEvent?.Invoke(this, touch); // send move event for this touch
                }
                else if (touch.State == 2) // state is up -- this should not happen!
                {
                    var fakeTouch = touch;
                    fakeTouch.State = 0;
                    TouchDownEvent?.Invoke(this, fakeTouch); // send missing down event for this touch
                    TouchUpEvent?.Invoke(this, touch); // send up event for this touch
                }
            }
        }

        public void Stop()
        {
            abort = true;
        }

        public static bool TryParseFromString(string buffer, out TouchStruct touch)
        {
            touch = new TouchStruct();
            bool success = true;
            if (buffer == null || buffer.Length == 0)
            {
                return false;
            }

            string[] splitString = buffer.Split(',');

            if (splitString.Length != 10)
            {
                return false;
            }

            touch.Sender = splitString[0];
            success &= int.TryParse(splitString[1], out touch.EventCount);
            success &= int.TryParse(splitString[2], out touch.Id);
            success &= int.TryParse(splitString[3], out touch.State);
            success &= float.TryParse(splitString[4], out touch.StartX);
            success &= float.TryParse(splitString[5], out touch.StartY);
            success &= float.TryParse(splitString[6], out touch.X);
            success &= float.TryParse(splitString[7], out touch.Y);

            touch.Y = (touch.Y * -1) + 4096;
            touch.StartY = (touch.StartY * -1) + 4096;


            string[] splitTime = splitString[8].Split('.');
            if (splitTime.Length == 2)
            {
                long t0, t1;
                success &= long.TryParse(splitTime[0], out t0);
                success &= long.TryParse(splitTime[1], out t1);
                touch.StartTime = t0 * 1000000 + t1;
            }

            splitTime = splitString[9].Split('.');
            if (splitTime.Length == 2)
            {
                long t0, t1;
                success &= long.TryParse(splitTime[0], out t0);
                success &= long.TryParse(splitTime[1], out t1);
                touch.Time = t0 * 1000000 + t1;
            }

            return success;
        }

    }

    public struct TouchStruct
    {
        public string Sender;
        public int EventCount;
        public int Id;
        public float X;
        public float Y;
        public float StartX;
        public float StartY;
        public long Time;
        public long StartTime;
        public bool IsPressed;
        public int State;
        public float deltaX;
        public float deltaY;
    }
}
