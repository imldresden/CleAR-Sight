// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMLD.Unity.Utils;
using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using UnityEngine;
using IMLD.Unity.Tracking;

namespace IMLD.Unity.Core
{
    /// <summary>
    /// This Unity component manages user indicators. These small objects show the status and position of participants in the analysis session.
    /// </summary>
    public class UserPositionManager : MonoBehaviour
    {
        public static UserPositionManager Instance = null;

        public Color Color;
        public SimpleInterpolator UserIndicatorPrefab;
        private Guid id = Guid.NewGuid();

        private readonly Dictionary<Guid, SimpleInterpolator> userList = new Dictionary<Guid, SimpleInterpolator>();
        private MessageUpdateUser message;
        private Transform worldAnchor;
        private Transform cameraTransform;

        public Transform GetClosestUser(Vector3 position)
        {
            float distance = Vector3.Distance(CameraCache.Main.transform.position, position);
            Transform closestUser = CameraCache.Main.transform;

            foreach (var kvp in userList)
            {
                float dist = Vector3.Distance(kvp.Value.transform.position, position);
                if (dist < distance)
                {
                    distance = dist;
                    closestUser = kvp.Value.transform;
                }
            }

            return closestUser;
        }

        private Task OnAcceptedAsClient(MessageContainer obj)
        {
            MessageAcceptClient message = MessageAcceptClient.Unpack(obj);
            SetColor(message.ClientIndex);

            // send position update, so that the other users get up to speed
            SendUserUpdate();

            return Task.CompletedTask;
        }

        private Task OnRemoteUserUpdate(MessageContainer obj)
        {
            MessageUpdateUser message = MessageUpdateUser.Unpack(obj);

            if (userList.ContainsKey(message.Id))
            {
                var userIndicator = userList[message.Id];
                userIndicator.SetTargetLocalPosition(message.Position);
                userIndicator.SetTargetLocalRotation(message.Orientation);
                if (userIndicator.GetComponentInChildren<Renderer>().material.color != message.Color)
                {
                    userIndicator.GetComponentInChildren<Renderer>().material.color = message.Color;
                }
            }
            else if (UserIndicatorPrefab)
            {
                // new user joined, instantiate indicator
                var userIndicator = Instantiate(UserIndicatorPrefab, worldAnchor);
                userIndicator.SetTargetLocalPosition(message.Position);
                userIndicator.SetTargetLocalRotation(message.Orientation);
                userIndicator.GetComponentInChildren<Renderer>().material.color = message.Color;
                userList.Add(message.Id, userIndicator);

                // send position update, so that the new user gets up to speed
                SendUserUpdate();
            }

            return Task.CompletedTask;
        }

        private void SetColor(int i)
        {
            float hue;
            switch (i)
            {
                case 0:
                    hue = 0.0f;
                    break;

                case 1:
                    hue = 0.25f;
                    break;

                case 2:
                    hue = 0.5f;
                    break;

                default:
                    hue = UnityEngine.Random.value;
                    break;
            }

            Color = Color.HSVToRGB(hue, 0.9f, 0.95f);
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (WorldOrigin.Instance != null)
            {
                worldAnchor = WorldOrigin.Instance.transform;
            }
            else
            {
                worldAnchor = transform;
            }

            SetColor(0);

            // register network message handlers
            NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.UPDATE_USER, OnRemoteUserUpdate);
            NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ACCEPT_CLIENT, OnAcceptedAsClient);

            Transform cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            if (cameraTransform != null)
            {
                message = new MessageUpdateUser(cameraTransform.position, cameraTransform.rotation, id, Color);
                NetworkManager.Instance?.SendMessage(message);
            }
        }

        // Update is called once per frame
        private void Update()
        {
            //// Only consider sending an update every second frame
            //// ToDo: Is this a good approach?
            //if (Time.frameCount % 2 == 1)
            //{
            //    return;
            //}
                        
            SendUserUpdate();
        }

        private void SendUserUpdate()
        {
            cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            if (cameraTransform != null && message != null)
            {
                Pose? externalPose = PointCloudSpatialLocalizer.Instance?.GetExternalPose(new Pose(cameraTransform.position, cameraTransform.rotation));

                if(externalPose.HasValue)
                {
                    message.Color = Color;
                    message.Position = externalPose.Value.position;
                    message.Orientation = externalPose.Value.rotation;
                    message.Id = id;
                    NetworkManager.Instance?.SendMessage(message.Pack());
                }
                
            }
        }
    }
}