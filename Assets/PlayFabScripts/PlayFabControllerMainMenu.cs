using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

namespace PlayFabScripts
{
    /// <summary>
    /// A function that will perform some of the functions that PlayFab requires on
    /// the main menu
    /// </summary>
    public class PlayFabControllerMainMenu : MonoBehaviour
    {
        private void OnEnable()
        {
            SendHighScoreDataToPlayFab();
            SetSkinOnStartOfGame();
        }

        /// <summary>
        /// Sets the skin of the player at the very first start of the game
        /// </summary>
        private void SetSkinOnStartOfGame()
        {
            //Setting up skin id
            if (PlayerPrefs.HasKey("skinID") == false)
            {
                PlayerPrefs.SetInt("skinID", 1);
            }
        }

        /// <summary>
        /// Sends the high score data to the server
        /// </summary>
        private void SendHighScoreDataToPlayFab()
        {
            //Getting stored high score
            int highScore = PlayerPrefs.GetInt("BestScore");

            PlayFabClientAPI.ExecuteCloudScript(
                new ExecuteCloudScriptRequest()
                {
                    FunctionName = "setHighScore",
                    FunctionParameter = new {score = highScore},
                    GeneratePlayStreamEvent = true,
                },
                result =>
                {
                    Debug.Log(PlayFab.PluginManager.GetPlugin
                        <ISerializerPlugin>(PluginContract.PlayFab_Serializer));
                    JsonObject jsonResult = (JsonObject)result.FunctionResult;
                    object messageValue;
                    jsonResult.TryGetValue("messageValue", out messageValue);
                },
                error =>
                {
                    print(error.ErrorMessage);
                } 
            );
        }
    }
}