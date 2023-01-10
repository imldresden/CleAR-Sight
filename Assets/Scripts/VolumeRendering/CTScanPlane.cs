// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace UnityVolumeRendering
{
    [ExecuteInEditMode]
    public class CTScanPlane : MonoBehaviour
    {
       
        private MeshRenderer meshRenderer;

        [HideInInspector]
        public VolumeDataset dataset;
        [HideInInspector]
        public TransferFunction transferFunction;
       
        private GameObject volume;
        private GameObject panel;
        

        public void SetUp(GameObject panel_, GameObject volume_)
        {            
            panel = panel_;
            volume = volume_;
            dataset = volume.GetComponent<VolumeRenderedObject>().dataset;
            transferFunction = volume.GetComponent<VolumeRenderedObject>().transferFunction;            

            meshRenderer = GetComponent<MeshRenderer>();
            

            Material sliceMat = GetComponent<MeshRenderer>().sharedMaterial;
            sliceMat.SetTexture("_DataTex", dataset.GetDataTexture());
            sliceMat.SetTexture("_TFTex", transferFunction.GetTexture());
        }

        /// <summary>
        /// In this <c>Update</c> function, the material of the CT Panel is constantly updated, depending on the position of the panel.
        /// Through this, the appropriate CT Scan slices are displayed at any given time.
        /// </summary>
        private void Update()
        {
            if (panel != null && volume != null) {
                
                Vector3 pos = panel.transform.position;

                transform.position = new Vector3(pos.x, pos.y, pos.z);
                transform.rotation = panel.transform.rotation;

                meshRenderer.sharedMaterial.SetMatrix("_parentInverseMat", transform.parent.worldToLocalMatrix);
                meshRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(transform.position, transform.rotation, transform.parent.lossyScale));
            }
        }
    }
}
