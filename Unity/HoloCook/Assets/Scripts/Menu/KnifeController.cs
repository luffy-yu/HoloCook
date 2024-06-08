using System;
using UnityEngine;

namespace HoloCook.Menu
{
    public class KnifeController: MonoBehaviour
    {
        public GameObject bananaSlices;

        public float movingThreshold = 150f;

        [HideInInspector] public bool canDisappear = false;
        
        private void Start()
        {
            canDisappear = false;
            EnableBananaSlices(false);
        }

        public void EnableBananaSlices(bool enable)
        {
            bananaSlices.SetActive(enable);
        }
    }
}