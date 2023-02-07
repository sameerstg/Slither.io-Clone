using UnityEngine;
using System.Collections;
using Photon_Multiplayer_Scripts.Photon;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.Advertisements;

public class BackMenu: MonoBehaviour {
	public void back(){
		if (PlayerPrefs.GetInt("removeAds",0)==0){
			ShowAd();
		}

		//Runs if we are connected to Photon
		if (PhotonNetwork.IsConnected)
		{
			StartCoroutine(ReturnToMainMenu());
			return;
		}
		
		SceneManager.LoadScene ("Menu");
	}

	private IEnumerator ReturnToMainMenu()
	{
		PhotonRoom.Instance.RemoveCallBackTarget();
		
		//Disconnecting from Photon
		while (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
			yield return new WaitForEndOfFrame();
		}
		
		print("Player has left the room");

		while (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
			yield return new WaitForEndOfFrame();
		}
		
		print("Disconnected from Photon");
		
		SceneManager.LoadScene ("Menu");
		
		yield break;
	}
	
	public void ShowAd()
	{
		
	}
}
