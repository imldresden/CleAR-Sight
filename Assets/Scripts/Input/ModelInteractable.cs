// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using u2vis.Input;
using IMLD.Unity.Core;

public class ModelInteractable : MonoBehaviour, ITouchable
{
    public InputHandler inputHandler;
    InteractionHandler interactionHandler;
    public GameObject panel;
    bool initialPickup;
    public bool selected = false;
    public int modelID;
    public bool physModel;

    public Collider Collider => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        interactionHandler = panel.GetComponent<InteractionHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        // Handles picking up a model

        if (inputHandler.Mode == InputHandler.Modi.PickUp && selected && !physModel)
        {
            Vector3 newPos = new Vector3(panel.transform.position.x, panel.transform.position.y, panel.transform.position.z);

            transform.position = newPos;

            Quaternion rot = new Quaternion(panel.transform.rotation.x, panel.transform.rotation.y, panel.transform.rotation.z, panel.transform.rotation.w);

            transform.rotation = rot;

        }             
    }

    public void OnTap(TouchEvent touch)
    {
        Debug.Log("ON TAP");
        //throw new System.NotImplementedException();
        if (inputHandler.Mode == InputHandler.Modi.Window && interactionHandler.model == this.gameObject)
        {
            //Debug.Log("tap correctly recognized");
            interactionHandler.windowModelSelect(touch.Touch.position, modelID);
            touch.Consume();
        }
        
    }

    public void select(Vector2 touchPosition)
    {
        Vector3 worldPos = getTouchWorldPosition(touchPosition);
       // Debug.Log("Call select");

        if (GetUiElements(worldPos, out List<UiElemHitResult> results))
            for (int i = 0; i < results.Count; i++)
            {
              //  Debug.Log("Hit UI element!");
                var res = results[i];
                Debug.Log(res.HitResult);
                res.UiElement.OnMouseBtnUp(0, i, res.HitResult);
            }
    }

    public Vector3 getTouchWorldPosition(Vector2 tabletPos)
    {
        return panel.transform.TransformPoint(inputHandler.ConvertCoordinates(tabletPos));
    }

    private bool GetUiElements(Vector3 worldPos, out List<UiElemHitResult> uiElements)
    {
        uiElements = new List<UiElemHitResult>();
        // var ray = tabletColl.Raycast(new Ray(Camera.main.transform.position, worldPos), out RaycastHit hit, 5.0f);
        // 
        //var ray = Camera.main.ScreenPointToRay(screenPosition);

        RaycastHit[] hits;

        Vector3 headposition = UserPositionManager.Instance.GetClosestUser(panel.transform.position).position;

        //Debug.Log("Headpos: " + headposition);

        hits = Physics.RaycastAll(headposition, worldPos - headposition, 5.0f);
      //  Debug.DrawRay(headposition, worldPos - headposition);
       // Debug.Log("hits: "+ hits.Length);

        if (hits.Length > 0)
        {
           // Debug.Log("Hit Something: Hits Length: "+ hits.Length);

            var hit = hits[hits.Length - 1];

            Debug.Log(hit.transform.gameObject.name);

            var uiElem = hit.transform.GetComponent<IUiElement>();

            if (uiElem == null && hit.transform.GetChild(0) != null)
            {
               // Debug.Log("Hit the child of a UI Element");
                uiElem = hit.transform.GetChild(0).GetComponent<IUiElement>();
            }
            if (uiElem != null)
            {
              //  Debug.Log("Hit UI elem");
                uiElements.Add(new UiElemHitResult(uiElem, hit));
            }
        }

        if (uiElements.Count == 0)
            return false;
        uiElements.Sort(UiElemHitResult.Compare);
        return true;
    }


    public void OnHold(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnDoubleTap(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTouchDown(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTouchMove(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTouchUp(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    private class UiElemHitResult
    {
        public readonly IUiElement UiElement;
        public readonly RaycastHit HitResult;

        public UiElemHitResult(IUiElement uiElement, RaycastHit hitResult)
        {
            UiElement = uiElement;
            HitResult = hitResult;
        }

        public static int Compare(UiElemHitResult x, UiElemHitResult y)
        {
            return Comparer<float>.Default.Compare(x.HitResult.distance, y.HitResult.distance);
        }
    }

}
