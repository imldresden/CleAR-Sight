// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageAnnotationStrokeUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOTATION_STROKE_UPDATE;

        [JsonIgnore]
        public Vector3 Point
        {
            get { return new Vector3(posX, posY, posZ); }
        }

        public float posX, posY, posZ;

        public InputHandler.InputTypes InputType;

        public MessageAnnotationStrokeUpdate(Vector3 point, InputHandler.InputTypes type)
        {
            posX = point.x;
            posY = point.y;
            posZ = point.z;
            InputType = type;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAnnotationStrokeUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAnnotationStrokeUpdate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}