using System;
using Photon.Pun;
using UnityEngine;

namespace Photon_Multiplayer_Scripts.Photon.Game_Controllers
{
    /// <summary>
    /// GameSetup for multi-player functions
    /// </summary>
    public class GameSetup : MonoBehaviourPunCallbacks
    {
        #region Variables

        //Singleton
        public static GameSetup Instance;

        //Photon View of this object
        public PhotonView pV;

        [Header("Spawn Points")] 
        public Transform[] spawnPoints;

        #endregion

        #region Unity Functions

        public override void OnEnable()
        {
            base.OnEnable();
            //Singleton setting
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            pV = GetComponent<PhotonView>();
        }

        private void Update()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                
            }
        }

        #endregion

        #region Photon RPC Functions

        /// <summary>
        /// A function that will handle the number of snakes killed for players
        /// </summary>
        /// <param name="snakeViewId"></param>
        [PunRPC]
        private void IncreaseNumberOfSnakesKilled(int snakeViewId)
        {
            //Only runs if we are the mast client
            if (PhotonNetwork.IsMasterClient == false) return;
            
            //Accessing the snake movement script
            PhotonView snakeView = PhotonNetwork.GetPhotonView(snakeViewId);

            //Returning if the view is null
            if (snakeView == null) return;
            
            print("Found snake view id to increase the number of snakes killed");
            
            //Returning if we are not the local player
            if (snakeView.IsMine == false) return;
            
            print("Increasing the number of snakes killed");
            
            //Getting the snake movement script and increasing the number of snakes killed
            SnakeMovement snakeMovement = snakeView.GetComponent<SnakeMovement>();
            snakeMovement.numberOfSnakesKilled++;
        }
        
        /// <summary>
        /// Handles the tags and layers of all foreign players
        /// </summary>
        [PunRPC]
        private void HandleBodyTagsForForeignPlayers()
        {
            //Getting all player heads
            GameObject[] playerHeads = GameObject.FindGameObjectsWithTag("Player");

            //Changing the tag of all foreign players
            foreach (GameObject playerHead in playerHeads)
            {
                PhotonView headPv = playerHead.GetComponent<PhotonView>();

                if (headPv == null) continue;
                
                //Changing tag if this head belongs to a foreign player
                if (headPv.IsMine == false)
                {
                    playerHead.layer = 7;
                    
                    //Changing layer of sides
                    SnakeMovement snakeMovement = playerHead.GetComponent<SnakeMovement>();
                    snakeMovement.snakeSidesRef[0].layer = 7;
                    snakeMovement.snakeSidesRef[1].layer = 7;
                }
            }
            
            //Getting all bodies
            GameObject[] snakeBodies = GameObject.FindGameObjectsWithTag("Snake");
            
            //Changing the tag of all bodies in foreign players
            foreach (GameObject snakeBody in snakeBodies)
            {
                PhotonView snakeBodyPv = snakeBody.GetComponent<PhotonView>();
                
                if(snakeBodyPv == null) continue;
                
                if (snakeBodyPv.IsMine == false)
                {
                    snakeBody.layer = 7;
                    snakeBody.transform.parent = null;
                }
            }
        }

        #endregion
    }
}