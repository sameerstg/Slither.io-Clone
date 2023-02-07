using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayFabScripts
{
    /// <summary>
    /// A script that will help the player register with Playfab
    /// </summary>
    public class RegisterCanvasScript : MonoBehaviour
    {
        #region Unity Functions

        private void OnEnable()
        {
            errorText.gameObject.SetActive(false);
            submitButtonRef.interactable = true;
            backButtonRef.interactable = true;
        }

        public void Start()
        {
            //Note: Setting title Id here can be skipped if you have set the value in Editor Extensions already.
            if (string.IsNullOrEmpty(PlayFabSettings.TitleId)){
                PlayFabSettings.TitleId = "4FB55";
            }
        }

        #endregion

        #region Login Functions

        [Header("Variables for registering in")]
        //Error message text
        [SerializeField] private Text errorText;
        //Error message text
        [SerializeField] private GameObject errorGameObject;

        [SerializeField] private GameObject loginOrRegisterPanel;
        [SerializeField] private GameObject registerPanel;
        
        //Submit button
        [SerializeField] private Button submitButtonRef, backButtonRef;
        
        //Variables for logging in
        private string _userEmail, _userPassword, _userName;

        public void BackButton()
        {
            loginOrRegisterPanel.SetActive(true);
            registerPanel.SetActive(false);
        }
        
        /// <summary>
        /// Tries to register the player in
        /// </summary>
        public void RegisterRequestFunction()
        {
            //If username is empty
            if (string.IsNullOrEmpty(_userName))
            {
                StartCoroutine(DisplayErrorMessage("Please Enter a username"));
                return;
            }
            
            //If the username is less than 6 characters
            if (_userEmail.Length <= 6)
            {
                StartCoroutine(DisplayErrorMessage("Please enter a username with more than 6 characters"));
                return;
            }
            
            //If the user email is empty
            if (string.IsNullOrEmpty(_userEmail))
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid email"));
                return;
            }

            //If the username does not contain @
            if (_userEmail.Contains('@') == false)
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid email"));
                return;
            }

            //Checking for bad password values
            if (string.IsNullOrEmpty(_userPassword) || _userPassword.Length < 6)
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid " +
                                                   "password with more than 6 characters"));
                return;
            }

            var request = new RegisterPlayFabUserRequest
            {
                Username = _userName,
                Email = _userEmail,
                Password = _userPassword,
            };
            
            PlayFabClientAPI.RegisterPlayFabUser(
                request,
                OnRegisterSuccess,
                OnRegisterFailure
            );
            
            //Turning off submit button
            submitButtonRef.interactable = false;
            backButtonRef.interactable = false;
        }
        
        /// <summary>
        /// A function that will get the username. It is called in the editor
        /// by an input field
        /// </summary>
        /// <param name="userName"></param>
        public void GetUserName(string userName)
        {
            _userName = userName;
        }
        
        /// <summary>
        /// Called in editor by input field. Used to set email value for user
        /// </summary>
        /// <param name="userEmail"></param>
        public void GetUserEmail(string userEmail)
        {
            _userEmail = userEmail;
        }

        /// <summary>
        /// Called in editor by input field, Used to set password value for user
        /// </summary>
        /// <param name="userPassword"></param>
        public void GetUserPassword(string userPassword)
        {
            _userPassword = userPassword;
        }

        private void OnRegisterSuccess(RegisterPlayFabUserResult result)
        {
            Debug.Log("User has successfully logged in to the server");
            StartCoroutine(DisplayErrorMessage("Registration was successful"));
            
            PlayerPrefs.SetString("PlayFabId", result.PlayFabId);
            
            //Updating contact email
            ContactEmailUpdate(_userEmail, result.PlayFabId, result);

            //Setting display name
            var displayNameRequest = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = _userName,
            };
            
            PlayFabClientAPI.UpdateUserTitleDisplayName(
                displayNameRequest,
                nameResult =>
                {
                    StartCoroutine(LoadNextScene());
                },
                error => {print(error.ErrorMessage);} 
            );
            
            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest(),
                statisticsResult =>
                {
                    foreach (var statistic in 
                        statisticsResult.Statistics.Where(statistic => statistic.StatisticName == "highScore"))
                    {
                        PlayerPrefs.SetInt("BestScore", statistic.Value);
                        print("Players high score on server is " + statistic.Value);
                        StartCoroutine(LoadNextScene());
                        return;
                    }
                    
                    print("No high score variable was found. So setting high score value to zero");
                    PlayerPrefs.SetInt("BestScore", 0);
                    StartCoroutine(LoadNextScene());
                },
                error => {print(error.ErrorMessage);}
            );
        }

        private void OnRegisterFailure(PlayFabError error)
        {
            print("Failed to login the player " + error.ErrorMessage);
            StartCoroutine(DisplayErrorMessage(error.ErrorMessage));
            submitButtonRef.interactable = true;
            backButtonRef.interactable = true;
        }

        /// <summary>
        /// A function that will display the text for a while
        /// </summary>
        /// <param name="textToDisplay"></param>
        /// <returns></returns>
        private IEnumerator DisplayErrorMessage(string textToDisplay)
        {
            errorGameObject.SetActive(true);
            errorText.gameObject.SetActive(true);
            errorText.text = textToDisplay;
            yield return new WaitForSeconds(2.5f);
            errorText.gameObject.SetActive(false);
            errorGameObject.SetActive(false);
        }

        /// <summary>
        /// A function that will load the next scene
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadNextScene()
        {
            yield return new WaitForSeconds(1.5f);
            SceneManager.LoadScene(2, LoadSceneMode.Single);
            
            yield break;
        }

        #endregion
        
        #region Contact Email Functions

        /// <summary>
        /// A function that updates the contact email of the player on the back-end
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <param name="playFabId"></param>
        /// <param name="loginResult"></param>
        private void ContactEmailUpdate(string emailAddress, string playFabId, RegisterPlayFabUserResult loginResult)
        {
            print("Trying to update contact email");

            PlayFabAuthenticationContext authenticationContext = new PlayFabAuthenticationContext();

            var request = new AddOrUpdateContactEmailRequest
            {
                EmailAddress = _userEmail,
            };
            
            PlayFabClientAPI.AddOrUpdateContactEmail(
                request,
                result =>
                {
                    print("Email address was successfully updated");
                },
                error =>
                {
                    print("Failed to update the contact email due to error\n" +
                          error.ErrorMessage);
                } 
            );
        }

        #endregion
    }
}