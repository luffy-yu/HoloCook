//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using HoloCook.Menu;
using HoloCook.Utility;
using UnityEngine;

namespace HoloCook.HoloLens2
{
    public class PlaneCollisionDetector : MonoBehaviour
    {
        [Header("Hand Tracker")] public HandPalmTracker handPalmTracker;

        private Renderer _renderer;

        private List<string> bottles = new List<string>();

        // Start is called before the first frame update
        void Start()
        {
            _renderer = GetComponent<Renderer>();

            bottles.Add(Menu.Static.GetName(Menu.Static.Stuff.Oil));
            bottles.Add(Menu.Static.GetName(Menu.Static.Stuff.Bowl));
            bottles.Add(Menu.Static.GetName(Menu.Static.Stuff.Beer));
            bottles.Add(Menu.Static.GetName(Menu.Static.Stuff.Rum));
            bottles.Add(Menu.Static.GetName(Menu.Static.Stuff.LimeJuice));
        }

        // Update is called once per frame
        void Update()
        {
            if (!handPalmTracker.CanAttach(Handness.Left))
            {
                var attached = handPalmTracker.GetAttached(Handness.Left);
                
                // return if it's null
                if(attached == null) return;

                if (!attached.name.Equals(Menu.Static.GetName(Menu.Static.Stuff.Pan))) return;

                // check if pan can drop
                if (!attached.GetComponent<PanGrabbable>().canDrop) return;

                var bcs = attached.GetComponents<BoxCollider>();
                foreach (var bc in bcs)
                {
                    if (bc.bounds.Intersects(_renderer.bounds))
                    {
                        handPalmTracker.DetachObject(false, Handness.Left);
                        // deactivate plate, pancake is done
                        handPalmTracker.SendDeactivateMessage(Menu.Static.Stuff.Plate);
                        break;
                    }
                }
            }

            if (!handPalmTracker.CanAttach(Handness.Right))
            {
                var attached = handPalmTracker.GetAttached(Handness.Right);

                var name = attached.name;

                if (!bottles.Contains(name)) return;

                var bcs = attached.GetComponents<BoxCollider>();
                // handle bottles: oil, beer, rum, limejuice

                foreach (var bc in bcs)
                {
                    if (bc.bounds.Intersects(_renderer.bounds))
                    {
                        handPalmTracker.DetachObject(false, Handness.Right);
                        return;
                    }
                }
            }
        }

        float GetPlaneY()
        {
            var bounds = GetComponent<Renderer>().bounds;
            var m = bounds.max;
            return m.y + 0.002f;
        }

        private void OnTriggerEnter(Collider other)
        {
            var go = other.gameObject;
            var name = other.gameObject.name;

            // right hand entered for small objects
            if (name.StartsWith(Handness.Right.ToString()))
            {
                var attached = handPalmTracker.GetAttached(Handness.Right);
                if (attached != null)
                {
                    go = attached;
                    name = attached.name;
                }
            }


            // knife
            if (name.Equals(Menu.Static.GetName(Menu.Static.Stuff.Knife)))
            {
                // don't drop knife
                if (!go.GetComponent<KnifeController>().canDisappear)
                {
                    return;
                }
            }

            // small cup
            if (name.Equals(Menu.Static.GetName(Menu.Static.Stuff.SmallCup)))
            {
                // restore small cup
                var scg = go.GetComponent<SmallCupGrabbable>();
                if (scg.CanDisappear())
                {
                    handPalmTracker.DetachObject(false, Handness.Right);
                    // all done
                    // deactivate big cup
                    handPalmTracker.SendDeactivateMessage(Menu.Static.Stuff.BigCup);
                }
                else
                {
                    // restore transform
                    scg.RestoreTransform();
                    // make it disappear
                    handPalmTracker.DetachObject(false, Handness.Right);
                    // enable big cup
                    handPalmTracker.SendActivateMessage(Menu.Static.Stuff.BigCup);
                    // show ice bowl after lime is poured
                    if (scg.limeShowed && scg.limePoured)
                    {
                        // enable ice-related
                        handPalmTracker.SendActivateMessage(Menu.Static.Stuff.Bowl);
                    }
                }
            }

            // release
            handPalmTracker.DetachObject(false, Handness.Right);
            if (handPalmTracker.collisionDetector != null)
            {
                handPalmTracker.collisionDetector.ReleaseFromHand();
            }
        }
    }
}