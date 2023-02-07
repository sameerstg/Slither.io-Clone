using System.Collections;
using System.IO;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Photon_Multiplayer_Scripts.Photon
{
    /// <summary>
    /// For Photon Rooms of the game
    /// </summary>
    public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
    {
        #region Variables
        
        //Room variables
        public static PhotonRoom Instance;
        
        [Header("Room Control")]
        public int playersInRoom;
        public int myNumberInRoom;

        public int playerInGame;
        public float startingTime;
        private float _atMaxPlayers;
        private float _lessThanMaxPlayers;

        //Player info variables
        private Player[] _photonPlayers;
        private PhotonView _pv;

        public bool isGameLoaded;
        public int currentScene;

        //Variable for Delay Start
        private bool _readyToCount;
        private bool _readyToStart;
        private float _timeToStart;

        #endregion

        #region Unity Functions

        private void Awake()
        {
            //Setting singleton
            if (PhotonRoom.Instance == null)
            {
                PhotonRoom.Instance = this;
            }
            else
            {
                //Destroying previous singleton instance
                if (PhotonRoom.Instance != this)
                {
                    Destroy(PhotonRoom.Instance.gameObject);
                    PhotonRoom.Instance = this;
                }
            }
            DontDestroyOnLoad(this.gameObject);
        }
        
        private void Start()
        {
            //Initializing
            _pv = GetComponent<PhotonView>();
            _readyToCount = false;
            _readyToStart = false;
            _lessThanMaxPlayers = startingTime;
            _atMaxPlayers = 2;
            _timeToStart = startingTime;
            isGameLoaded = false;
        }

        private void Update()
        {
            //Multiplayer settings
            if (MultiplayerSettings.Instance.delayStart)
            {
                //1 Player in room
                if (playersInRoom == 1)
                {
                    RestartTimer();
                }
                //When not game loaded
                if (!isGameLoaded)
                {
                    if (_readyToStart)
                    {
                        _atMaxPlayers -= Time.deltaTime;
                        _lessThanMaxPlayers = _atMaxPlayers;
                        _timeToStart = _atMaxPlayers;
                    }
                    else if (_readyToCount)
                    {
                        _lessThanMaxPlayers -= Time.deltaTime;
                        _timeToStart = _lessThanMaxPlayers;
                        Debug.Log("Display time remaining for start " + _timeToStart);
                    }
                    if (_timeToStart <= 0)
                    {
                        StartGame();
                    }
                }
            }
        }

        #endregion
        
        #region Playfab Functions

        /// <summary>
        /// A function that will set the nickname of the player on the Photon Server
        /// </summary>
        private void SetNickNameInsidePhoton()
        {
            //Request for info request
            var getAccountInfoRequest = new GetAccountInfoRequest
            {
                PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            };
            
            PlayFabClientAPI.GetAccountInfo(
                getAccountInfoRequest,
                result =>
                {
                    string nickNameToSet = result.AccountInfo.Username;
                    PhotonNetwork.NickName = nickNameToSet;
                    
                    print("The player's nickname on the server is set to " + PhotonNetwork.NickName);

                    //Setting the properties in the room
                    SetRoomPropertiesAfterJoining();
                },
                error =>
                {
                    print("Failed to get account info due to error: " + error.ErrorMessage);
                }
            );
        }

        #endregion
        
        #region PUNCALLBACKS and Room Control

        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
            SceneManager.sceneLoaded += OnSceneFinishedLoading;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        /// <summary>
        /// Removes the photon room as the callback target
        /// </summary>
        public void RemoveCallBackTarget()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            SceneManager.sceneLoaded -= OnSceneFinishedLoading;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            print("The disconnection cause was " + cause);
            StartCoroutine(LeaveRoomOnDisconnect());
        }

        /// <summary>
        /// Disconnects the player by leaving room first if the player loses connection
        /// </summary>
        /// <returns></returns>
        private IEnumerator LeaveRoomOnDisconnect()
        {
            while (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                yield return new WaitForEndOfFrame();
            }
            
            print("Player has left room");

            while (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                yield return new WaitForEndOfFrame();
            }
            
            print("Player has disconnected from Photon");
            
            //Trying to still disconnect
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
            
            yield break;
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            SetNickNameInsidePhoton();
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
        }

        //public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        //{
        //    base.OnPlayerEnteredRoom(newPlayer);
        //    Debug.Log("New player entered");
        //    _photonPlayers = PhotonNetwork.PlayerList;
        //    playersInRoom++;
        //    //Delay start
        //    if (MultiplayerSettings.Instance.delayStart)
        //    {
        //        Debug.Log("Waiting for players\n " + playersInRoom + " \\ "
        //                  + MultiplayerSettings.Instance.maxPlayers);
        //        //Ready to count
        //        if (playersInRoom > 1)
        //        {
        //            _readyToCount = true;
        //        }
        //        //Max Players reached
        //        if (playersInRoom == MultiplayerSettings.Instance.maxPlayers)
        //        {
        //            _readyToStart = true;
        //            if (!PhotonNetwork.IsMasterClient)
        //                return;
        //            PhotonNetwork.CurrentRoom.IsOpen = false;
        //        }
        //    }
        //}

        /// <summary>
        /// For starting the multiplayer game
        /// </summary>
        void StartGame()
        {
            isGameLoaded = true;
            
            //Returning if we are not the master client
            if (!PhotonNetwork.IsMasterClient)
                return;
            
            //Delay start setting
            if (MultiplayerSettings.Instance.delayStart)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
            PhotonNetwork.LoadLevel(MultiplayerSettings.Instance.multiPlayerScene);
        }

        /// <summary>
        /// To restart timer
        /// </summary>
        void RestartTimer()
        {
            _lessThanMaxPlayers = startingTime;
            _timeToStart = startingTime;
            _atMaxPlayers = 2;
            _readyToCount = false;
            _readyToStart = false;
        }

        /// <summary>
        /// This function will set the room properties once the user's nickname has been set
        /// </summary>
        private void SetRoomPropertiesAfterJoining()
        {
            Debug.Log("Room joined");
            //Getting room info
            //_photonPlayers = PhotonNetwork.PlayerList;
            playersInRoom++;
            playerInGame = _photonPlayers.Length;
            myNumberInRoom = playersInRoom;
            
            //Delay start setting
            if (MultiplayerSettings.Instance.delayStart)
            {
                Debug.Log("Waiting for players\n " + playersInRoom + " \\ "
                          + MultiplayerSettings.Instance.maxPlayers);
                //Ready to count
                if (playersInRoom > 1)
                {
                    _readyToCount = true;
                }
                //Handling maximum player reach
                if (playersInRoom == MultiplayerSettings.Instance.maxPlayers)
                {
                    _readyToStart = true;
                    if (PhotonNetwork.IsMasterClient)
                        return;
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                }
            }
            else
            {
                StartGame();
            }
        }
        
        #endregion

        #region RPCs, and Scene Loading
        
        /// <summary>
        /// Scene loading callback
        /// </summary>
        void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            currentScene = scene.buildIndex;
            if (currentScene == MultiplayerSettings.Instance.multiPlayerScene)
            {
                isGameLoaded = true;
                //Delay start loading
                if (MultiplayerSettings.Instance.delayStart)
                {
                    _pv.RPC("RPC_LoadedGameScene", RpcTarget.MasterClient);
                }
                else
                {
                    RPC_CreatePlayer();
                }
            }
        }

        /// <summary>
        /// RPC Loaded Game Scene
        /// </summary>
        [PunRPC]
        private void RPC_LoadedGameScene()
        {
            playerInGame++;
            if (playerInGame == PhotonNetwork.PlayerList.Length)
            {
                _pv.RPC("RPC_CreatePlayer", RpcTarget.All);
            }
        }

        /// <summary>
        /// Create Player RPC
        /// </summary>
        [PunRPC]
        private void RPC_CreatePlayer()
        {
            var spawnedPlayer = PhotonNetwork.Instantiate(Path.Combine(
                    "PhotonPrefabs", "PlayerAvatarParent"
                ),
                Vector3.zero,
                Quaternion.identity,
                0
            );
            
            print(spawnedPlayer.name);
        }

        #endregion
    }
}