// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageAnnotationStrokeClear : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOTATION_STROKE_CLEAR;

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAnnotationStrokeClear Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAnnotationStrokeClear>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
