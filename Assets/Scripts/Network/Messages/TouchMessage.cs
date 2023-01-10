// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.InputSystem.LowLevel;
using IMLD.Unity.Network;

public class TouchMessage
{
    public static MessageContainer.MessageType Type = MessageContainer.MessageType.TOUCH;
    public TouchState Touch;


    public TouchMessage(Touch touch)
    {
        Touch = new TouchState();
        Touch.delta = touch.delta;
        Touch.position = touch.screenPosition;
        Touch.phase = touch.phase;
        Touch.startPosition = touch.startScreenPosition;        
        Touch.startTime = touch.startTime;
        Touch.isTap = touch.isTap;
        Touch.pressure = touch.pressure;
        Touch.radius = touch.radius;
        Touch.tapCount = (byte)(touch.tapCount);
        Touch.touchId = touch.touchId;
    }

    public TouchMessage(TouchState touch)
    {
        Touch = new TouchState();
        Touch.delta = touch.delta;
        Touch.position = touch.position;
        Touch.phase = touch.phase;
        Touch.startPosition = touch.startPosition;
        Touch.startTime = touch.startTime;
        Touch.isTap = touch.isTap;
        Touch.pressure = touch.pressure;
        Touch.radius = touch.radius;
        Touch.tapCount = touch.tapCount;
        Touch.touchId = touch.touchId;
    }

    public TouchMessage()
    {
        Touch = new TouchState();
    }

    public MessageContainer Pack()
    {
        string Payload = JsonConvert.SerializeObject(this, Formatting.Indented,
    new JsonSerializerSettings()
    {
        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
    });
        return new MessageContainer(Type, Payload);
    }

    public static TouchMessage Unpack(MessageContainer container)
    {
        if (container.Type != Type)
        {
            return null;
        }
        try
        {
            string Payload = Encoding.UTF8.GetString(container.Payload);
            var Result = JsonConvert.DeserializeObject<TouchMessage>(Payload);
            return Result;
        }
        catch (Exception e)
        {
            return null;
        }
    }

}
