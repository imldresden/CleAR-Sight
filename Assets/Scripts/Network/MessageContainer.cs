// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network
{
    public class MessageContainer
    {

        public IPEndPoint Sender;

        public MessageType Type;
        public byte[] Payload;

        /// <summary>
        /// Enum of all possible message types. Register new messages here!
        /// </summary>
        public enum MessageType
        {
            WORLD_ANCHOR,
            ANNOUNCEMENT = FIRST_JSON_MESSAGE_TYPE,
            UPDATE_USER,
            ACCEPT_CLIENT,
            TOUCH,
            DIMENSIONS,
            VOLUME_PICKUP,
            VOLUME_RELEASE,
            VOLUME_TRANSFORM,
            VOLUME_SCALEROTATE,
            ANNOTATION_RELEASE,
            ANNOTATION_UPDATE,
            ANNOTATION_INSITUMODE,
            ANNOTATION_INSITU_PARAMETER,
            ANNOTATION_STROKE_UPDATE,
            ANNOTATION_STROKE_UNDO,
            ANNOTATION_STROKE_CLEAR,
            BOOKMARK_CREATE,
            BOOKMARK_DELETE,
            BOOKMARK_VISIBILITY,
            SLICE_FREEZE,
            SLICE_CTMODE,
            SET_WATERLEVEL,
            CONTACT_SELECTION_ENABLE,
            CONTACT_SELECTION,
            MODEL_SELECTION,
            COLORPICKER_CHANGED
        }

        public const byte FIRST_JSON_MESSAGE_TYPE = 128;

        public MessageContainer(MessageType type, string payload)
        {
            Type = type;
            Payload = Encoding.UTF8.GetBytes(payload);
        }

        public MessageContainer(MessageType type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }

        public static MessageContainer Deserialize(IPEndPoint sender, byte[] payload, byte messageType)
        {
            MessageType Type = (MessageType)messageType;
            var Message = new MessageContainer(Type, payload);
            Message.Sender = sender;
            return Message;
        }

        public static MessageContainer Deserialize(IPEndPoint sender, byte[] data)
        {
            byte Type = data[4];
            byte[] Payload = new byte[data.Length - 5];
            Array.Copy(data, 5, Payload, 0, data.Length - 5);
            return Deserialize(sender, Payload, Type);
        }

        //public static MessageContainer Deserialize(IPEndPoint sender, string input)
        //{
        //    var Message = JsonConvert.DeserializeObject<MessageContainer>(input);
        //    Message.Sender = sender;
        //    return Message;
        //}

        //public static MessageContainer Deserialize(IPEndPoint sender, JsonReader reader)
        //{
        //    JsonSerializer Serializer = JsonSerializer.CreateDefault();
        //    var Message = Serializer.Deserialize<MessageContainer>(reader);
        //    Message.Sender = sender;
        //    return Message;
        //}

        //private static string Serialize(MessageContainer message)
        //{
        //    return JsonConvert.SerializeObject(message);
        //}

        public byte[] Serialize()
        {
            byte[] Envelope = new byte[Payload.Length + 5];
            Array.Copy(BitConverter.GetBytes(Payload.Length), Envelope, 4);
            Envelope[4] = (byte)Type;
            Array.Copy(Payload, 0, Envelope, 5, Payload.Length);
            return Envelope;
        }

    }
}