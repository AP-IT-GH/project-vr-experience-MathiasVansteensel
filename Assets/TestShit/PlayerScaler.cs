using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is berre's code originally tho, i changed it :)
namespace MathiasCode
{
    public class PlayerScaler : MonoBehaviour
    {
        // Start is called before the first frame update
        [Header("Variables")]
        public float shrinkFactor = 0.2f;

        [Header("Scale Sets")]
        private Vector3 originalScale;
        private Vector3 shrunkenScale;
        private float originalJumpForce;
        private float shrunkenJumpForce;
        private float originalMass;
        private float shrunkenMass;
        private bool isShrunken = false;

        private void Start()
        {
            originalMass = PlayerController.LocalPlayer.mass;
            //originalJumpForce = PlayerController.LocalPlayer.jumpForce;

            shrunkenScale = originalScale * shrinkFactor;
            shrunkenMass = originalMass * shrinkFactor;
            shrunkenJumpForce = originalJumpForce * shrinkFactor;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) ToggleShrink();
        }

        public void ToggleShrink()
        {
            isShrunken = !isShrunken;
            PlayerController.LocalPlayer.playerScale = isShrunken ? shrinkFactor : 1f;
            PlayerController.LocalPlayer.jumpForce = isShrunken ? shrunkenJumpForce : originalJumpForce;
            PlayerController.LocalPlayer.mass = isShrunken ? shrunkenMass : originalMass;
        }
    }
}
