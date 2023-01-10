// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageBookmarkCreate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.BOOKMARK_CREATE;

        [JsonIgnore]
        public Vector3 Scale
        {
            get { return new Vector3(scaleX, scaleY, scaleZ); }
        }

        [JsonIgnore]
        public Vector3 Position
        {
            get { return new Vector3(posX, posY, posZ); }
        }

        [JsonIgnore]
        public Quaternion Rotation
        {
            get { return new Quaternion(rotX, rotY, rotZ, rotW); }
        }

        public float posX, posY, posZ, rotX, rotY, rotZ, rotW, scaleX, scaleY, scaleZ;

        public MessageBookmarkCreate(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            posX = position.x;
            posY = position.y;
            posZ = position.z;

            scaleX = scale.x;
            scaleY = scale.y;
            scaleZ = scale.z;

            rotX = rotation.x;
            rotY = rotation.y;
            rotZ = rotation.z;
            rotW = rotation.w;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageBookmarkCreate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageBookmarkCreate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
