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
    public class MessageAnnotationUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOTATION_UPDATE;

        [JsonIgnore]
        public Vector3 Position { get { return new Vector3(posx, posy, posz); } set { posx = value.x; posy = value.y; posz = value.z; } }
        [JsonIgnore]
        public Quaternion Orientation { get { return new Quaternion(rotx, roty, rotz, rotw); } set { rotx = value.x; roty = value.y; rotz = value.z; rotw = value.w; } }

        [JsonProperty]
        public int Id;
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

        public MessageAnnotationUpdate(Vector3 position, Quaternion orientation, int id)
        {
            posx = position.x; posy = position.y; posz = position.z;
            rotx = orientation.x; roty = orientation.y; rotz = orientation.z; rotw = orientation.w;
            Id = id;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAnnotationUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            try
            {
                string Payload = Encoding.UTF8.GetString(container.Payload);
                var Result = JsonConvert.DeserializeObject<MessageAnnotationUpdate>(Payload);
                return Result;
            }
            catch (Exception e)
            {
                return null;
            }

        }

    }
}