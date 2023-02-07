using System;
using UnityEngine;
using UnityEngine.UI;

namespace MenuScript
{
    /// <summary>
    /// A script that will change the skin of the player
    /// </summary>
    public class ChangeSkinScreen : MonoBehaviour
    {
        #region Change Skin Buttons

        [Header("Change Skin Buttons")]
        //Current skin image
        [SerializeField] private Image currentSkinImage;
        //Skins list
        [SerializeField] private Sprite[] skinsArray;
        
        //Back button or okay button
        [SerializeField] private Button backButton;

        /// <summary>
        /// A function that will change the skin based on the button that is clicked
        /// </summary>
        /// <param name="skinButton"></param>
        public void SkinButtonClicked(int skinButton)
        {
            //Returning if the skin button value is greater than 2
            if (skinButton > 2) return;
            
            //Changing skin by changing the player pref value
            PlayerPrefs.SetInt("skinID", skinButton + 1);
            //Changing the image on the screen
            DisplayCurrentSkin();
        }

        /// <summary>
        /// Runs when the back button is clicked
        /// </summary>
        private void BackButtonClicked()
        {
            mainMenuRef.SetActive(true);
            gameObject.SetActive(false);
        }

        #endregion

        #region Change Skin at Start

        /// <summary>
        /// A function that will display the current skin that the player has displayed
        /// </summary>
        private void DisplayCurrentSkin()
        {
            print($"Current skin is {PlayerPrefs.GetInt("skinID") - 1}");
            currentSkinImage.sprite = skinsArray[PlayerPrefs.GetInt("skinID") - 1];
        }

        #endregion

        #region Screen References

        [Header("Screen References")]
        //Main Menu Reference
        [SerializeField] private GameObject mainMenuRef;

        #endregion

        #region Unity Functions

        private void OnEnable()
        {
            //Setting up skin id
            if (PlayerPrefs.HasKey("skinID") == false)
            {
                PlayerPrefs.SetInt("skinID", 1);
            }
            
            DisplayCurrentSkin();
        }

        private void Start()
        {
            backButton.onClick.AddListener(BackButtonClicked);
        }

        #endregion
    }
}