using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingStatus : MonoBehaviour
{
    public string nickname;
    public int skinID;
    public int controlMethodId;
    public InputField input;

    [SerializeField] private GameObject leaderBoardScreen;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject photonLoadingImage;
    [SerializeField] private GameObject betAmountScreen;

    //Set the settings when related button is clicked.			
    public void Selected(string buttonName)
    {
        if (buttonName == "Skin1")
        {
            skinID = 1;
            PlayerPrefs.SetInt("skinID", skinID);
        }
        else if (buttonName == "Skin2")
        {
            skinID = 2;
            PlayerPrefs.SetInt("skinID", skinID);
        }
        else if (buttonName == "Skin3")
        {
            skinID = 3;
            PlayerPrefs.SetInt("skinID", skinID);
        }
        else if (buttonName == "Control1")
        {
            controlMethodId = 1;
            PlayerPrefs.SetInt("moveWayID", controlMethodId);
        }
        else if (buttonName == "Control2")
        {
            controlMethodId = 2;
            PlayerPrefs.SetInt("moveWayID", controlMethodId);
        }
        else if (buttonName == "Control3")
        {
            controlMethodId = 3;
            PlayerPrefs.SetInt("moveWayID", controlMethodId);
        }
        else if (buttonName == "removeAds")
        {
            PlayerPrefs.SetInt("removeAds", 1);
        }
        else if (buttonName == "showAds")
        {
            PlayerPrefs.SetInt("removeAds", 0);
        }
        //When button "PlayOnline" or "PlayWithAI" is clicked, set the nickname and load game scene.
        else if (buttonName == "PlayOnline" || buttonName == "PlayWithAI")
        {
            //input = GameObject.Find("Nickname").GetComponent<InputField>();
            //PlayerPrefs.SetString("nickname", input.text);s
            SceneManager.LoadScene("Slither.io");
        }
        //When we have clicked the leaderboard button
        else if (buttonName == "Leaderboard")
        {
            mainMenuPanel.SetActive(false);
            leaderBoardScreen.SetActive(true);
        }
    }

    /// <summary>
    /// Displays the bet amount screen to the player
    /// </summary>
    public void DisplayBetAmountScreen()
    {
        betAmountScreen.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// a function that will turn on the photon multiplayer screen
    /// </summary>
    public void TurnOnPhotonLoadingImage()
    {
        photonLoadingImage.SetActive(true);
        mainMenuPanel.SetActive(false);
    }
}