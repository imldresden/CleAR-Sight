// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageVolumeScaleRotate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.VOLUME_SCALEROTATE;

        [JsonIgnore]
        public Vector3 Scale
        {
            get { return new Vector3(posX, posY, posZ); }
        }

        public float posX, posY, posZ;
        public float Angle;
        public int modelId;

        public MessageVolumeScaleRotate(Vector3 scale, float angle, int id)
        {
            posX = scale.x;
            posY = scale.y;
            posZ = scale.z;
            Angle = angle;
            modelId = id;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageVolumeScaleRotate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageVolumeScaleRotate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}