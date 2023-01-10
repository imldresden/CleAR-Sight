// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DimensionsMessage
{
    public static MessageContainer.MessageType Type = MessageContainer.MessageType.DIMENSIONS;
    [JsonProperty]
    public float screenWidth;
    public float screenHeight;


    public DimensionsMessage(float width, float height)
    {
        screenWidth = width;
        screenHeight = height;
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

    public static DimensionsMessage Unpack(MessageContainer container)
    {
        if (container.Type != Type)
        {
            return null;
        }
        try
        {
            string Payload = Encoding.UTF8.GetString(container.Payload);
            var Result = JsonConvert.DeserializeObject<DimensionsMessage>(Payload);
            return Result;
        }
        catch (Exception e)
        {
            return null;
        }
    }

}
