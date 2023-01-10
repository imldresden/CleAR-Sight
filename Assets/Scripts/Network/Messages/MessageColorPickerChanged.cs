// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageColorPickerChanged : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.COLORPICKER_CHANGED;

        [JsonIgnore]
        public Color Color { get { return new Color(r, g, b, a); } }

        public float r, g, b, a;
        public string Id;

        public MessageColorPickerChanged(Color color, string id)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;

            Id = id;
        }
        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageColorPickerChanged Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageColorPickerChanged>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
