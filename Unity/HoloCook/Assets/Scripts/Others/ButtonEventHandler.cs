//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using UnityEngine;

namespace HoloCook.Others
{
    public class ButtonEventHandler : MonoBehaviour
    {
        [Header("Reference")] public GameObject webcamSource;

//     private WebcamSource _webcamSource = null;
//     void Start()
//     {
//         if (webcamSource != null)
//         {
//             if (webcamSource.TryGetComponent<WebcamSource>(out _webcamSource))
//             {
// #if UNITY_EDITOR
//                 Debug.LogWarning("Set WebcamSource");
// #endif
//             }
//         }
//     }

        public void QuitAction()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit(0);
        }

        public void CloseVideoStreamAction()
        {
            if (webcamSource != null)
            {
                webcamSource.SetActive(false);
            }
        }

        public void OpenVideoStreamAction()
        {
            if (webcamSource != null)
            {
                webcamSource.SetActive(true);
            }
        }

        public void DisableMRC()
        {
            // if (_webcamSource != null)
            // {
            //     _webcamSource.EnableMixedRealityCapture = false;
            // }
        }

        public void EnableMRC()
        {
            // if (_webcamSource != null)
            // {
            //     _webcamSource.EnableMixedRealityCapture = true;
            // }
        }
    }
}