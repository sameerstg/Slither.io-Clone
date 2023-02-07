using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlayFabScripts
{
    /// <summary>
    /// A script for the login canvas and its control
    /// </summary>
    public class LoginCanvasScript : MonoBehaviour
    {
        [Header("Login Canvas variables")]
        //Login button
        [SerializeField] private Button loginButton;
        //Register Button
        [SerializeField] private Button registerButton;
        
        //Screen references
        [SerializeField] private GameObject loginScreenRef, registerScreenRef, mainMenuScreenRefs;

        private void OpenLoginScreen()
        {
            loginScreenRef.SetActive(true);
            mainMenuScreenRefs.SetActive(false);
        }

        private void OpenRegisterScreen()
        {
            registerScreenRef.SetActive(true);
            mainMenuScreenRefs.SetActive(false);
        }

        private void AddingButtonListeners()
        {
            loginButton.onClick.AddListener(OpenLoginScreen);
            registerButton.onClick.AddListener(OpenRegisterScreen);
        }

        private void Start()
        {
            AddingButtonListeners();
        }
    }
}