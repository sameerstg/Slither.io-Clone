using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayFabScripts
{
    public class LeaderboardItemPrefab : MonoBehaviour
    {
        [Header("Leaderboard Item Variables")]
        //Player Name
        [SerializeField] private Text playerName;
        //Player score
        [SerializeField] private Text playerScore;

        /// <summary>
        /// A function that will set the score and the name on the prefab
        /// </summary>
        /// <param name="nameToSet"></param>
        /// <param name="scoreToSet"></param>
        public void SetLeaderboardItemValues(string nameToSet, string scoreToSet)
        {
            playerName.text = nameToSet;
            playerScore.text = scoreToSet;
        }
    }
}