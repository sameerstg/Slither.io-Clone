using System.Collections;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayFabScripts
{
    /// <summary>
    /// A script that will be used to log the player into the playFab back-end
    /// </summary>
    public class PlayFabLoginScript : MonoBehaviour
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

        [Header("Variables for logging in")]
        //Error message text
        [SerializeField] private Text errorText;
        //Error message gameObject
        [SerializeField] private GameObject errorGameObject;

        //References to other screens
        [SerializeField] private GameObject loginOrRegisterPanel;
        [SerializeField] private GameObject loginToAccountPanel;
        
        //Button references
        [SerializeField] private Button submitButtonRef, backButtonRef;

        //Variables for logging in
        private string _userEmail, _userPassword;
        
        /// <summary>
        /// Logs the player in
        /// </summary>
        public void LoginRequestFunction()
        {
            //If string is empty
            if (string.IsNullOrEmpty(_userEmail))
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid email"));
                return;
            }
            
            //If string does not have @
            if (_userEmail.Contains('@') == false)
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid mail"));
                return;
            }

            if (string.IsNullOrEmpty(_userPassword))
            {
                StartCoroutine(DisplayErrorMessage("Please enter a valid password"));
                return;
            }
            
            var request = new LoginWithEmailAddressRequest
            {
                Email = _userEmail,
                Password = _userPassword
            };
            
            PlayFabClientAPI.LoginWithEmailAddress(request,
                OnLoginSuccess, OnLoginFailure);
            
            submitButtonRef.interactable = false;
            backButtonRef.interactable = false;
        }
        
        public void BackButton()
        {
            loginOrRegisterPanel.SetActive(true);
            loginToAccountPanel.SetActive(false);
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

        private void OnLoginSuccess(LoginResult result)
        {
            Debug.Log("User has successfully logged in to the server");

            StartCoroutine(DisplayErrorMessage("Login was successful"));
            
            PlayerPrefs.SetString("PlayFabId", result.PlayFabId);
            
            //Update contact email
            ContactEmailUpdate(_userEmail, result.PlayFabId);
            
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

        private void OnLoginFailure(PlayFabError error)
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
            yield return new WaitForSeconds(2.5f);
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
        private void ContactEmailUpdate(string emailAddress, string playFabId)
        {
            print("Trying to update contact email");

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