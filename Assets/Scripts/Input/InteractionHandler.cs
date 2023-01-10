// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using IMLD.Unity.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using u2vis.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityVolumeRendering;

public class InteractionHandler : MonoBehaviour
{
    public static bool DebugMode = true;

    public Paintable canvas;
    public InputHandler inputHandler;
    public TouchInteractions touchInteractions;
    public WindowMode windowMode;
    //public GameObject selectionBox;


    public GameObject model;
    public bool isVolumeRendering;
   // public GameObject pickedUpObj;
    // private GameObject panel;


    private static List<GameObject> bookMarks;

    private static bool bookMarksVisible = true;
    private GameObject ctScanPlane;
    private bool cTscanVisible = false;
    private GameObject slicingPlane;

    private Material barChartClipMat;
    private Material barChartMat;
    private Material scatterPlotCubeMat;
    private Material scatterPlotCubeClipMat;

    private GameObject contactObject;
    public GameObject contactButton;
    public GameObject physModel;

    public bool contactAR = false;



    /// <summary>
    /// <c>ActivateInsituAnnotations</c> puts the panel in in-situ mode. Lines are directly drawn into 3D space.
    /// </summary>
    public void ActivateInsituAnnotations(bool networkSync = true)
    {
        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationInSituMode(true));
        }

        Debug.Log("In-Situ activated!");

        canvas.brushThickness = canvas.inSituBrushThickness;
        inputHandler.Mode = InputHandler.Modi.InSitu;
       // canvas.brushThickness /= 10;
    }

    /// <summary>
    /// <c>DectivateInsituAnnotations</c> puts the panel back into tablet mode where the lines are bound to the panel.
    /// </summary>
    public void DeactivateInsituAnnotations(bool networkSync = true)
    {
        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationInSituMode(false));
        }

        Debug.Log("In-Situ deactivated!");

        inputHandler.Mode = InputHandler.Modi.DrawOnTablet;
        canvas.brushThickness = canvas.OnTabletbrushThickness;
    }

    public void SetInsituAnnotationProjectionParameter(float parameter, bool networkSync = true)
    {
        float param = Mathf.Clamp01(parameter);
        if (param == 0.0)
        {
            canvas.brushThickness = canvas.inSituBrushThickness;
        } else if (param == 1.0)
        {
            canvas.brushThickness = canvas.projectedBrushThickness;
        }

        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationInSituProjectionParameter(param));
        }

        canvas.ProjectionDistanceParameter = param;
    }

    //public void DrawStroke(Vector3 point, InputHandler.InputTypes type, bool networkSync = true)
    //{
    //    if (networkSync)
    //    {
    //        if (inputHandler.Mode == InputHandler.Modi.InSitu)
    //        {
    //            point = canvas.GetProjectedPoint(point, canvas.ProjectionDistanceParameter);
    //        }

    //        NetworkManager.Instance?.SendMessage(new MessageAnnotationStrokeUpdate(point, type));
    //    }        
        
    //    canvas.DrawInput(point, type, inputHandler.Mode);
    //}

    public void DrawStroke(Vector3 point, InputHandler.InputTypes type, bool networkSync = true)
    {
        if (networkSync)
        {
            if (inputHandler.Mode == InputHandler.Modi.InSitu)
            {
                point = PointCloudSpatialLocalizer.Instance.GetExternalPose(new Pose(canvas.GetProjectedPoint(point, canvas.ProjectionDistanceParameter), Quaternion.identity)).position;
            }

            NetworkManager.Instance?.SendMessage(new MessageAnnotationStrokeUpdate(point, type));
        }

        if (inputHandler.Mode == InputHandler.Modi.InSitu)
        {
            point = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(point, Quaternion.identity)).position;
        }
        canvas.DrawInput(point, type, inputHandler.Mode);
    }

    public void UndoLastStroke(bool networkSync = true)
    {
        if(networkSync)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationStrokeUndo());
        }

        canvas.Undo();
    }

    public void ClearStrokes(bool networkSync = true)
    {
        if (networkSync)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationStrokeClear());
        }

        canvas.Clear();
    }

    /// <summary>
    /// <c>CreateBookMark</c> instantiates a colorful replica of the panel at its position and orientation at the moment of this method call.
    /// </summary>
    public void CreateBookMark()
    {
        // only create bookmarks if they are currently set to be visible
        if (bookMarksVisible)
        {
            // tell all clients to also create the bookmark
            NetworkManager.Instance?.SendMessage(new MessageBookmarkCreate(transform.localPosition, transform.localScale, transform.localRotation));

            // create a bookmark at the current position
            CreateBookmarkAtPosition(transform.localPosition, transform.localScale, transform.localRotation);
        }
    }

    /// <summary>
    /// <c>DeleteBookMark</c> checks if the plane is within a reasonable distance from the targeted bookmark and deletes the bookmark if that is true.
    /// </summary>
    public bool DeleteBookMark()
    {
        // only try to delete a bookmark if they are set to be visible
        if (bookMarksVisible)
        {
            // iterate over bookmarks, check if any is close to the current position of the tablet
            for (int i = 0; i < bookMarks.Count; i++)
            {
                GameObject bookMark = bookMarks[i];
                if (checkPlaneDistance(bookMark))
                {
                    // tell other clients to also delete the bookmark with the index i
                    NetworkManager.Instance?.SendMessage(new MessageBookmarkDelete(i));

                    // remove the bookmark
                    Destroy(bookMark);
                    bookMarks.Remove(bookMark);

                    // we only delete one bookmark
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// <c>FreezeSlicingPlane</c> freezes slicing plane in place.
    /// </summary>
    public void FreezeSlicingPlane(bool networkSync = true)
    {
        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageSlicingFreeze(true));
        }

        slicingPlane.GetComponent<SlicingPanel>().freeze = true;
    }

    /// <summary>
    /// <c>UnfreezeSlicingPlane</c> unfreezes slicing plane.
    /// </summary>
    public void UnfreezeSlicingPlane(bool networkSync = true)
    {
        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageSlicingFreeze(false));
        }

        slicingPlane.GetComponent<SlicingPanel>().freeze = false;
    }

    // MOVE
    /// <summary>
    /// <c>PickUp</c> fixes the volume to the panel.
    /// </summary>
    public void PickUp(bool networkSync = true)
    {
        int id = model.GetComponent<ModelInteractable>().modelID;
        Debug.Log("Picked up Model-ID: "+id);

        if (networkSync)
        {
            NetworkManager.Instance?.SendMessage(new MessageVolumePickup(id));
        }

        inputHandler.Mode = InputHandler.Modi.PickUp;
        model.GetComponent<ModelInteractable>().selected = true;
        UnityEngine.Debug.Log("Pickup activated");
    }
  
    /// <summary>
    /// <c>Release</c> unparents and detaches the volume from the panel.
    /// </summary>
    public void Release(bool networkSync = true)
    {
        if (model != null) {
            int id = model.GetComponent<ModelInteractable>().modelID;
            Debug.Log("Released up Model-ID: " + id);

            if (networkSync)
            {
                NetworkManager.Instance?.SendMessage(new MessageVolumeRelease(id));
            }

            //volume.transform.parent = null;
            inputHandler.Mode = InputHandler.Modi.DrawOnTablet;
            model.GetComponent<ModelInteractable>().selected = false;
            model = null;
            UnityEngine.Debug.Log("Pickup deactivated");            
        }
    }

    /// <summary>
    /// <c>ReleaseAnnotation</c> creates an invisible "holder" panel and parents all the lineobjects to it, so that they can be released into the scene.
    /// Since the annotation holder is equipped with the appropriate scripts and functionality, it can then be manipulated through hand gestures (move, rotate...)
    /// </summary>
    public void ReleaseAnnotation()
    {
        Vector3 position = transform.localPosition;
        // planes are 10 units in worldscale
        Vector3 scale = new Vector3(transform.lossyScale.x * 10, /* transform.lossyScale.y*/ 0, transform.lossyScale.z * 10);
        Quaternion rotation = transform.localRotation;

        // tell other clients to also release annotations at current position
        NetworkManager.Instance?.SendMessage(new MessageAnnotationRelease(position, scale, rotation));

        // release annotations at the current position
        ReleaseAnnotationAtPosition(position, scale, rotation);
    }

    private void ReleaseAnnotationAtPosition(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        if (canvas)
        {
            canvas.DetachAnnotation(position, scale, rotation);
        }
    }

    /// <summary>
    /// <c>RotateScaleOnWindow</c> identifies whether a scaling or rotating interaction is taking place when in Window mode and executes this interaction.
    /// </summary>
    public void RotateScaleOnWindow(TouchEvent touchEvent)
    {
        TouchState touch = touchEvent.Touch;
        float speed;
        float localAngle = model.transform.localEulerAngles.y;
        Vector3 localScale = model.transform.localScale;

        Debug.Log(touchInteractions.gestures.LastOrDefault());
        //scaling
        if (touchInteractions.gestures.LastOrDefault() == TouchInteractions.TouchGestures.Hold || inputHandler.scaling)
        {
            speed = 0.0009f;
            localScale.x += speed * touch.delta.x;
            localScale.y += speed * touch.delta.x;
            localScale.z += speed * touch.delta.x;
            touchInteractions.gestures.Add(TouchInteractions.TouchGestures.Scale);
            inputHandler.scaling = true;
        }
        //rotation
        else
        {
            speed = 0.09f;
            localAngle -= speed * touch.delta.x;
            touchInteractions.gestures.Add(TouchInteractions.TouchGestures.Rotate);
        }

        // set new values
        SetVolumeScaleRotation(localScale, localAngle, model.GetComponent<ModelInteractable>().modelID);
    }

    /// <summary>
    /// <c>ShowCTScan</c> Creates a plane displaying the CT Scan slice at the position and orientation of the Slicing Panel.
    /// </summary>
    public void ShowCTScan(bool networkSync = true)
    {
        if (!cTscanVisible)
        {
            ctScanPlane = GameObject.Instantiate(Resources.Load<GameObject>("ctScanPlane"));
            ctScanPlane.transform.parent = model.transform.GetChild(0).transform;
            ctScanPlane.transform.localScale = new Vector3(0.1115f, 0.1f, 0.0625f);
            ctScanPlane.GetComponent<CTScanPlane>().SetUp(this.gameObject, model.transform.GetChild(0).gameObject);
            cTscanVisible = true;
        }
        else
        {
            Destroy(ctScanPlane);
            cTscanVisible = false;
        }

        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageSlicingCTMode(cTscanVisible));
        }
    }

    /// <summary>
    /// <c>ToggleBookmarkVisibility</c> toggles the visibility of all the bookmarks in the scene.
    /// When invisible, no new bookmarks can be created.
    /// </summary>
    public void ToggleBookmarkVisibility(bool networkSync = true)
    {
        foreach (GameObject bookMark in bookMarks)
        {
            bookMark.GetComponent<MeshRenderer>().enabled = !bookMark.GetComponent<MeshRenderer>().enabled;
        }
        bookMarksVisible = !bookMarksVisible;

        if (networkSync == true)
        {
            NetworkManager.Instance?.SendMessage(new MessageBookmarkVisibility(bookMarksVisible));
        }
    }

    /// <summary>
    /// Creates a new bookmark at the given position. The caller is responsible to check if the position is valid.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="scale"></param>
    /// <param name="rotation"></param>
    private void CreateBookmarkAtPosition(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        GameObject bookMark = GameObject.Instantiate(Resources.Load<GameObject>("bookMark"));
        bookMark.transform.parent = transform.parent;
        bookMark.transform.localScale = scale;
        bookMark.transform.localPosition = position;
        bookMark.transform.localRotation = rotation;
        bookMark.transform.SetParent(model.transform, true);

        bookMarks.Add(bookMark);
    }

    /// <summary>
    /// Checks if a given object is close to the current tablet position. Both distance and angle are checked.
    /// </summary>
    /// <param name="bookMark"></param>
    /// <returns></returns>
    private bool checkPlaneDistance(GameObject bookMark)
    {
        float posDistance = Vector3.Distance(transform.position, bookMark.transform.position);
        //Debug.Log("Pos Distance: "+ posDistance);
        float rotDistance = Quaternion.Angle(transform.rotation, bookMark.transform.rotation);
        //Debug.Log("Rot Distance: " + rotDistance);

        // Debug.Log(posDistance);

        //Debug.Log(rotDistance);
        if (posDistance < 0.02 && rotDistance < 3)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// <c>ShowCTScan</c> Creates a plane displaying the CT Scan slice at the position and orientation of the Slicing Panel.
    /// </summary>
    private void CreateSlicingPlane()
    {
        if (inputHandler.Mode != InputHandler.Modi.InSitu)
        {
            inputHandler.Mode = InputHandler.Modi.Slice;
            //Debug.Log("INPUT MODE: " + InputHandler.Modi.Slice);
            slicingPlane = GameObject.Instantiate(Resources.Load<GameObject>("slicingPlane"));
            slicingPlane.transform.parent = model.transform.GetChild(0).transform;
            slicingPlane.GetComponent<SlicingPanel>().SetUp(this.gameObject, model.transform.GetChild(0).gameObject);
        }
    }

    private bool IsCollisionFromBottom(GameObject colmodel, GameObject plane)
    {
        // https://answers.unity.com/questions/339532/how-can-i-detect-which-side-of-a-box-i-collided-wi.html
        RaycastHit MyRayHit;
        Vector3 direction = (colmodel.transform.position - plane.transform.position).normalized;
        Ray MyRay = new Ray(plane.transform.position, direction);

        Debug.DrawRay(plane.transform.position, direction, Color.red);

        Debug.Log("Is Collision from bottom?");
        // Debug.Log("Volumen neue Position: " + volume.transform.position);
        if (Physics.Raycast(MyRay, out MyRayHit))
        {
            Debug.Log("Raycast entered");
            if (MyRayHit.collider != null)
            {
                Debug.Log("Raycast hit collider");
                Vector3 MyNormal = MyRayHit.normal;
                Debug.DrawRay(plane.transform.position, MyNormal, Color.red);

                // Volume is rotated by 90 degrees, so forward == down
                if (isVolumeRendering && colmodel.transform.eulerAngles.x == 90)
                {
                    if (MyNormal == MyRayHit.transform.forward) { Debug.Log("Normal transform forward!"); return true; }
                }
                else
                {
                    if (MyNormal == -MyRayHit.transform.up) { Debug.Log("Normal transform down!"); return true; }
                }                
            }
        }
        return false;
    }

    private Task OnRemoteAnnotationInSituMode(MessageContainer container)
    {
        Debug.Log("From Remote: ANNOTATION_INSITUMODE");
        var message = MessageAnnotationInSituMode.Unpack(container);
        if (message != null)
        {
            if (message.InSituMode == true)
            {
                ActivateInsituAnnotations(false);
            }
            else if (message.InSituMode == false)
            {
                DeactivateInsituAnnotations(false);
            }
        }
        return Task.CompletedTask;
    }

    private Task OnRemoteVolumePickup(MessageContainer arg)
    {
        var message = MessageVolumePickup.Unpack(arg);
        Debug.Log("From Remote: VOLUME_PICKUP");

        // compares all object ids with the one sent by a remote computer and picksup respective object
        var objs = FindObjectsOfType<ModelInteractable>();
        foreach (ModelInteractable obj in objs)
        {
            if (obj.modelID == message.id)
            {
                model = obj.transform.gameObject;             
                PickUp(false);
            }
        }        
        return Task.CompletedTask;
    }

    private Task OnRemoteVolumeRelease(MessageContainer arg)
    {
        Debug.Log("From Remote: VOLUME_RELEASE");
        Release(false);
        return Task.CompletedTask;
    }

    private Task OnRemoteVolumeScaleRotate(MessageContainer container)
    {
        Debug.Log("From Remote: VOLUME_SCALEROTATE");
        var message = MessageVolumeScaleRotate.Unpack(container);
        if (message != null)
        {
            SetVolumeScaleRotation(message.Scale, message.Angle, message.modelId, false);
        }

        return Task.CompletedTask;
    }

    // ---------  COLLIDER EVENTS
    /// <summary>
    /// <c>OnTriggerEnter</c>: If the panel collides with the volume, it needs to be determined if the volume is supposed to be picked up or sliced
    /// </summary>
    private void OnTriggerEnter(Collider col)
    {
        // Debug.Log("Collision");
        if(col.transform.GetComponent<OnDisplayInteraction>()){
            // Contact AR
            inputHandler.Mode = InputHandler.Modi.Select;
            contactAR = true;
            contactObject = col.transform.gameObject;
            EnableContactSelectionMode(true);
        }
        else if (inputHandler.Mode != InputHandler.Modi.InSitu && col.transform.GetComponent<ModelInteractable>())
        {

            model = col.gameObject;

            // make sure 
            if (model.GetComponent<ModelInteractable>() != null)
            {
                model.GetComponent<ModelInteractable>().selected = true;
            }
            
           //UnityEngine.Debug.Log("Triggerentered");
            if (IsCollisionFromBottom(model, this.gameObject))
            {
               // UnityEngine.Debug.Log("Collision from bottom");
                if (model.GetComponent<ModelInteractable>() != null)
                {
                    PickUp();
                }
            }
            else
            {
                if (inputHandler.Mode != InputHandler.Modi.PickUp)
                {
                    inputHandler.Mode = InputHandler.Modi.Slice;

                    if (isVolumeRendering)
                    {
                        if (GameObject.Find("SlicingPlane(Clone)") == null)
                        {
                            CreateSlicingPlane();
                        }
                    } else
                    {
                        if (model != physModel)
                        {
                            var uiElem = model.transform.GetChild(0).GetComponent<IUiElement>();

                            if (uiElem != null)
                            {
                                Debug.Log(uiElem.GetType().Name);
                                switch (uiElem.GetType().Name)
                                {
                                    case nameof(u2vis.BarChart3D_Interaction):
                                        model.transform.GetChild(0).GetComponent<MeshRenderer>().material = barChartClipMat;
                                        this.transform.GetComponent<ClippingPlane>().material = barChartClipMat;
                                        break;
                                    case nameof(u2vis.ARGH.ScatterPlotInteraction):
                                        //TODO: Anpassen
                                        model.transform.GetChild(0).GetComponent<MeshRenderer>().material = scatterPlotCubeClipMat;
                                        this.transform.GetComponent<ClippingPlane>().material = scatterPlotCubeClipMat;
                                        break;
                                }
                            }
                        }
                        
                    }                    

                //volume.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetFloat("_isSlicing", 1f);+
                /*if (!volume.transform.GetChild(0).GetComponent<MeshRenderer>().material.IsKeywordEnabled("_ISSLICING_ON"))
                {
                    volume.transform.GetChild(0).GetComponent<MeshRenderer>().material.EnableKeyword("_ISSLICING_ON");
                    volume.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetFloat("_isSlicing", 1f);
                }*/

            }
        }
        }
    }

    /// <summary>
    /// <c>OnTriggerEnter</c>: If the panel is removed from inside the volume, slicing and CT Scan planes need to be destroyed.
    /// A Slicing plane is only kept in place if the volume is frozen.
    /// </summary>
    private void OnTriggerExit(Collider col)
    {
        if (inputHandler.Mode != InputHandler.Modi.InSitu && inputHandler.Mode != InputHandler.Modi.PickUp)
        {
            //model = col.gameObject;

            if (col.transform.GetComponent<OnDisplayInteraction>())
            {
                contactAR = false;
                DisableContactSelectionMode();
                Debug.Log("Removed from 2D BarChart");

            }
            inputHandler.Mode = InputHandler.Modi.DrawOnTablet;

            if (model != null)
            {
                if (model.GetComponent<ModelInteractable>() != null)
                {
                    model.GetComponent<ModelInteractable>().selected = false;
                }


                if (isVolumeRendering)
                {
                    if (ctScanPlane != null)
                    {
                        Destroy(ctScanPlane);
                        cTscanVisible = false;
                    }

                    if (slicingPlane != null && !slicingPlane.GetComponent<SlicingPanel>().freeze)
                    {
                        Destroy(slicingPlane);
                    }
                }
                else
                {
                    if (model != physModel)
                    {
                        var uiElem = model.transform.GetChild(0).GetComponent<IUiElement>();

                        if (uiElem != null)
                        {

                            Debug.Log(uiElem.GetType().Name);
                            switch (uiElem.GetType().Name)
                            {
                                case nameof(u2vis.BarChart3D_Interaction):
                                    model.transform.GetChild(0).GetComponent<MeshRenderer>().material = barChartMat;
                                    break;
                                case nameof(u2vis.ARGH.ScatterPlotInteraction):
                                    model.transform.GetChild(0).GetComponent<MeshRenderer>().material = scatterPlotCubeMat;
                                    break;
                            }
                        }
                    }
                }
            }
            model = null;
        }
        else if (inputHandler.Mode == InputHandler.Modi.InSitu && contactAR == true)
        {
            Debug.Log("Exit");
            contactAR = false;
            DisableContactSelectionMode();
            DeactivateInsituAnnotations();
            inputHandler.Mode = InputHandler.Modi.DrawOnTablet;
        }
    }

    private void SetVolumeScaleRotation(Vector3 localScale, float rotationAngle, int modelId, bool networkSync = true)
    {
        if (networkSync)
        {
            NetworkManager.Instance?.SendMessage(new MessageVolumeScaleRotate(localScale, rotationAngle, modelId));
        }

        var objs = FindObjectsOfType<ModelInteractable>();
        foreach (ModelInteractable obj in objs)
        {
            if (obj.modelID == modelId)
            {
                model = obj.transform.gameObject;

                model.transform.localScale = localScale;

                Vector3 angles = model.transform.localEulerAngles;
                angles.y = rotationAngle;
                model.transform.localEulerAngles = angles;
            }
        }        
    }

    private void Start()
    {
        bookMarks = new List<GameObject>();

        barChartClipMat = (Material)Resources.Load("Materials/BarChartMat_Clip", typeof(Material));
        barChartMat = (Material)Resources.Load("Materials/DefaultBarChartMat", typeof(Material));
        scatterPlotCubeMat = (Material)Resources.Load("Materials/ScatterplotCubeMaterial", typeof(Material));
        scatterPlotCubeClipMat = (Material)Resources.Load("Materials/ScatterplotCubeMaterial_Clip", typeof(Material));


        // Register message handlers
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.VOLUME_PICKUP, OnRemoteVolumePickup);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.VOLUME_RELEASE, OnRemoteVolumeRelease);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.VOLUME_SCALEROTATE, OnRemoteVolumeScaleRotate);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_INSITUMODE, OnRemoteAnnotationInSituMode);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_INSITU_PARAMETER, OnRemoteAnnotationInSituProjectionParameter);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_RELEASE, OnRemoteAnnotationRelease);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_UPDATE, OnRemoteAnnotationUpdate);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_STROKE_UPDATE, OnRemoteAnnotationStrokeUpdate);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_STROKE_UNDO, OnRemoteAnnotationStrokeUndo);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.ANNOTATION_STROKE_CLEAR, OnRemoteAnnotationStrokeClear);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.BOOKMARK_CREATE, OnRemoteBookmarkCreate);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.BOOKMARK_DELETE, OnRemoteBookmarkDelete);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.BOOKMARK_VISIBILITY, OnRemoteBookmarkVisibility);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.SLICE_FREEZE, OnRemoteSliceFreeze);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.SLICE_CTMODE, OnRemoteSliceCTMode);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.CONTACT_SELECTION_ENABLE, onRemoteContactSelectionMode);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.CONTACT_SELECTION, onRemoteContactSelect);
        //NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.SLICE_CTMODE, OnRemoteSliceCTMode);
        NetworkManager.Instance?.RegisterMessageHandler(MessageContainer.MessageType.MODEL_SELECTION, onRemoteWindowModelSelect);


    }

    private Task OnRemoteAnnotationInSituProjectionParameter(MessageContainer container)
    {
        var message = MessageAnnotationInSituProjectionParameter.Unpack(container);
        if (message != null && canvas)
        {
            canvas.ProjectionDistanceParameter = message.ProjectionParameter;
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteAnnotationUpdate(MessageContainer container)
    {
        var message = MessageAnnotationUpdate.Unpack(container);
        if (message != null && canvas)
        {
            if (canvas.DetachedAnnotations.Count >= message.Id)
            {
                foreach(var annotation in canvas.DetachedAnnotations)
                {
                    if (annotation.Id == message.Id)
                    {
                        annotation.UpdatePose(message.Position, message.Orientation);
                        break;
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteAnnotationStrokeClear(MessageContainer container)
    {
        var message = MessageAnnotationStrokeClear.Unpack(container);
        if (message != null)
        {
            ClearStrokes(false);
        }

        return Task.CompletedTask;
    }



    private Task OnRemoteAnnotationStrokeUndo(MessageContainer container)
    {
        var message = MessageAnnotationStrokeUndo.Unpack(container);
        if (message != null)
        {
            UndoLastStroke(false);
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteAnnotationStrokeUpdate(MessageContainer container)
    {
        var message = MessageAnnotationStrokeUpdate.Unpack(container);
        if (message != null)
        {
            DrawStroke(message.Point, message.InputType, false);
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteAnnotationRelease(MessageContainer container)
    {
        var message = MessageAnnotationRelease.Unpack(container);
        if (message != null)
        {
            ReleaseAnnotationAtPosition(message.Position, message.Scale, message.Rotation);
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteSliceCTMode(MessageContainer container)
    {
        var message = MessageSlicingCTMode.Unpack(container);
        if (message != null)
        {
            if (message.CTMode != cTscanVisible)
            {
                ShowCTScan(false);
            }
        }

        return Task.CompletedTask;
        
    }

    private Task OnRemoteBookmarkVisibility(MessageContainer container)
    {
        var message = MessageBookmarkVisibility.Unpack(container);
        if (message != null)
        {
            if (message.BookmarksVisible != bookMarksVisible)
            {
                ToggleBookmarkVisibility(false);
            }
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteSliceFreeze(MessageContainer container)
    {
        var message = MessageSlicingFreeze.Unpack(container);
        if (message != null)
        {
            if (message.SliceFrozen == true)
            {
                FreezeSlicingPlane(false);
            }
            else
            {
                UnfreezeSlicingPlane(false);
            }
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteBookmarkCreate(MessageContainer container)
    {
        var message = MessageBookmarkCreate.Unpack(container);
        if (message != null)
        {
            CreateBookmarkAtPosition(message.Position, message.Scale, message.Rotation);
        }

        return Task.CompletedTask;
    }

    private Task OnRemoteBookmarkDelete(MessageContainer container)
    {
        var message = MessageBookmarkDelete.Unpack(container);
        if (message != null)
        {
            if (bookMarks.Count > message.index)
            {
                var bookmark = bookMarks[message.index];
                Destroy(bookmark);
                bookMarks.Remove(bookmark);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// <c>Update</c> listens for keyboard input to a show CT scan if desired.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            DebugMode = !DebugMode;
        }

        //Debug.Log(inputHandler.Mode);
        // Debug.Log(inputHandler.Mode);
        if (Keyboard.current[Key.C].wasPressedThisFrame && inputHandler.Mode == InputHandler.Modi.Slice)
        {
            //Debug.Log("C Pressed, Showing CTScan");
            ShowCTScan();
        }

        if (Keyboard.current[Key.P].wasPressedThisFrame && inputHandler.Mode == InputHandler.Modi.PickUp)
        {
            Release();
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (inputHandler.Mode != InputHandler.Modi.InSitu)
            {
                ActivateInsituAnnotations(true);
            }
            else
            {
                DeactivateInsituAnnotations(true);
            }            
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetInsituAnnotationProjectionParameter(1.0f);
        }

        if (Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            SetInsituAnnotationProjectionParameter(0.0f);
        }

        if (DebugMode)
        {
            var mesh = GetComponent<MeshRenderer>();
            mesh.enabled = true;
        }
        else {
            var mesh = GetComponent<MeshRenderer>();
            mesh.enabled = false;
        }
    }

    public void EnableContactSelectionMode(bool networkSync = true)
    {
        if (networkSync)
        {
            int ID = contactObject.GetComponent<OnDisplayInteraction>().twoDmodelID;
            NetworkManager.Instance?.SendMessage(new MessageContactAREnableSelection(true, ID));
        }

        Debug.Log("Selection Mode enabled");
        inputHandler.Mode = InputHandler.Modi.Select;
        contactButton.gameObject.SetActive(true);
        //contactObject.GetComponent<OnDisplayInteraction>().enableSelectionMode();
    }

    public Task onRemoteContactSelectionMode(MessageContainer container)
    {
        var message = MessageContactAREnableSelection.Unpack(container);
        if (message != null)
        {
            if (message.selection == true)
            {
                var objs = FindObjectsOfType<OnDisplayInteraction>();
                foreach (OnDisplayInteraction obj in objs)
                {
                    if (obj.twoDmodelID == message.modelID)
                    {
                        contactObject = obj.transform.gameObject;
                    }
                }
                EnableContactSelectionMode(false);                
            }
            else
            {
                contactObject = null;
                DisableContactSelectionMode(false);
            }
        }

        return Task.CompletedTask;
    }

    public void DisableContactSelectionMode(bool networkSync = true)
    {
        if (networkSync)
        {
            NetworkManager.Instance?.SendMessage(new MessageContactAREnableSelection(false));
        }

        Debug.Log("Selection Mode disabled");
        contactButton.gameObject.SetActive(false);
        //contactObject.GetComponent<OnDisplayInteraction>().enableSelectionMode();
    }

    public void ContactSelect(Vector2 touchPosition, bool networkSync = true)
    {
        if (networkSync)
        {
           // Debug.Log("Sending selection ... ");
            NetworkManager.Instance?.SendMessage(new MessageContactARSelection(touchPosition));
           // Debug.Log("Sent!");
        }

        contactObject.GetComponent<OnDisplayInteraction>().select(touchPosition); 

        Debug.Log("Something was selected");
    }

    public Task onRemoteContactSelect(MessageContainer container)
    {
        var message = MessageContactARSelection.Unpack(container);

        if (message != null)
        {
            contactObject.GetComponent<OnDisplayInteraction>().select(new Vector2(message.touchX, message.touchY));
        }
        return Task.CompletedTask;
    }

    /* public void EnableContactDrawingMode()
     {
         Debug.Log("Drawing Mode enabled");
         inputHandler.Mode = InputHandler.Modi.InSitu;
         contactObject.GetComponent<OnDisplayInteraction>().enableDrawingMode();
     }

     public void ContactARSelect(TouchEvent touchEvent)
     {
         contactObject.GetComponent<OnDisplayInteraction>().select(touchEvent);
     }*/

    public void windowModelSelect(Vector2 touchPosition, int modelID, bool networkSync = true)
    {
        if (networkSync)
        {
           // Debug.Log("Sending selection ... ");
            NetworkManager.Instance?.SendMessage(new MessageWindowModelSelection(touchPosition, modelID));
           // Debug.Log("Sent!");
        }

        model.GetComponent<ModelInteractable>().select(touchPosition);

    }

    public Task onRemoteWindowModelSelect(MessageContainer container)
    {
        Debug.Log("Empfangen!");
        var message = MessageWindowModelSelection.Unpack(container);

        if (message != null)
        {
            var objs = FindObjectsOfType<ModelInteractable>();
            foreach (ModelInteractable obj in objs)
            {
                if (obj.modelID == message.modelID)
                {
                    model = obj.transform.gameObject;
                }
            }

            windowModelSelect(new Vector2(message.touchX, message.touchY), model.GetComponent<ModelInteractable>().modelID,false);
        }
        return Task.CompletedTask;
    }

}
