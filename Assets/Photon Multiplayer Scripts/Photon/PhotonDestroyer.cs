using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Photon_Multiplayer_Scripts.Photon
{
    /// <summary>
    /// Destroys all instances of Photon if there are any in the scene and also disconnects
    /// the player from the Photon server
    /// </summary>
    public class PhotonDestroyer : MonoBehaviour
    {
        private void Start()
        {
            DestroyAllPhotonInstances();
        }

        /// <summary>
        /// Destroys all Photon Instances
        /// </summary>
        private void DestroyAllPhotonInstances()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                print("Disconnecting from the Photon Servers");
            }

            if (PhotonLobby.Instance != null)
            {
                Destroy(PhotonLobby.Instance.gameObject);
                print("Destroyed Photon Lobby");
            }

            if (PhotonRoom.Instance != null)
            {
                Destroy(PhotonRoom.Instance.gameObject);
                print("Destroyed Photon Room");
            }

            if (MultiplayerSettings.Instance != null)
            {
                Destroy(MultiplayerSettings.Instance.gameObject);
                print("Multiplayer settings destroyed");
            }

            if (SceneManager.GetActiveScene().buildIndex == 6)
            {
                SceneManager.LoadScene("Menu");
            }
        }
    }
}