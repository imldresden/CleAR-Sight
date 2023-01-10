// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;
public class VolumeManager : MonoBehaviour
{

    public string VolumeFilePath;

    // Start is called before the first frame update
    void Start()
    {
        OpenRAWDatasetResult(VolumeFilePath);
    }

    private void OpenRAWDatasetResult(string filePath)
    {
        if (filePath != null)
        {
            filePath = System.IO.Path.GetFullPath(filePath);

            // Did the user try to import an .ini-file? Open the corresponding .raw file instead
            if (System.IO.Path.GetExtension(filePath) == ".ini")
                filePath = filePath.Replace(".ini", ".raw");

            // Parse .ini file
            DatasetIniData initData = DatasetIniReader.ParseIniFile(filePath + ".ini");
            if (initData != null)
            {
                // Import the dataset
                RawDatasetImporter importer = new RawDatasetImporter(filePath, initData.dimX, initData.dimY, initData.dimZ, initData.format, initData.endianness, initData.bytesToSkip);
                VolumeDataset dataset = importer.Import();
                // Spawn the object
                if (dataset != null)
                {
                    VolumeObjectFactory.CreateObject(dataset, gameObject);
                }
            }
        }
    }
}
