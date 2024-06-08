//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;

namespace HoloCook.Menu
{
    public class SmallCupGrabbable : CollisionDetector
    {
        public MeshRenderer renderer;
        public TextMesh textMesh;

        private Transform camera;

        private Vector3 backupPosition;
        private Quaternion backupRotation;
        private bool backuped = false;

        [HideInInspector]public bool limeShowed = false;
        [HideInInspector]public bool rumShowed = false;
        
        [HideInInspector] public bool limePoured = false;
        [HideInInspector] public bool rumPoured = false;

        void Start()
        {
            camera = Camera.main.transform;
            textMesh.text = "";
        }

        // public override bool CanGrab()
        // {
        //     return limeShowed || rumShowed;
        // }

        public bool CanDisappear()
        {
            return limePoured && rumPoured;
        }

        public void BackupTransform()
        {
            backupPosition = transform.position;
            backupRotation = transform.rotation;
            backuped = true;
        }

        public override void TriggerEnterHandler(Collider other)
        {
            // disable the text when hand enters
            var name = other.gameObject.name;

            // hand grab
            if (name.StartsWith(Utils.GetThumbJointName(handness)))
            {
                if (CanGrab() && handPalmTracker.CanAttach(handness))
                {
                    if (!backuped)
                    {
                        backupPosition = transform.position;
                        backupRotation = transform.rotation;
                        backuped = true;
                    }
                    
                    // make text disappear
                    textMesh.text = "";

                    // attach and follow hand
                    handPalmTracker.AttachObject(gameObject, handness);
                    handPalmTracker.collisionDetector = this;
                    
                    // enable big cup
                    handPalmTracker.SendActivateMessage(Static.Stuff.BigCup);
                }
            }
        }

        void LetTextFaceCamera()
        {
            Vector3 lookDirection = camera.position - textMesh.transform.position;
            textMesh.transform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
        }

        private void Update()
        {
            if (textMesh.text != "")
            {
                LetTextFaceCamera();
            }
        }

        public void DisableText()
        {
            ShowText("", true);
        }

        public void ShowTextFor(bool lime)
        {
            if (lime)
            {
                ShowLimeText();
            }
            else
            {
                ShowRumText();
            }
        }

        void ShowLimeText()
        {
            var text = "0.75 oz";
            ShowText(text, true);
            limeShowed = true;
        }

        public void UpdatePourFlag()
        {
            if (limeShowed) limePoured = true;
            rumPoured = true;
        }

        void ShowRumText()
        {
            var text = "2 oz";
            ShowText(text, true);
            rumShowed = true;
        }

        public void ShowText(string text, bool render)
        {
            // hide mesh
            renderer.enabled = render;
            textMesh.text = text;
        }

        public void RestoreTransform()
        {
            transform.position = backupPosition;
            transform.rotation = backupRotation;
        }

        public override void ReleaseFromHand()
        {
            RestoreTransform();
        }
    }
}