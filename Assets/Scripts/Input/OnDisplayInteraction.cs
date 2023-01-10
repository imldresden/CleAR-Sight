// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using u2vis.Input;
using UnityEngine;
using System.Linq;
using IMLD.Unity.Core;

public class OnDisplayInteraction : MonoBehaviour, ITouchable
{
    //bool isSelectionMode = false;
   // bool isDrawingMode = false;
    public InputHandler inputHandler;
    public InteractionHandler interactionHandler;
    //public GameObject penImage;
    // public GameObject cursorImage;

    // public GameObject panel;

    // public u2visInputModule inputModule;
    public GameObject tablet;
    public BoxCollider tabletColl;
    public int twoDmodelID;

    public Collider Collider => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("x: "+UnityEngine.Input.mousePosition.x+", y: "+UnityEngine.Input.mousePosition.y+", z:"+UnityEngine.Input.mousePosition.z);
    }      

    public void select(Vector2 touchPosition)
    {
        Vector3 worldPos = getTouchWorldPosition(touchPosition);

        Debug.Log("Something was selected ... ");
        Debug.Log("At: X: "+worldPos.x+", Y:"+worldPos.y+", Z:"+worldPos.z);

        if (GetUiElements(worldPos, out List<UiElemHitResult> results))
            for (int i = 0; i < results.Count; i++)
            {
                Debug.Log("Hit UI element!");
                var res = results[i];
                Debug.Log(res);
                res.UiElement.OnMouseBtnUp(0, i, res.HitResult);
            }
    }

    public Vector3 getTouchWorldPosition(Vector2 tabletPos)
    {
        return tablet.transform.TransformPoint(inputHandler.ConvertCoordinates(tabletPos));
    }

   /* private bool GetUiElement(Vector3 screenPosition, out IUiElement uiElement, out RaycastHit hit)
    {
        uiElement = null;
        var ray = Camera.main.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray.origin, ray.direction, out hit))
        {
            //Debug.Log("Nothing hit");
            return false;
        }
        uiElement = hit.transform.GetComponent<IUiElement>();
        if (uiElement == null)
        {
            // Debug.Log("uiElement null");
            return false;
        }
        return true;
    }*/

    private bool GetUiElements(Vector3 worldPos, out List<UiElemHitResult> uiElements)
    {
        uiElements = new List<UiElemHitResult>();
        // var ray = tabletColl.Raycast(new Ray(Camera.main.transform.position, worldPos), out RaycastHit hit, 5.0f);
        // 
        //var ray = Camera.main.ScreenPointToRay(screenPosition);

        RaycastHit[] hits;

        Vector3 headposition = UserPositionManager.Instance.GetClosestUser(tablet.transform.position).position;

       // Debug.Log("Headpos: " + headposition);

        hits = Physics.RaycastAll(headposition, worldPos - headposition, 5.0f);
       // Debug.DrawRay(headposition, worldPos - Camera.main.transform.position);

        if (hits.Length > 0)
        {
            Debug.Log("Something was hit!!!!!!!");
            var hit = hits[hits.Length - 1];

            if (hit.transform.GetComponent<IUiElement>() != null) {

                var uiElem = hit.transform.GetComponent<IUiElement>();

                if (uiElem == null && hit.transform.GetChild(0) != null)
                {
                    uiElem = hit.transform.GetChild(0).GetComponent<IUiElement>();
                }

                if (uiElem != null)
                {
                    uiElements.Add(new UiElemHitResult(uiElem, hit));
                }
            }
        }

        if (uiElements.Count == 0)
            return false;
        uiElements.Sort(UiElemHitResult.Compare);
        return true;
    }

    public void OnTap(TouchEvent touch)
    {
        Debug.Log("Tap Position: "+ touch.Position);
        //throw new System.NotImplementedException();
        if (inputHandler.Mode == InputHandler.Modi.Select)
        {
            interactionHandler.ContactSelect(touch.Touch.position);
        }
    }

    public void OnHold(TouchEvent touch)
    {
       // throw new System.NotImplementedException();
    }

    public void OnDoubleTap(TouchEvent touch)
    {
       // throw new System.NotImplementedException();
    }

    public void OnTouchDown(TouchEvent touch)
    {
        //  throw new System.NotImplementedException();
       // touch.Consume();
    }

    public void OnTouchMove(TouchEvent touch)
    {
       // throw new System.NotImplementedException();
    }

    public void OnTouchUp(TouchEvent touch)
    {
      //  throw new System.NotImplementedException();
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

