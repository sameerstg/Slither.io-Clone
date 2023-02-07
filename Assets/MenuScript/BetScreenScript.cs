using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

namespace MenuScript
{
    /// <summary>
    /// This script contains all the functions required for the bet screen to work properly
    /// </summary>
    public class BetScreenScript : MonoBehaviour
    {
        #region References to children and other scene objects

        [Header("References to children and other scene Objects")]
        //Main Menu panel
        [SerializeField] private GameObject mainMenuPanel;
        //Display loading image
        [SerializeField] private GameObject loadingImage;
        //Token display
        [SerializeField] private Text tokenDisplay;

        //Error text
        [SerializeField] private Text errorText;
        
        //Back button
        [SerializeField] private Button backButton;
        
        //Stores the text
        private float m_TokensInInventory;
        //Stores the bet amount
        private int m_CurrentBetAmount;

        /// <summary>
        /// Runs when the back button is clicked
        /// </summary>
        private void BackButtonClicked()
        {
            mainMenuPanel.SetActive(true);
            gameObject.SetActive(false);
        }
        
        #endregion

        #region Information and Bet Loading
        
        /// <summary>
        /// Processes the bet amount. This function will display an error text if the
        /// player tries to bet with an amount greater than that he possess
        /// </summary>
        public void ProcessBetAmount()
        {
            if (m_CurrentBetAmount > m_TokensInInventory)
            {
                print("Amount entered was too high");
                StartCoroutine(DisplayErrorText());
                return;
            }
            
            print("Amount is being processed as the amount was correct");
            //Subtracting bet amount to 
            m_TokensInInventory -= m_CurrentBetAmount;
            PlayerPrefs.SetFloat("PlayerTokens", m_TokensInInventory);
            
            //Updating player data on playfab backend
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "TokensCollected", m_TokensInInventory.ToString() }
                }
            };
            PlayFabClientAPI.UpdateUserData(
                request,
                result =>
                {
                    print("Tokens data was successfully added to the servers");
                },
                error =>
                {
                    print("Data could not be updated on the server due to " + error.ErrorMessage);
                } 
            );
            
            //Setting bet amount
            PlayerPrefs.SetInt("BetAmount", m_CurrentBetAmount);
            
            loadingImage.SetActive(true);
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Stores the bet amount as a string for processing later
        /// </summary>
        /// <param name="betAmount"></param>
        public void StoreBetAmount(string betAmount)
        {
            m_CurrentBetAmount = int.Parse(betAmount);
        }

        /// <summary>
        /// Loads the text and displays it
        /// </summary>
        private void LoadTokensInInventoryAndDisplay()
        {
            if (PlayerPrefs.HasKey("PlayerTokens") == false)
            {
                //Updating player data on playfab backend
                var request = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { "TokensCollected", "50.5" }
                    }
                };
                PlayFabClientAPI.UpdateUserData(
                    request,
                    result =>
                    {
                        print("Tokens data was successfully added to the servers");
                        PlayerPrefs.SetFloat("PlayerTokens", 50.5f);
                        LoadTokensInInventoryAndDisplay();
                    },
                    error =>
                    {
                        print("Data could not be updated on the server due to " + error.ErrorMessage);
                    } 
                );
                
                return;
            }
            
            m_TokensInInventory = PlayerPrefs.GetFloat("PlayerTokens", 55.5f);
            tokenDisplay.text = m_TokensInInventory.ToString();
            
            PlayFabClientAPI.GetUserData(
                new GetUserDataRequest(),
                result =>
                {
                    print("Successfully retrieved data from the servers for tokens");
                    float tokensFromServer = float.Parse(result.Data["TokensCollected"].Value);
                    tokenDisplay.text = tokensFromServer.ToString();
                },
                error =>
                {
                    print("Failed to get token data from servers due to error " + error.ErrorMessage);
                }
            );
        }

        private IEnumerator DisplayErrorText()
        {
            //Displaying the error text
            errorText.gameObject.SetActive(true);
            errorText.text = "Please bet with an amount lower than " + m_TokensInInventory;
            
            yield return new WaitForSeconds(2.5f);
            
            errorText.gameObject.SetActive(false);
        }

        #endregion

        #region Unity Functions

        private void Start()
        {
            backButton.onClick.AddListener(BackButtonClicked);
            
            LoadTokensInInventoryAndDisplay();
        }

        #endregion
    }
}