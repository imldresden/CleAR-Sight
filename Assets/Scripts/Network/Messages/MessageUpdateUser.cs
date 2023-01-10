// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageUpdateUser : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_USER;

        [JsonIgnore]
        public Vector3 Position { get { return new Vector3(posx, posy, posz); } set { posx = value.x; posy = value.y; posz = value.z; } }
        [JsonIgnore]
        public Quaternion Orientation { get { return new Quaternion(rotx, roty, rotz, rotw); } set { rotx = value.x; roty = value.y; rotz = value.z; rotw = value.w; } }

        public Guid Id;

        [JsonIgnore]
        public Color Color { get { return new Color(color_r, color_g, color_b); } set { color_r = value.r; color_g = value.g; color_b = value.b; } }

        [JsonProperty]
        public float posx;
        [JsonProperty]
        public float posy;
        [JsonProperty]
        public float posz;
        [JsonProperty]
        public float rotx;
        [JsonProperty]
        public float roty;
        [JsonProperty]
        public float rotz;
        [JsonProperty]
        public float rotw;

        [JsonProperty]
        public float color_r;
        [JsonProperty]
        public float color_g;
        [JsonProperty]
        public float color_b;

        public MessageUpdateUser(Vector3 position, Quaternion orientation, Guid id, Color color)
        {
            posx = position.x; posy = position.y; posz = position.z;
            rotx = orientation.x; roty = orientation.y; rotz = orientation.z; rotw = orientation.w;
            color_r = color.r; color_g = color.g; color_b = color.b;
            Id = id;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageUpdateUser Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            try
            {
                string Payload = Encoding.UTF8.GetString(container.Payload);
                var Result = JsonConvert.DeserializeObject<MessageUpdateUser>(Payload);
                return Result;
            }
            catch (Exception e)
            {
                return null;
            }

        }

    }
}