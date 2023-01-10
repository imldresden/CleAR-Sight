// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;
using IMLD.Unity.Core;

namespace IMLD.Unity.Network.Messages
{
    public class MessageWindowModelSelection : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.MODEL_SELECTION;

        public float touchX;
        public float touchY;
        public int modelID;

        public MessageWindowModelSelection(Vector2 position, int ID)
        {
            touchX = position.x;
            touchY = position.y;
            modelID = ID;
        }
        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageWindowModelSelection Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageWindowModelSelection>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
