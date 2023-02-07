using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Photon_Multiplayer_Scripts.Photon.Game_Controllers
{
    /// <summary>
    /// For the photon player
    /// </summary>
    public class PhotonPlayer : MonoBehaviour
    {
        [Header("Scene variable references")]
        //Reference to count and tokens text
        [SerializeField] private Text countText;
        [SerializeField] private Text tokensText;
        [SerializeField] private Text snakesText;
        
        //PhotonView
        private PhotonView _pV;
        //Avatar
        public GameObject myAvatar;

        #region Unity Functions

        private void Start()
        {
            //Getting the canvas texts using tag 
            GameObject countTextObject = GameObject.FindGameObjectWithTag("Score Text");
            countText = countTextObject.GetComponent<Text>();
            GameObject tokensObject = GameObject.FindGameObjectWithTag("Tokens Text");
            tokensText = tokensObject.GetComponent<Text>();
            GameObject snakeTextObject = GameObject.FindGameObjectWithTag("Snake Text");
            snakesText = snakeTextObject.GetComponent<Text>();
            
            //PhotonView
            _pV = GetComponent<PhotonView>();
            //Random Spawn
            int spawnPicker = Random.Range(0,
                GameSetup.Instance.spawnPoints.Length);

            //Loading the last skin that the player selected
            int skinID = 0;
            
            //Setting up the skin id key
            if (PlayerPrefs.HasKey("skinID") == false)
            {

                PlayerPrefs.SetInt("skinID", 1);
                skinID = PlayerPrefs.GetInt("skinID");
            }
            else
            {
                skinID = PlayerPrefs.GetInt("skinID");
            }

            //Spawning local player
            if (_pV.IsMine)
            {
                //Spawning the snake head according to the skin that the player selected
                switch (skinID)
                {
                    case 1:
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "SnakeHeadPhoton"),
                    GameSetup.Instance.spawnPoints[spawnPicker].position,
                    GameSetup.Instance.spawnPoints[spawnPicker].rotation,
                    0);
                        myAvatar.GetComponent<SnakeMovement>().photonView.RPC(
                            "SetSkinId",
                            RpcTarget.AllBuffered,
                            1
                        );
                        break;
                    case 2:
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "SnakeHeadPhoton 1"),
                    GameSetup.Instance.spawnPoints[spawnPicker].position,
                    GameSetup.Instance.spawnPoints[spawnPicker].rotation,
                    0);
                        myAvatar.GetComponent<SnakeMovement>().photonView.RPC(
                            "SetSkinId",
                            RpcTarget.AllBuffered,
                            2
                        );
                        break;
                    case 3:
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "SnakeHeadPhoton 2"),
                    GameSetup.Instance.spawnPoints[spawnPicker].position,
                    GameSetup.Instance.spawnPoints[spawnPicker].rotation,
                    0);
                        myAvatar.GetComponent<SnakeMovement>().photonView.RPC(
                            "SetSkinId",
                            RpcTarget.AllBuffered,
                            3
                        );
                        break;
                    default:
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "SnakeHeadPhoton"),
                            GameSetup.Instance.spawnPoints[spawnPicker].position,
                            GameSetup.Instance.spawnPoints[spawnPicker].rotation,
                            0);
                        myAvatar.GetComponent<SnakeMovement>().photonView.RPC(
                            "SetSkinId",
                            RpcTarget.AllBuffered,
                            1
                        );
                        break;
                }

                //Setting the canvas text object for the spawned snake
                SnakeMovement snakeMovement = myAvatar.GetComponent<SnakeMovement>();
                snakeMovement.countText = countText;
                snakeMovement.tokenText = tokensText;
                snakeMovement.snakesKilledText = snakesText;
            }
        }

        /// <summary>
        /// Tries to stop the food generation
        /// </summary>
        public void StopFoodGeneration()
        {
            SnakeMovement snakeMovementScript = myAvatar.GetComponent<SnakeMovement>();
            PhotonView snakeMovementView = snakeMovementScript.photonView;
            snakeMovementView.RPC(
                "StopMultiplayerRunGenerateFoodItemRoutine", RpcTarget.AllBuffered
            );
        }

        #endregion
    }
}