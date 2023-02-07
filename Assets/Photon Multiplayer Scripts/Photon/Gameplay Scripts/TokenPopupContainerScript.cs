using System;
using UnityEngine;

namespace Photon_Multiplayer_Scripts.Photon.Gameplay_Scripts
{
    /// <summary>
    /// Token popup container script
    /// </summary>
    public class TokenPopupContainerScript : MonoBehaviour
    {
        #region Popup Text Functions

        [Header("Token popup container variables")]
        //Token popup container children array
        [SerializeField] private GameObject[] popupTextArray;
        
        //Current index for children
        private int _currentIndex = 0;

        /// <summary>
        /// Activates the next popup text when a player collects a token
        /// </summary>
        public TextMesh ActivateAndReturnNextPopupText()
        {
            //Creating TextMesh variable
            TextMesh popupTextMeshToReturn;
            
            //Try catch for popupTextArray
            try
            {
                //Enabling the text
                popupTextArray[_currentIndex].SetActive(true);
                //Popup Text Mesh to return
                popupTextMeshToReturn = popupTextArray[_currentIndex].GetComponent<TextMesh>();
            }
            catch (Exception e)
            {
                print(e.Message);
                popupTextArray[9].SetActive(true);
                //Popup Text Mesh to return
                popupTextMeshToReturn = popupTextArray[_currentIndex].GetComponent<TextMesh>();
            }
            
            //Handling the index
            _currentIndex++;
            if (_currentIndex >= popupTextArray.Length)
            {
                _currentIndex = 0;
            }

            return popupTextMeshToReturn;
        }

        /// <summary>
        /// Disables all text gameObject at the very start
        /// </summary>
        private void DisableAllTextAtStart()
        {
            foreach (GameObject o in popupTextArray)
            {
                o.SetActive(false);
            }
        }

        #endregion

        #region Unity Functions

        private void Awake()
        {
            DisableAllTextAtStart();
        }

        #endregion
    }
}