// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;

namespace IMLD.Unity.Network.Messages
{
    public class MessageSlicingCTMode : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.SLICE_CTMODE;

        public bool CTMode;

        public MessageSlicingCTMode(bool ctmode)
        {
            CTMode = ctmode;
        }
        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageSlicingCTMode Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageSlicingCTMode>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}