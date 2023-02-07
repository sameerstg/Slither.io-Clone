using Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Photon_Multiplayer_Scripts.Photon
{
    /// <summary>
    /// Photon Lobby
    /// </summary>
    public class PhotonLobby : MonoBehaviourPunCallbacks
    {
        public static PhotonLobby Instance; //Singleton

        [Header("Button References")]
        //Begin search button
        [SerializeField] private Button beginSearchButton;

        #region Unity Functions, Photon Callbacks

        private void Awake()
        {
            //Creating singleton
            Instance = this;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            
            beginSearchButton.gameObject.SetActive(false);
            
            //Try to connect if we are not connected
            if (PhotonNetwork.IsConnected == false)
            {
                //Connecting using settings
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        /// <summary>
        /// Begins searching for a room
        /// </summary>
        public void BeginRoomSearch()
        {
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("Trying to join random room!");
        }

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            Debug.Log("Connection Successful");
            PhotonNetwork.AutomaticallySyncScene = true;
            BeginRoomSearch();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("Failed to join a room! Trying to create one!");
            //Creating random room
            CreateRoom();
        }

        #endregion

        #region Create Room and Join Room Failed

        /// <summary>
        /// Creating room
        /// </summary>
        void CreateRoom()
        {
            //Setting up a random number for the room
            int randomRoomName = Random.Range(0, 10000);
            
            //Setting up room options
            RoomOptions roomOps = new RoomOptions
            {
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = (byte) MultiplayerSettings.Instance.maxPlayers,
                PlayerTtl = 0,
                EmptyRoomTtl = 0,
            };
            
            PhotonNetwork.CreateRoom($"Room {randomRoomName}", roomOps);
            Debug.Log("Room created");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.Log("Room creation failed! Attempting to recreate.");
            CreateRoom();
        }

        #endregion
    }
}