using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace PlayFabScripts
{
    /// <summary>
    /// Initializes values on the leaderboard
    /// </summary>
    public class LeaderboardScript : MonoBehaviour
    {
        [Header("Leaderboard related variables")]
        //Main menu reference
        [SerializeField] private GameObject mainMenuReference;
        
        //Top Leaderboard content container
        [SerializeField] private GameObject topLeaderboardContentContainer;
        //Player leaderboard container
        [SerializeField] private GameObject playerLeaderboardContentContainer;
        
        //Leaderboard prefab reference
        [SerializeField] private LeaderboardItemPrefab leaderboardItemPrefab;

        public void BackButtonClicked()
        {
            mainMenuReference.SetActive(true);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// A function that will load the top leaderboard
        /// </summary>
        private void LoadTopLeaderboard()
        {
            //Destroying all children to create new ones
            for (int i = 0; i < topLeaderboardContentContainer.transform.childCount; i++)
            {
                Destroy(topLeaderboardContentContainer.transform.GetChild(i).gameObject);
            }

            //Setting up client leaderboard request
            var leaderBoardRequest = new GetLeaderboardRequest
            {
                StatisticName = "highScore",
                MaxResultsCount = 10,
            };
            
            //Getting leaderboard results and displaying them on screen
            PlayFabClientAPI.GetLeaderboard(
                leaderBoardRequest,
                result =>
                {
                    foreach (var leaderboardEntry in result.Leaderboard)
                    {
                        //Spawning board item
                        var boardItem 
                            = Instantiate(leaderboardItemPrefab, topLeaderboardContentContainer.transform);
                        //Set leaderboard values
                        boardItem.SetLeaderboardItemValues(leaderboardEntry.DisplayName, 
                            leaderboardEntry.StatValue.ToString());
                    }
                },
                error =>{print(error.ErrorMessage);} 
            );
        }

        /// <summary>
        /// Loads leaderboard around the current player
        /// </summary>
        private void LoadAroundLeaderboard()
        {
            //Destroying all children to create new ones
            for (int i = 0; i < playerLeaderboardContentContainer.transform.childCount; i++)
            {
                Destroy(playerLeaderboardContentContainer.transform.GetChild(i).gameObject);
            }

            //Setting up client leaderboard request
            var leaderBoardRequest = new GetLeaderboardAroundPlayerRequest()
            {
                StatisticName = "highScore",
                MaxResultsCount = 10,
            };
            
            //Getting leaderboard results and displaying them on screen
            PlayFabClientAPI.GetLeaderboardAroundPlayer(
                leaderBoardRequest,
                result =>
                {
                    foreach (var leaderboardEntry in result.Leaderboard)
                    {
                        //Spawning board item
                        var boardItem 
                            = Instantiate(leaderboardItemPrefab, playerLeaderboardContentContainer.transform);
                        //Set leaderboard values
                        boardItem.SetLeaderboardItemValues(leaderboardEntry.DisplayName,
                            leaderboardEntry.StatValue.ToString());
                    }
                },
                error =>{print(error.ErrorMessage);}
            );
        }

        private void OnEnable()
        {
            LoadTopLeaderboard();
            LoadAroundLeaderboard();
        }
    }
}