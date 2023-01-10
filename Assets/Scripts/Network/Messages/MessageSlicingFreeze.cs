// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;

namespace IMLD.Unity.Network.Messages
{
    public class MessageSlicingFreeze : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.SLICE_FREEZE;

        public bool SliceFrozen;

        public MessageSlicingFreeze(bool frozen)
        {
            SliceFrozen = frozen;
        }
        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageSlicingFreeze Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageSlicingFreeze>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}