using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Photon_Multiplayer_Scripts.Photon.Game_Controllers;
using Photon_Multiplayer_Scripts.Photon.Gameplay_Scripts;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using SnakeScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SnakeMovement : MonoBehaviour, IPunObservable
{
    #region Main Variables

    public PhotonView photonView;
    //When true, tells the game that this is a photon player
    [SerializeField] private bool isPhotonPlayer;

    private bool _hasCollidedWithPlayer = false;

    //Camera for the Photon Network
    private GameObject _multiPlayerCamera;

    //Body parts spawn
    public GameObject snakeBodyParts;

    //The texture of this snake head
    [FormerlySerializedAs("snakeTexture")] public Sprite snakeBodyTexture;
    public Sprite greenSnakeTexture, blueSnakeTexture, orangeSnakeTexture;

    public List<Transform>
        bodyParts = new List<Transform>(); // Records the location information of body parts of the snake

    //A reference to the sides of the Snake
    public List<GameObject> snakeSidesRef = new List<GameObject>();

    public List<GameObject> Robots = new List<GameObject>(); // Records the information of Robots
    //public float runBodyPartSmoothTime = 0.1f; // // Called in SnakeRun()
    // private float cameraSmoothTime = 0.13f;  // Called in CameraFollowSnake()

    public float runBodyPartSmoothTime = 0.1f; // // Called in SnakeRun()

    public Transform
        addBodyPart; // Called in OnCollisionEnter(), it is the thing added behind the snake after eating food

    public int foodUpCounter;
    public int foodCounter; // Called in OnCollisionEnter(), the number of food the snake eats so far
    public int curSizeLevel; // Called in OnCollisionEnter()
    public Vector3 curSize = Vector3.one; //Called in SizeUp()

    //	##### added by Yue Chen #####
    public Text countText;
    public Text tokenText;
    public Text snakesKilledText;
    public int length;
    public float tokensCollected;
    public int numberOfSnakesKilled = 0;
    public float boostSpeed = 0;
    public bool hasLongReachAbility = false;

    public int curAmountOfRobot, maxAmountOfRobot = 30; // The max amount of robots in the map
    public GameObject[] robotGenerateTarget; // Store the objects of robot snakes

    /* Generate food points every few seconds until there are enough points on the map*/
    public int curAmountOfFood, maxAmountOfFood = 10; // The max amount of food in the map
    public int curAmountOfItem, maxAmountOfItem = 60; // The max amount of item in the map
    public GameObject[] foodGenerateTarget; // Store the objects of food points
    public GameObject[] itemGenerateTarget; // Store the objects of item
    public GameObject stone; // the object of stone

    /* Choose the skin of snake*/
    public Material blue, red, orange;
    private float bodyPartSmoothTime = 0.2f; //Called in OnCollisionEnter(), the same value as in SnakeBodyActions.cs
    private float bodySmoothTime; // Called in SetBodySizeAndSmoothTime()

    private readonly float
        cameraGrowRate = 0.03f; // Called in OnCollisionEnter(), when snake gets larger, camera size gets larger as well

    private readonly float cameraSmoothTime = 0.13f; // Called in CameraFollowSnake()

    private readonly float foodGenerateEveryXSecond = 1.0f; // Generate a food point every 3 seconds

    //Called in OnCollisionEnter(), determine after eating how many food points will the snake add a body part
    private readonly int[] foodUpArray = { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 10 };
    private readonly float growRate = 0.1f; //Called in OnCollisionEnter(), how much to grow snake size

    private bool isRunning; // Called in SnakeRun()

    //	##### added by Yue Chen #####
    private int moveWay; // It determines how to control the movement of snake, gained from initial interface

    private string nickName;

    /* Sanke moves toward finger*/
    private Vector3 pointInWorld, mousePosition, direction, pointInWorldForeignLagCompensation = new Vector3();
    private readonly float radius = 20.0f;

    //Called in SizeUp(), determine after eating how much food the snake will grow its size
    private readonly int[] sizeUpArray = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 5096, 10192, 20348, 999999 };
    public int skinID; // It determines the skin of the snake, gained from initial interface
    private readonly float snakeRunSpeed = 7.0f; // Called in SnakeRun()

    private float snakeWalkSpeed = 3.5f; // Called in SnakeMove()

    /* Make the snake run when it should run, and lose parts*/
    private float t1;
    private float t2;

    //Variables for Photon Observable function
    private Vector3 _foreignPlayerPosition = new Vector3();
    private Vector3 _foreignPlayerRotation = new Vector3();

    #endregion

    #region Photon RPCs, and Observable Region

    /// <summary>
    /// A function that will increase the amount of snakes killed for both players
    /// </summary>
    public void IncreaseSnakesKilledNumber()
    {
        print("Increasing the snake killed number");
        photonView.RPC("IncreaseSnakesKilledByOne", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void SetSkinId(int id)
    {
        skinID = id;
    }

    /// <summary>
    /// Adds a body part to the snake across the network
    /// </summary>
    /// <param name="viewID"></param>
    [PunRPC]
    private void AddBodyPartWithViewId(int viewID)
    {
        var snakeBodyPart = PhotonNetwork.GetPhotonView(viewID);
        bodyParts.Add(snakeBodyPart.transform);
        snakeBodyPart.gameObject.layer = 3;

        //Setting the tags for all bodies in the network
        GameSetup.Instance.pV.RPC("HandleBodyTagsForForeignPlayers", RpcTarget.AllBuffered);
    }

    /// <summary>
    /// Spawns the food particle across the network according to the number passed
    /// to it. This is used whena snake has been destroyed on the network
    /// </summary>
    [PunRPC]
    private void SpawnFoodOnNetwork(int foodNumber, Vector3 spawnPosition)
    {
        //Food and spawn position reference
        PhotonView foodSpawned;
        Vector3 spawnRef = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);

        switch (foodNumber)
        {
            case 0:
                foodSpawned = PhotonNetwork.InstantiateRoomObject(
                    Path.Combine("PhotonPrefabs", "GlowFood_GreenPhoton"),
                    spawnRef,
                    Quaternion.identity).GetComponent<PhotonView>();
                break;
            case 1:
                foodSpawned = PhotonNetwork.InstantiateRoomObject(
                    Path.Combine("PhotonPrefabs", "GlowFood_RedPhoton"),
                    spawnRef,
                    Quaternion.identity).GetComponent<PhotonView>();
                break;
            case 2:
                foodSpawned = PhotonNetwork.InstantiateRoomObject(
                    Path.Combine("PhotonPrefabs", "GlowFood_YellowPhoton"),
                    spawnRef,
                    Quaternion.identity).GetComponent<PhotonView>();
                break;
            case 3:
                var newFoodPhotonView =
                    PhotonNetwork.InstantiateRoomObject(
                        Path.Combine("PhotonPrefabs", "GlowFood_DummyTokenPhoton"),
                        spawnRef,
                        Quaternion.identity).GetComponent<PhotonView>();
                //If the photon view is found
                if (newFoodPhotonView)
                {
                    photonView.RPC(
                        "SetTokenValue",
                        RpcTarget.AllBuffered,
                        newFoodPhotonView.ViewID
                    );
                }
                break;
            default:
                foodSpawned =
                    PhotonNetwork.InstantiateRoomObject(
                        Path.Combine("PhotonPrefabs", "GlowFood_YellowPhoton"),
                        spawnRef,
                        Quaternion.identity).GetComponent<PhotonView>();
                break;
        }
    }

    /// <summary>
    /// A function that will spawn a token on the network with the correct bet amount
    /// </summary>
    /// <param name="spawnPosition"></param>
    /// <param name="betAmount"></param>
    [PunRPC]
    private void SpawnTokenOnNetworkWithBetValue(Vector3 spawnPosition, int betAmount)
    {
        //Food and spawn position reference
        Vector3 spawnRef = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);
        
        //Spawning new food item
        var newFoodPhotonView =
            PhotonNetwork.InstantiateRoomObject(
                Path.Combine("PhotonPrefabs", "GlowFood_DummyTokenPhoton"),
                spawnRef,
                Quaternion.identity).GetComponent<PhotonView>();
        //If the photon view is found
        if (newFoodPhotonView)
        {
            photonView.RPC(
                "SetTokenValue",
                RpcTarget.AllBuffered,
                newFoodPhotonView.ViewID,
                betAmount
            );
        }
    }

    /// <summary>
    /// A function that will set the value of the token across the network by finding the token
    /// with the id and then using a for each loop to find the token and set its value
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="betAmount"></param>
    [PunRPC]
    private void SetTokenValue(int tokenId, int betAmount)
    {
        var getTokenInView = PhotonNetwork.GetPhotonView(tokenId);
        TokenScript getTokenScrip = getTokenInView.GetComponent<TokenScript>();
        getTokenScrip.SetTokenValueByAdding(betAmount/3.0f);
        getTokenScrip.SetTokenBetValue(betAmount);
    }

    /// <summary>
    /// An RPC that will run when the player has collided with a token that has a bet amount value
    /// greater than its own
    /// </summary>
    /// <param name="tokenPhotonViewId"></param>
    /// <param name="deductAmount"></param>
    [PunRPC]
    private void DeductTokenValue(int tokenPhotonViewId, int deductAmount)
    {
        var getTokenInView = PhotonNetwork.GetPhotonView(tokenPhotonViewId);
        TokenScript getTokenScript = getTokenInView.GetComponent<TokenScript>();
        getTokenScript.DeductTokenValueBySubtracting(deductAmount);
        
    }

    /// <summary>
    /// Destroys a snake on the network. Call an RPC to spawn tokens and food on the network
    /// Please refer to RPC called 'SpawnTokenOnNetworkWithBetValue' to get an idea of the
    /// spawning system for the tokens
    /// </summary>
    private void DestroySnakeCompleteOnNetworkAndSpawnFoodTokens(GameObject obj)
    {
        //Getting view id
        PhotonView snakeViewId = obj.GetComponent<PhotonView>();

        print("Collided snake had an id " + snakeViewId.ViewID);

        //If snake view id is not null
        if (snakeViewId != null)
        {
            //Sending RPC to increase the number of snakes killed
            int snakesViewId = snakeViewId.ViewID;
            GameSetup.Instance.pV.RPC(
                "IncreaseNumberOfSnakesKilled",
                RpcTarget.AllBuffered,
                snakesViewId
            );
        }

        //Deactivating game objects
        photonView.RPC("DeactivateSnakeBodies", RpcTarget.AllBuffered);
        PhotonNetwork.Destroy(photonView.gameObject);
    }

    /// <summary>
    /// Deactivates the snake bodies on master client
    /// </summary>
    [PunRPC]
    private void DeactivateSnakeBodies()
    {
        //Making middle part and end part num in these variables
        float middleBodyPartNum = 0;
        float endBodyPartNum = 0;

        //Checking if number is even
        if (bodyParts.Count % 2 == 0)
        {
            //Setting body part
            middleBodyPartNum = bodyParts.Count / 2.0f;
            endBodyPartNum = bodyParts.Count - 1;
        }
        else
        {
            //Setting body part
            middleBodyPartNum = middleBodyPartNum = bodyParts.Count / 2.0f;
            endBodyPartNum = bodyParts.Count - 1;
        }

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (i == 0 || i == middleBodyPartNum || i == endBodyPartNum)
            {
                //Spawns only if we are the local player
                if (photonView.IsMine == true)
                {
                    photonView.RPC("SpawnTokenOnNetworkWithBetValue",
                        RpcTarget.MasterClient, bodyParts[i].position,
                        PlayerPrefs.GetInt("BetAmount")
                    );
                }

                bodyParts[i].gameObject.SetActive(false);
                Destroy(bodyParts[i].gameObject);
                continue;
            }

            //Spawning a random food item
            int randomFoodToSpawn = Random.Range(0, 3);
            //Spawn only if we are the local player
            if (photonView.IsMine == true)
            {
                photonView.RPC("SpawnFoodOnNetwork",
                    RpcTarget.MasterClient, randomFoodToSpawn,
                    bodyParts[i].position);
            }

            bodyParts[i].gameObject.SetActive(false);
            Destroy(bodyParts[i].gameObject);
        }
    }

    /// <summary>
    /// An RPC that will be called on the master client. It will simply destroy the object
    /// that is on the network by finding it using its view id. The tag of the object will also
    /// be passed
    /// </summary>
    /// <param name="viewID"></param>
    [PunRPC]
    private void DestroyObjectOnNetwork(int viewID)
    {
        var photonViewOfObjectToDestroy = PhotonNetwork.GetPhotonView(viewID);
        if (!photonViewOfObjectToDestroy) return;

        PhotonNetwork.Destroy(photonViewOfObjectToDestroy.gameObject);
    }

    /// <summary>
    /// Applies the rotation on the foreign player
    /// </summary>
    [PunRPC]
    private void ApplyHeadRotationOnForeignPlayer(Vector3 eulerToSet, Vector3 positionToSet)
    {
        transform.rotation = Quaternion.Euler(eulerToSet);
        transform.position = Vector3.MoveTowards(
            transform.position,
            positionToSet,
            10.0f
        );
    }

    /// <summary>
    /// Increases the number of snakes killed ofr both players
    /// </summary>
    [PunRPC]
    private void IncreaseSnakesKilledByOne()
    {
        numberOfSnakesKilled++;
        print("Number of snakes killed was increased");
    }

    /// <summary>
    /// Enables the snake colliders after a while
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnableSnakeCollidersAfterAWhile()
    {
        yield return new WaitForSeconds(2.0f);
        photonView.RPC("EnableSnakeCollidersAfterAWhileRPC", RpcTarget.AllBuffered);
    }

    /// <summary>
    /// RPC that will enable the snake colliders after a while
    /// </summary>
    [PunRPC]
    private void EnableSnakeCollidersAfterAWhileRPC()
    {
        StartCoroutine("EnableSnakeColliders");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!isPhotonPlayer) return;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation.eulerAngles);
            stream.SendNext(snakeWalkSpeed);
            stream.SendNext(pointInWorld);
            stream.SendNext(direction);
            stream.SendNext(mousePosition);
            stream.SendNext(_isCollidingWithGameObject);
            stream.SendNext(bodySmoothTime);
        }
        else if (stream.IsReading)
        {
            _foreignPlayerPosition = (Vector3)stream.ReceiveNext();
            _foreignPlayerRotation = (Vector3)stream.ReceiveNext();
            snakeWalkSpeed = (float)stream.ReceiveNext();
            pointInWorld = (Vector3)stream.ReceiveNext();
            direction = (Vector3)stream.ReceiveNext();
            mousePosition = (Vector3)stream.ReceiveNext();
            _isCollidingWithGameObject = (bool)stream.ReceiveNext();
            bodyPartSmoothTime = (float)stream.ReceiveNext();

            _isReading = true;
        }
    }

    #endregion

    #region Unity Functions

    private void Awake()
    {
        //Functions to run if we are a photon player
        if (isPhotonPlayer)
        {
            photonView = GetComponent<PhotonView>();

            if (photonView.IsMine == false)
            {
                return;
            }

            SpawnBodyParts();
            return;
        }

        SpawnBodyParts();
    }

    // use this for initialization
    private void Start()
    {
        //Code to run if we are the Photon Player
        if (isPhotonPlayer)
        {
            DisableSnakeColliders();
            
            //Setting tokens collected to 0 at start for both players
            tokensCollected = 0;

            if (photonView.IsMine == false)
            {
                EnableSnakeCollidersOnForeignPlayer();
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                print("We are the master client");
                maxAmountOfFood = 400;
                // GenerateFoodBeforeBegin();
                // //GenerateStoneBeforeBegin();
                // GenerateRobotBeforeBegin();
            }

            //Snake movement speed
            snakeWalkSpeed += boostSpeed;
            
            //Adding long reach component if this component has
            //the long reach ability
            if (hasLongReachAbility)
            {
                gameObject.AddComponent<LongReachAbilityScript>();
            }
            
            //Spawning multiplayer camera
            _multiPlayerCamera = PhotonNetwork.Instantiate(
                Path.Combine("PhotonPrefabs", "PhotonMultiplayerCamera"),
                transform.position,
                Quaternion.identity
            );

            //	##### added by Yue Chen #####
            moveWay = PlayerPrefs.GetInt("moveWayID",
                1); // It determines how to control the movement of snake, gained from initial interface
            nickName = PhotonNetwork.NickName;

            //Enabling all colliders after a while
            StartCoroutine(EnableSnakeCollidersAfterAWhile());

            return;
        }

        GenerateFoodBeforeBegin();
        //GenerateStoneBeforeBegin();
        GenerateRobotBeforeBegin();
        //	##### added by Yue Chen #####
        moveWay = PlayerPrefs.GetInt("moveWayID",
            1); // It determines how to control the movement of snake, gained from initial interface
        // It determines the skin of the snake, gained from initial interface
        skinID = PlayerPrefs.GetInt("skinID", 1);
        nickName = PlayerPrefs.GetString("nickname", "");
    }

    // update is called once per frame
    private void Update()
    {
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false)
            {
                ColorSnake(skinID);

                //Deactivating foreign cameras
                var mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
                foreach (GameObject mainCamera in mainCameras)
                {
                    PhotonView cameraPhotonView = mainCamera.GetComponent<PhotonView>();
                    if (cameraPhotonView.IsMine == false)
                    {
                        cameraPhotonView.gameObject.SetActive(false);
                    }
                }

                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                maxAmountOfFood = 400;
            }

            ChooseControlMethod(moveWay);

            photonView.RPC("ColorSnakeRPC", RpcTarget.AllBuffered, skinID);

            GenerateFoodAndItem();
            SnakeRun();
            SetScore(length);

            return;
        }

        ChooseControlMethod(moveWay);
        ColorSnake(skinID);
        GenerateFoodAndItem();
        SnakeRun();
        SetScore(length);
    }

    private void FixedUpdate()
    {
        //Code to run if we are the Photon Player
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false)
            {
                SnakeMovementOnForeignPlayer();
                return;
            }

            SnakeMove();
            SetBodySizeAndSmoothTime();
            CameraFollowSnake();
            SnakeGlowing(isRunning);
            SnakeMoveAdjust();
            //	##### added by Yue Chen #####
            countText.text = "G o o d  j o b  !  " + nickName + "\nY o u r  L e n g t h  :  " + length;
            tokenText.text = "T o k e n s  C o l l e c t e d : " + tokensCollected;
            snakesKilledText.text = "S n a k e s  K i l l e d : " + numberOfSnakesKilled;

            return;
        }

        SnakeMove();
        SetBodySizeAndSmoothTime();
        CameraFollowSnake();
        SnakeGlowing(isRunning);
        SnakeMoveAdjust();
        //	##### added by Yue Chen #####
        countText.text = "G o o d  j o b  !  " + nickName + "\nY o u r  L e n g t h  :  " + length;
        tokenText.text = "T o k e n s  C o l l e c t e d : " + tokensCollected;
    }

    /* When the head encounters an object, figure out what to do*/
    private void OnCollisionEnter(Collision obj)
    {
        //Code to run if we are the photon player
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false)
            {
                return;
            }
            if (obj.transform.CompareTag("Boundary")) {
              SpawnPlayerSnakeScript.Instance.levelFail.SetActive(true);
            }

                if (obj.transform.CompareTag("Food"))
            {
                length++;

                //Destroying food item
                int foodViewId = obj.gameObject.GetComponent<PhotonView>().ViewID;
                //Photon view RPC
                photonView.RPC(
                    "DestroyObjectOnNetwork",
                    RpcTarget.MasterClient,
                    foodViewId
                );
                obj.gameObject.SetActive(false);

                curAmountOfFood--;
                if (foodUpCounter + 1 == foodUpArray[curSizeLevel])
                {
                    if (SizeUp(foodCounter) == false)
                    {
                        foodCounter++;
                        // The contents in 'if' shouldn't be executed in logic as we always have several body parts
                        Vector3 currentPos;
                        if (bodyParts.Count == 0)
                            currentPos = transform.position;
                        else
                            currentPos = bodyParts[bodyParts.Count - 1].position;
                        var newPart = PhotonNetwork.Instantiate(
                            Path.Combine("PhotonPrefabs", "SnakeBodyPhoton"),
                            currentPos, Quaternion.identity).transform;
                        newPart.gameObject.layer = 3;
                        int photonId = newPart.gameObject.GetComponent<PhotonView>().ViewID;
                        photonView.RPC("AddBodyPartWithViewId", RpcTarget.AllBuffered, photonId);
                    }
                    else
                    {
                        curSize += Vector3.one * growRate;
                        bodyPartSmoothTime += 0.01f;
                        transform.localScale = curSize;
                        // Scale up camera
                        if (isPhotonPlayer == false)
                        {
                            var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                                findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                        }
                        else
                        {
                            if (photonView.IsMine)
                            {
                                _multiPlayerCamera.GetComponent<Camera>().orthographicSize +=
                                    _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                            }
                        }
                    }

                    foodUpCounter = 0;
                }
                else
                {
                    foodUpCounter++;
                }
            }
            //If the snake collides with a token, this else/if will run
            else if (obj.transform.CompareTag("Token"))
            {
                //Getting the token script and the value to add
                TokenScript tokenScript = obj.gameObject.GetComponent<TokenScript>();
                float tokensToAdd = tokenScript.GetTokenValue();

                //Trying to get the popup text
                GameObject popupTextMeshGameObject = GameObject.FindGameObjectWithTag("TokenPopContainer");
                
                //When the snake has already collided
                if (tokenScript.localSnakeHasCollided == true)
                {
                    //If we have found the popupTextMesh, we will tell the player that he/she has already
                    //collided with this token
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        popupText.text = "Cannot Collect Anymore!";
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }
                    
                    return;
                }
                
                //Getting the photon view id
                int tokenPhotonViewId 
                    = tokenScript.gameObject.GetComponent<PhotonView>().ViewID;
                
                //Telling the game that the token has collided with the player
                tokenScript.localSnakeHasCollided = true;

                int playersBetAmount = PlayerPrefs.GetInt("BetAmount");

                //If the token script bet value is greater than the player's bet value
                if (tokenScript.GetTokenBetValue() > PlayerPrefs.GetInt("BetAmount"))
                {
                    //Sending the RPC to deduct the amount
                    photonView.RPC(
                        "DeductTokenValue", 
                        RpcTarget.AllBuffered,
                        tokenPhotonViewId,
                        playersBetAmount
                    );

                    //Telling the game that the player can only collect up to bet amount
                    tokensCollected += playersBetAmount;
                    
                    print("Only a portion of the token could be collected");
                    
                    //If we have found the text object, we will tell the player how much they have collected
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        //Displaying the amount that the player has collected
                        popupText.text = playersBetAmount.ToString();
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }

                    //Returning as we do not want to destroy the token on the network as only its value
                    //has been deducted
                    return;
                }

                //If the token has a bet value less than the player's bet value, we will add up all of the
                //amount of the token and then destroy it
                if (tokenScript.GetTokenBetValue() < PlayerPrefs.GetInt("BetAmount"))
                {
                    //Collecting the entire amount that the token has
                    tokensCollected += tokensToAdd;
                    
                    //If we have found the text object, we will tell the player how much they have collected
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        //Displaying the amount that the player has collected
                        popupText.text = tokensToAdd.ToString();
                        
                        print("All of the token was collected");
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }
                }

                // //Running this code only if we find the gameObject
                // if (popupTextMeshGameObject)
                // {
                //     //Getting the popup text from the script in the scene if we find it
                //     TokenPopupContainerScript tokenPopupContainerScript
                //         = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                //     TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();
                //     
                //     //Only runs when the value of the bet is less than the amount that this player bet
                //     if (tokenScript.GetTokenBetValue() < PlayerPrefs.GetInt("BetAmount"))
                //     {
                //         popupText.text = tokensToAdd.ToString();
                //     }
                //     else
                //     {
                //         popupText.text = "Bet Not High Enough!";
                //     }
                //
                //     //Setting popup text position including local position
                //     var thisGameObjectPosition = transform.position;
                //     var popupTextTransform = popupText.transform;
                //     popupTextTransform.position = new Vector3(
                //         thisGameObjectPosition.x,
                //         thisGameObjectPosition.y,
                //         0
                //     );
                //     var localPositionToSetToPopupText = popupTextTransform.localPosition;
                //     localPositionToSetToPopupText = new Vector3(
                //         localPositionToSetToPopupText.x,
                //         localPositionToSetToPopupText.y,
                //         -25
                //     );
                //     popupTextTransform.localPosition = localPositionToSetToPopupText;
                // }

                //Destroying token
                int tokenId = obj.gameObject.GetComponent<PhotonView>().ViewID;
                //Photon view RPC
                photonView.RPC(
                    "DestroyObjectOnNetwork",
                    RpcTarget.MasterClient,
                    tokenId
                );
                obj.gameObject.SetActive(false);
                curAmountOfFood--;

                if (foodUpCounter + 1 == foodUpArray[curSizeLevel])
                {
                    if (SizeUp(foodCounter) == false)
                    {
                        Vector3 currentPos;
                        if (bodyParts.Count == 0)
                            currentPos = transform.position;
                        else
                            currentPos = bodyParts[bodyParts.Count - 1].position;
                        var newPart = PhotonNetwork.Instantiate(
                            Path.Combine("PhotonPrefabs", "SnakeBodyPhoton"),
                            currentPos, Quaternion.identity).transform;
                        newPart.parent = GameObject.Find("SnakeBodies").transform;
                        newPart.gameObject.layer = 3;
                        int photonId = newPart.gameObject.GetComponent<PhotonView>().ViewID;
                        photonView.RPC("AddBodyPartWithViewId", RpcTarget.AllBuffered, photonId);
                    }
                    else
                    {
                        curSize += Vector3.one * growRate;
                        bodyPartSmoothTime += 0.01f;
                        transform.localScale = curSize;
                        // Scale up camera
                        if (isPhotonPlayer == false)
                        {
                            var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                                findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                        }
                        else
                        {
                            if (photonView.IsMine)
                            {
                                _multiPlayerCamera.GetComponent<Camera>().orthographicSize +=
                                    _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                            }
                        }
                    }

                    foodUpCounter = 0;
                }
            }
            //	##### added by Morgan #####
            else if (obj.transform.CompareTag("Item"))
            {
                if (obj.transform.GetComponent<ParticleSystem>().startColor == new Color32(255, 0, 255, 255))
                {
                    Destroy(obj.gameObject);
                    curAmountOfItem--;
                    snakeWalkSpeed += 3.5f;
                    StartCoroutine("speedUpTime");
                }

                if (obj.transform.GetComponent<ParticleSystem>().startColor == new Color32(0, 255, 0, 255))
                {
                    Destroy(obj.gameObject);
                    curAmountOfItem--;
                    if (bodyParts.Count > 4)
                    {
                        isRunning = true;
                        StartCoroutine("punishTime");
                    }
                }
            }
            else if (obj.transform.CompareTag("Boundary"))
            {
                while (bodyParts.Count > 0)
                {
                    var lastIndex = bodyParts.Count - 1;
                    var lastBodyPart = bodyParts[lastIndex].transform;
                    bodyParts.RemoveAt(lastIndex);
                    var newFood = Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                        lastBodyPart.position, Quaternion.identity);
                    newFood.transform.parent = GameObject.Find("Foods").transform;
                    Destroy(lastBodyPart.gameObject);
                }

                var head = GameObject.FindGameObjectWithTag("Player");
                Destroy(head);
                //	##### added by Yue Chen #####
                if (PlayerPrefs.GetInt("removeAds", 0) == 0) ShowAd();
                SceneManager.LoadScene("Menu");
            }
            //WORKS ONLY ON ROBOTS. Isn't being used on the multi-player scene right now
            else if (obj.transform.CompareTag("Body") || obj.transform.CompareTag("Robot"))
            {
                int middleBodyPartNum = 0;
                int endBodyPartNum = 0;

                //Checking if number is even
                if (bodyParts.Count % 2 == 0)
                {
                    //Setting body part
                    middleBodyPartNum = Mathf.RoundToInt(bodyParts.Count / 2);
                    endBodyPartNum = bodyParts.Count - 1;
                }
                else
                {
                    //Setting body part
                    middleBodyPartNum = Mathf.RoundToInt(bodyParts.Count / 2);
                    endBodyPartNum = bodyParts.Count - 1;
                }

                while (bodyParts.Count > 0)
                {
                    var lastIndex = bodyParts.Count - 1;
                    var lastBodyPart = bodyParts[lastIndex].transform;
                    bodyParts.RemoveAt(lastIndex);
                    //Generating token
                    if (lastIndex == middleBodyPartNum || lastIndex == endBodyPartNum)
                    {
                        Instantiate(foodGenerateTarget[3],
                            lastBodyPart.position, Quaternion.identity);
                        //Generating token here
                        var tokenSpawned = Instantiate(foodGenerateTarget[3],
                            lastBodyPart.position, Quaternion.identity);

                        //Middle token spawned, token handling
                        if (lastIndex == middleBodyPartNum)
                        {
                            TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                            tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                            print($"Value of token is {tokenScript.GetTokenValue()}");
                        }

                        //Last token spawned, token handling
                        if (lastIndex == middleBodyPartNum)
                        {
                            TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                            tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                            print($"Value of token is {tokenScript.GetTokenValue()}");
                        }

                        Destroy(lastBodyPart.gameObject);
                        continue;
                    }

                    //Generating food particle
                    var newFood =
                        Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                            lastBodyPart.position,
                            Quaternion.identity);
                    newFood.transform.parent = GameObject.Find("Foods").transform;
                    Destroy(lastBodyPart.gameObject);
                }

                var head = GameObject.FindGameObjectWithTag("Player");

                //Generating token here
                // var spawnedToken = PhotonNetwork.Instantiate(
                //     Path.Combine("PhotonPrefabs", "GlowFood_DummyTokenPhoton"),
                //     head.transform.position, Quaternion.identity);
                // TokenScript tokenScriptRef = spawnedToken.GetComponent<TokenScript>();
                // tokenScriptRef.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                // print($"Current token value is {tokenScriptRef.GetTokenValue()}");

                //Destorying the head
                Destroy(head);

                StartCoroutine(ReturnToMainMenu());
                //	##### added by Yue Chen #####
            }
            //This is for collisions with a foreign player. Please look at this if you want to know
            //what the snake does when the snake collides with any foreign player. Do not use it for
            //any other purpose at all. This is basically the snake's death code
            else if (obj.transform.CompareTag("Snake") || obj.transform.CompareTag("Player"))
            {
                if (obj.gameObject.layer == 7)
                {
                    //We will return if we have already collided with the player
                    if (_hasCollidedWithPlayer == true) return;

                    _hasCollidedWithPlayer = true;

                    if (obj.transform.CompareTag("Player"))
                    {
                        DestroySnakeCompleteOnNetworkAndSpawnFoodTokens(obj.gameObject);
                        return;
                    }

                    //Passing the snake head to the function here
                    SnakeBodyActions bodyActions = obj.gameObject.GetComponent<SnakeBodyActions>();

                    //Returning if we do not find the body actions component
                    if (bodyActions == null) return;

                    GameObject snakeHead = bodyActions.parentHead;
                    DestroySnakeCompleteOnNetworkAndSpawnFoodTokens(snakeHead);
                }
            }
            else if (obj.transform.CompareTag("Snake"))
            {
                var temp = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, 1);
                gameObject.transform.position = temp;
            }

            return;
        }

        if (obj.transform.CompareTag("Food"))
        {
            length++;
            Destroy(obj.gameObject);
            curAmountOfFood--;
            if (foodUpCounter + 1 == foodUpArray[curSizeLevel])
            {
                if (SizeUp(foodCounter) == false)
                {
                    foodCounter++;
                    // The contents in 'if' shouldn't be executed in logic as we always have several body parts
                    Vector3 currentPos;
                    if (bodyParts.Count == 0)
                        currentPos = transform.position;
                    else
                        currentPos = bodyParts[bodyParts.Count - 1].position;
                    var newPart = Instantiate(addBodyPart, currentPos, Quaternion.identity);
                    newPart.parent = GameObject.Find("SnakeBodies").transform;
                    newPart.gameObject.layer = 3;
                    bodyParts.Add(newPart);
                }
                else
                {
                    curSize += Vector3.one * growRate;
                    bodyPartSmoothTime += 0.01f;
                    transform.localScale = curSize;
                    // Scale up camera
                    if (isPhotonPlayer == false)
                    {
                        var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                        findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                    }
                    else
                    {
                        if (photonView.IsMine)
                        {
                            _multiPlayerCamera.GetComponent<Camera>().orthographicSize +=
                                _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                        }
                    }
                }

                foodUpCounter = 0;
            }
            else
            {
                foodUpCounter++;
            }
        }
        else if (obj.transform.CompareTag("Token"))
        {
            TokenScript tokenScript = obj.gameObject.GetComponent<TokenScript>();
            float tokensToAdd = tokenScript.GetTokenValue();
            tokensCollected += tokensToAdd;
            Destroy(obj.gameObject);
            curAmountOfFood--;
            if (foodUpCounter + 1 == foodUpArray[curSizeLevel])
            {
                if (SizeUp(foodCounter) == false)
                {
                    tokensToAdd = tokenScript.GetTokenValue();
                    tokensCollected += tokensToAdd;
                    // The contents in 'if' shouldn't be executed
                    // in logic as we always have several body parts
                    Vector3 currentPos;
                    if (bodyParts.Count == 0)
                        currentPos = transform.position;
                    else
                        currentPos = bodyParts[bodyParts.Count - 1].position;
                    var newPart = Instantiate(addBodyPart, currentPos, Quaternion.identity);
                    newPart.parent = GameObject.Find("SnakeBodies").transform;
                    newPart.gameObject.layer = 3;
                    bodyParts.Add(newPart);
                }
                else
                {
                    curSize += Vector3.one * growRate;
                    bodyPartSmoothTime += 0.01f;
                    transform.localScale = curSize;
                    // Scale up camera
                    if (isPhotonPlayer == false)
                    {
                        var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                        findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                    }
                    else
                    {
                        if (photonView.IsMine)
                        {
                            _multiPlayerCamera.GetComponent<Camera>().orthographicSize +=
                                _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                        }
                    }
                }

                foodUpCounter = 0;
            }
            else
            {
                tokensCollected += tokenScript.GetTokenValue();
            }
        }
        //	##### added by Morgan #####
        else if (obj.transform.CompareTag("Item"))
        {
            if (obj.transform.GetComponent<ParticleSystem>().startColor == new Color32(255, 0, 255, 255))
            {
                Destroy(obj.gameObject);
                curAmountOfItem--;
                snakeWalkSpeed += 3.5f;
                StartCoroutine("speedUpTime");
            }

            if (obj.transform.GetComponent<ParticleSystem>().startColor == new Color32(0, 255, 0, 255))
            {
                Destroy(obj.gameObject);
                curAmountOfItem--;
                if (bodyParts.Count > 4)
                {
                    isRunning = true;
                    StartCoroutine("punishTime");
                }
            }
        }
        else if (obj.transform.CompareTag("Boundary"))
        {
            while (bodyParts.Count > 0)
            {
                var lastIndex = bodyParts.Count - 1;
                var lastBodyPart = bodyParts[lastIndex].transform;
                bodyParts.RemoveAt(lastIndex);
                var newFood = Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                    lastBodyPart.position, Quaternion.identity);
                newFood.transform.parent = GameObject.Find("Foods").transform;
                Destroy(lastBodyPart.gameObject);
            }

            var head = GameObject.FindGameObjectWithTag("Player");
            Destroy(head);
            //	##### added by Yue Chen #####
            if (PlayerPrefs.GetInt("removeAds", 0) == 0) ShowAd();
            SceneManager.LoadScene("Menu");
        }
        else if (obj.transform.CompareTag("Body") || obj.transform.CompareTag("Robot"))
        {
           SpawnPlayerSnakeScript.Instance.levelFail.gameObject.SetActive(true);
            int middleBodyPartNum = 0;
            int endBodyPartNum = 0;

            //Checking if number is even
            if (bodyParts.Count % 2 == 0)
            {
                print("Snake that died had even number of parts" +
                      " and body parts were " + bodyParts.Count);

                //Setting body part
                middleBodyPartNum = Mathf.RoundToInt(bodyParts.Count / 2);
                endBodyPartNum = bodyParts.Count - 1;
            }
            else
            {
                print("Snake that died had an odd number of parts" +
                      " and body parts were " + bodyParts.Count);

                //Setting body part
                middleBodyPartNum = Mathf.RoundToInt(bodyParts.Count / 2);
                endBodyPartNum = bodyParts.Count - 1;
            }

            while (bodyParts.Count > 0)
            {
                var lastIndex = bodyParts.Count - 1;
                var lastBodyPart = bodyParts[lastIndex].transform;
                bodyParts.RemoveAt(lastIndex);
                //Generating token
                // if (lastIndex == middleBodyPartNum || lastIndex == endBodyPartNum)
                // {
                //     Instantiate(foodGenerateTarget[3],
                //         lastBodyPart.position, Quaternion.identity);
                //     //Generating token here
                //     var tokenSpawned = Instantiate(foodGenerateTarget[3],
                //         lastBodyPart.position, Quaternion.identity);
                //
                //     //Middle token spawned, token handling
                //     if (lastIndex == middleBodyPartNum)
                //     {
                //         TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                //         tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                //     }
                //
                //     //Last token spawned, token handling
                //     if (lastIndex == middleBodyPartNum)
                //     {
                //         TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                //         tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                //     }
                //
                //     Destroy(lastBodyPart.gameObject);
                //     continue;
                // }

                //Generating new food
                var newFood =
                    Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)], lastBodyPart.position,
                        Quaternion.identity);
                newFood.transform.parent = GameObject.Find("Foods").transform;
                Destroy(lastBodyPart.gameObject);
            }

            var head = GameObject.FindGameObjectWithTag("Player");

            //Generating token here
            var spawnedToken = Instantiate(foodGenerateTarget[3],
                head.transform.position, Quaternion.identity);
            TokenScript tokenScriptRef = spawnedToken.GetComponent<TokenScript>();
            tokenScriptRef.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));

            Destroy(head);

            StartCoroutine(ReturnToMainMenu());
            //	##### added by Yue Chen #####
        }
        else if (obj.transform.CompareTag("Snake"))
        {
            var temp = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, 1);
            gameObject.transform.position = temp;
        }
    }

    private void OnTriggerEnter(Collider obj)
    {
        //Runs only if this is the photon player player
        if (isPhotonPlayer)
        {
            //Returns if we are not the local player
            if (photonView.IsMine == false) return;
            
            //If we collide with the token gameObject
            if (obj.transform.CompareTag("Token"))
            {
                //Getting the token script and the value to add
                TokenScript tokenScript = obj.gameObject.GetComponent<TokenScript>();
                float tokensToAdd = tokenScript.GetTokenValue();

                //Trying to get the popup text
                GameObject popupTextMeshGameObject = GameObject.FindGameObjectWithTag("TokenPopContainer");
                
                //When the snake has already collided
                if (tokenScript.localSnakeHasCollided == true)
                {
                    //If we have found the popupTextMesh, we will tell the player that he/she has already
                    //collided with this token
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        popupText.text = "Cannot Collect Anymore!";
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }
                    
                    return;
                }
                
                //Getting the photon view id
                int tokenPhotonViewId 
                    = tokenScript.gameObject.GetComponent<PhotonView>().ViewID;
                
                //Telling the game that the token has collided with the player
                tokenScript.localSnakeHasCollided = true;

                int playersBetAmount = PlayerPrefs.GetInt("BetAmount");

                //If the token script bet value is greater than the player's bet value
                if (tokenScript.GetTokenBetValue() > PlayerPrefs.GetInt("BetAmount"))
                {
                    //Sending the RPC to deduct the amount
                    photonView.RPC(
                        "DeductTokenValue", 
                        RpcTarget.AllBuffered,
                        tokenPhotonViewId,
                        playersBetAmount
                    );

                    //Telling the game that the player can only collect up to bet amount
                    tokensCollected += playersBetAmount;
                    
                    //Handling token inventory
                    float currentTokensInInventory = PlayerPrefs.GetFloat("PlayerTokens");
                    currentTokensInInventory += playersBetAmount;
                    PlayerPrefs.SetFloat("PlayerTokens", currentTokensInInventory);
                    
                    //Updating player data on playfab backend
                    var request = new UpdateUserDataRequest
                    {
                        Data = new Dictionary<string, string>
                        {
                            { "TokensCollected", currentTokensInInventory.ToString(CultureInfo.InvariantCulture) }
                        }
                    };
                    PlayFabClientAPI.UpdateUserData(
                        request,
                        result =>
                        {
                            print("Tokens data was successfully added to the servers");
                        },
                        error =>
                        {
                            print("Data could not be updated on the server due to " + error.ErrorMessage);
                        } 
                    );
                    
                    print("Only a portion of the token could be collected");
                    
                    //If we have found the text object, we will tell the player how much they have collected
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        //Displaying the amount that the player has collected
                        popupText.text = playersBetAmount.ToString();
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }

                    //Returning as we do not want to destroy the token on the network as only its value
                    //has been deducted
                    return;
                }

                //If the token has a bet value less than the player's bet value, we will add up all of the
                //amount of the token and then destroy it
                if (tokenScript.GetTokenBetValue() < PlayerPrefs.GetInt("BetAmount"))
                {
                    //Collecting the entire amount that the token has
                    tokensCollected += tokensToAdd;

                    //Handling token inventory
                    float currentTokensInInventory = PlayerPrefs.GetFloat("PlayerTokens");
                    currentTokensInInventory += tokensToAdd;
                    PlayerPrefs.SetFloat("PlayerTokens", currentTokensInInventory);
                    
                    //Updating player data on playfab backend
                    var request = new UpdateUserDataRequest
                    {
                        Data = new Dictionary<string, string>
                        {
                            { "TokensCollected", currentTokensInInventory.ToString() }
                        }
                    };
                    PlayFabClientAPI.UpdateUserData(
                        request,
                        result =>
                        {
                            print("Tokens data was successfully added to the servers");
                        },
                        error =>
                        {
                            print("Data could not be updated on the server due to " + error.ErrorMessage);
                        } 
                    );
                    
                    //If we have found the text object, we will tell the player how much they have collected
                    if (popupTextMeshGameObject == true)
                    {
                        TokenPopupContainerScript tokenPopupContainerScript
                            = popupTextMeshGameObject.GetComponent<TokenPopupContainerScript>();
                        TextMesh popupText = tokenPopupContainerScript.ActivateAndReturnNextPopupText();

                        //Displaying the amount that the player has collected
                        popupText.text = tokensToAdd.ToString();
                        
                        print("All of the token was collected");
                        
                        //Setting popup text position including local position
                        var thisGameObjectPosition = transform.position;
                        var popupTextTransform = popupText.transform;
                        popupTextTransform.position = new Vector3(
                            thisGameObjectPosition.x,
                            thisGameObjectPosition.y,
                            0
                        );
                        var localPositionToSetToPopupText = popupTextTransform.localPosition;
                        localPositionToSetToPopupText = new Vector3(
                            localPositionToSetToPopupText.x,
                            localPositionToSetToPopupText.y,
                            -25
                        );
                        popupTextTransform.localPosition = localPositionToSetToPopupText;
                    }
                }

                //Destroying token
                int tokenId = obj.gameObject.GetComponent<PhotonView>().ViewID;
                //Photon view RPC
                photonView.RPC(
                    "DestroyObjectOnNetwork",
                    RpcTarget.MasterClient,
                    tokenId
                );
                obj.gameObject.SetActive(false);
                curAmountOfFood--;

                if (foodUpCounter + 1 == foodUpArray[curSizeLevel])
                {
                    if (SizeUp(foodCounter) == false)
                    {
                        Vector3 currentPos;
                        if (bodyParts.Count == 0)
                            currentPos = transform.position;
                        else
                            currentPos = bodyParts[bodyParts.Count - 1].position;
                        var newPart = PhotonNetwork.Instantiate(
                            Path.Combine("PhotonPrefabs", "SnakeBodyPhoton"),
                            currentPos, Quaternion.identity).transform;
                        newPart.parent = GameObject.Find("SnakeBodies").transform;
                        newPart.gameObject.layer = 3;
                        int photonId = newPart.gameObject.GetComponent<PhotonView>().ViewID;
                        photonView.RPC("AddBodyPartWithViewId", RpcTarget.AllBuffered, photonId);
                    }
                    else
                    {
                        curSize += Vector3.one * growRate;
                        bodyPartSmoothTime += 0.01f;
                        transform.localScale = curSize;
                        // Scale up camera
                        if (isPhotonPlayer == false)
                        {
                            var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                                findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                        }
                        else
                        {
                            if (photonView.IsMine)
                            {
                                _multiPlayerCamera.GetComponent<Camera>().orthographicSize +=
                                    _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                            }
                        }
                    }

                    foodUpCounter = 0;
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //Only runs if the player is a photon player
        if (isPhotonPlayer == true)
        {
            //Returns if photon view is not mine
            if (photonView.IsMine == false) return;

            //If colliding with another gameObject
            if (!other.CompareTag("Player") || !other.CompareTag("Robot") ||
                !other.CompareTag("Snake"))
            {
                _isCollidingWithGameObject = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Only runs if the player is a photon player
        if (isPhotonPlayer == true)
        {
            //Returns if photon view is not mine
            if (photonView.IsMine == false) return;

            //If colliding with another gameObject
            if (!other.CompareTag("Player") || !other.CompareTag("Robot") ||
                !other.CompareTag("Snake"))
            {
                _isCollidingWithGameObject = false;
                print($"Colliding object is {other.gameObject.name}");
            }
        }
    }

    #endregion

    #region Snake Collider and Sprite Renderer Functions

    [Header("Snake Collider and Sprite Renderer Variables")]
    //When true, tells the game that the snake is colliding with a collider
    private bool _isCollidingWithGameObject = false;

    /// <summary>
    /// Enable the colliders on the snake
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnableSnakeColliders()
    {
        while (_isCollidingWithGameObject == true)
        {
            yield return new WaitForEndOfFrame();
        }

        //Disable main head collider
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = false;

        //Disabling side colliders
        BoxCollider sideCollider = transform.GetChild(1).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = false;
        sideCollider = transform.GetChild(2).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = false;

        MakeSnakeOpaque();

        //For each loop to enable all body colliders + make the sprites transparent
        foreach (var snakeBodyActionsScript in bodyParts.Select
            (bodyPart => bodyPart.gameObject.GetComponent<SnakeBodyActions>()))
        {
            snakeBodyActionsScript.StartCoroutine(snakeBodyActionsScript.EnableLocalBodyCollider());
        }
    }

    /// <summary>
    /// Enables the snake colliders on the foreign player
    /// </summary>
    private void EnableSnakeCollidersOnForeignPlayer()
    {
        //Disable main head collider
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = false;

        //Disabling side colliders
        BoxCollider sideCollider = transform.GetChild(1).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = false;
        sideCollider = transform.GetChild(2).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = false;

        MakeSnakeOpaque();

        //For each loop to enable all body colliders + make the sprites transparent
        foreach (var snakeBodyActionsScript in bodyParts.Select
            (bodyPart => bodyPart.gameObject.GetComponent<SnakeBodyActions>()))
        {
            snakeBodyActionsScript.EnableColliderOnForeignBody();
        }
    }

    /// <summary>
    /// Disables all colliders
    /// </summary>
    private void DisableSnakeColliders()
    {
        print("Disabled snake colliders");
        
        //Disable main head collider
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = true;

        //Disabling side colliders
        BoxCollider sideCollider = transform.GetChild(1).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = true;
        sideCollider = transform.GetChild(2).gameObject.GetComponent<BoxCollider>();
        sideCollider.isTrigger = true;

        MakeSnakeHeadTransparent();

        //For each loop to disable all body colliders + make the sprites transparent
        foreach (Transform bodyPart in bodyParts)
        {
            SnakeBodyActions snakeBodyActionsScript = bodyPart.gameObject.GetComponent<SnakeBodyActions>();
            snakeBodyActionsScript.DisableLocalCollider();
        }
    }

    /// <summary>
    /// Makes the snake head transparent
    /// </summary>
    private void MakeSnakeHeadTransparent()
    {
        SpriteRenderer snakeHeadTransparent =
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        snakeHeadTransparent.color = new Color(
            255,
            255,
            255,
            0.35f
        );
    }

    /// <summary>
    /// Makes the snake head opaque
    /// </summary>
    private void MakeSnakeOpaque()
    {
        SpriteRenderer snakeHeadTransparent =
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        snakeHeadTransparent.color = new Color(
            255,
            255,
            255,
            1.0f
        );
    }

    #endregion

    #region UI Buttons

    private IEnumerator ReturnToMainMenu()
    {
        print("Returning to main menu");

        yield return new WaitForSeconds(3.5f);

        SceneManager.LoadScene("Menu", LoadSceneMode.Single);

        yield break;
    }

    #endregion

    #region Special Effects on Snake Head

    //	##### added by Morgan #####
    private IEnumerator speedUpTime()
    {
        yield return new WaitForSeconds(2);
        snakeWalkSpeed -= 3.5f;
    }

    private IEnumerator punishTime()
    {
        yield return new WaitForSeconds(2);
        isRunning = false;
        snakeWalkSpeed = 3.5f;
    }

    #endregion

    #region Body Spawn and Scale Up Related

    /// <summary>
    /// A function that will spawn body parts and will also assign the body parts
    /// to the snake's head
    /// </summary>
    private void SpawnBodyParts()
    {
        if (isPhotonPlayer)
        {
            GameObject spawnedBodyPart = PhotonNetwork.Instantiate(
                Path.Combine("PhotonPrefabs", "SnakeBodiesPhoton"),
                transform.position,
                Quaternion.identity
            );

            spawnedBodyPart.name = "SnakeBodies";
            spawnedBodyPart.layer = 3;

            //Getting id of the first body part
            int bodyId =
                spawnedBodyPart.transform.GetChild(0).gameObject.GetComponent<PhotonView>().ViewID;
            photonView.RPC("AddBodyPartWithViewId", RpcTarget.AllBuffered, bodyId);
            bodyId =
                spawnedBodyPart.transform.GetChild(1).gameObject.GetComponent<PhotonView>().ViewID;
            photonView.RPC("AddBodyPartWithViewId", RpcTarget.AllBuffered, bodyId);

            return;
        }

        //Spawning body parts
        GameObject spawnedBodyParts = Instantiate(snakeBodyParts, transform.position, Quaternion.identity);
        spawnedBodyParts.name = "SnakeBodies";

        //Changing the layer as required
        spawnedBodyParts.transform.GetChild(0).gameObject.layer = 3;
        spawnedBodyParts.transform.GetChild(1).gameObject.layer = 3;

        //Adding body parts
        bodyParts.Add(spawnedBodyParts.transform.GetChild(0));
        bodyParts.Add(spawnedBodyParts.transform.GetChild(1));
    }

    /* When losing body parts, snake size down*/
    private void SnakeScaleChange()
    {
        if (curSizeLevel > 0 && foodCounter <= sizeUpArray[curSizeLevel - 1])
        {
            curSizeLevel--;
            curSize -= Vector3.one * growRate;
            bodyPartSmoothTime -= 0.01f;
            transform.localScale = curSize;
            // Scale down camera
            if (isPhotonPlayer == false)
            {
                var camera = GameObject.FindGameObjectWithTag("MainCamera");
                camera.GetComponent<Camera>().orthographicSize -=
                    camera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
            }
            else
            {
                if (photonView.IsMine)
                {
                    _multiPlayerCamera.GetComponent<Camera>().orthographicSize -=
                        _multiPlayerCamera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
                }
            }
        }
    }

    /* Figure out whether snake size increases after eating*/
    private bool SizeUp(int x)
    {
        if (x == sizeUpArray[curSizeLevel])
        {
            curSizeLevel++;
            return true;
        }

        return false;
    }

    /* Set the size and smooth time of snake body parts every frame*/
    private void SetBodySizeAndSmoothTime()
    {
        transform.localScale = curSize;
        if (snakeWalkSpeed >= snakeRunSpeed)
            bodySmoothTime = runBodyPartSmoothTime;
        else
            bodySmoothTime = bodyPartSmoothTime - (boostSpeed / 40);
        foreach (var part in bodyParts)
        {
            part.localScale = curSize;
            part.GetComponent<SnakeBodyActions>().smoothTime = bodySmoothTime;
        }
    }

    // create robots
    private void GenerateRobotBeforeBegin()
    {
        var i = 0;
        while (i < 20)
        {
            var r = Random.Range(0, 2);
            Vector3 robotPos;
            if (r == 0)
                robotPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else
                robotPos = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), 0);
            var newRobot = Instantiate(robotGenerateTarget[Random.Range(0, robotGenerateTarget.Length)],
                robotPos, Quaternion.identity);
            newRobot.name = "Robot" + i;
            Robots.Add(newRobot);
            newRobot.GetComponent<RobotAction>().SkinId = Random.Range(1, 4);
            newRobot.transform.parent = GameObject.Find("Robots").transform;
            curAmountOfRobot++;
            i++;
        }
    }

    #endregion

    #region Level Food Spawning

    /* Gernate 200 food points before game start*/
    private void GenerateFoodBeforeBegin()
    {
        var i = 0;
        while (i < 200)
        {
            var r = Random.Range(0, 2);
            Vector3 foodPos;
            if (r == 0)
                foodPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else
                foodPos = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), 0);
            var newFood = Instantiate(foodGenerateTarget[Random.Range(0, 2)], foodPos,
                Quaternion.identity);
            newFood.transform.parent = GameObject.Find("Foods").transform;
            curAmountOfFood++;
            i++;
        }
    }

    private void GenerateStoneBeforeBegin()
    {
        var i = 0;
        while (i < 400)
        {
            var stonePos = new Vector3(Random.Range(-120, 120), Random.Range(-120, 120), 0);
            var newStone = Instantiate(stone, stonePos, Quaternion.identity);
            newStone.transform.parent = GameObject.Find("Stones").transform;
            curAmountOfFood++;
            i++;
        }
    }

    /// <summary>
    /// Generates food on the network
    /// </summary>
    [PunRPC]
    private void GenerateFoodOnNetwork()
    {
        StartCoroutine(MultiplayerRunGenerateFoodItemNormal(foodGenerateEveryXSecond));
    }

    /// <summary>
    /// Generates food for the player
    /// </summary>
    private void GenerateFoodAndItem()
    {
        if (isPhotonPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(MultiplayerRunGenerateFoodItem(foodGenerateEveryXSecond));
            }

            return;
        }

        StartCoroutine("RunGenerateFoodAndItem", foodGenerateEveryXSecond);
    }

    /// <summary>
    /// Generates a photon view using the normal instantiate method
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator MultiplayerRunGenerateFoodItemNormal(float time)
    {
        yield return new WaitForSeconds(time);
        StopCoroutine(MultiplayerRunGenerateFoodItem(0));

        if (curAmountOfFood < maxAmountOfFood)
        {
            var r = Random.Range(0, 4);

            //Getting food spawn position
            Vector2 foodPos;
            if (r == 0)
                foodPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else if (r <= 1)
                foodPos = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), 0);
            else if (r <= 2)
                foodPos = new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), 0);
            else
                foodPos = new Vector3(Random.Range(-120, 120), Random.Range(-120, 120), 0);

            Transform newFood;
            var foodToSpawn = Random.Range(0, 3);

            switch (foodToSpawn)
            {
                case 0:
                    newFood = Instantiate(
                        foodGenerateTarget[0],
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                case 1:
                    newFood = Instantiate(
                        foodGenerateTarget[1],
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                case 2:
                    newFood = Instantiate(
                        foodGenerateTarget[2],
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                default:
                    newFood = Instantiate(
                        foodGenerateTarget[0],
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
            }

            newFood.transform.parent = GameObject.Find("Items").transform;
            curAmountOfFood++;
        }

        yield break;
    }

    /// <summary>
    /// Stops the multiplayer food generation on all players
    /// </summary>
    [PunRPC]
    public void StopMultiplayerRunGenerateFoodItemRoutine()
    {
        StopCoroutine(MultiplayerRunGenerateFoodItemNormal(0));
    }

    /// <summary>
    /// Generates food items in the multi-player scene
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator MultiplayerRunGenerateFoodItem(float time)
    {
        yield return new WaitForSeconds(time);
        StopCoroutine(MultiplayerRunGenerateFoodItem(0));

        if (curAmountOfFood < maxAmountOfFood)
        {
            var r = Random.Range(0, 4);

            //Getting food spawn position
            Vector2 foodPos;
            if (r == 0)
                foodPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else if (r <= 1)
                foodPos = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), 0);
            else if (r <= 2)
                foodPos = new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), 0);
            else
                foodPos = new Vector3(Random.Range(-120, 120), Random.Range(-120, 120), 0);


            Transform newFood;
            var foodToSpawn = Random.Range(0, 3);
            switch (foodToSpawn)
            {
                case 0:
                    newFood = PhotonNetwork.Instantiate(
                        Path.Combine("PhotonPrefabs", "GlowFood_GreenPhoton"),
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                case 1:
                    newFood = PhotonNetwork.Instantiate(
                        Path.Combine("PhotonPrefabs", "GlowFood_RedPhoton"),
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                case 2:
                    newFood = PhotonNetwork.Instantiate(
                        Path.Combine("PhotonPrefabs", "GlowFood_YellowPhoton"),
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
                default:
                    newFood = PhotonNetwork.Instantiate(
                        Path.Combine("PhotonPrefabs", "GlowFood_GreenPhoton"),
                        foodPos,
                        Quaternion.identity
                    ).transform;
                    break;
            }

            newFood.transform.parent = GameObject.Find("Foods").transform;
            curAmountOfFood++;
        }

        yield break;
    }

    private IEnumerator RunGenerateFoodAndItem(float time)
    {
        yield return new WaitForSeconds(time);
        StopCoroutine("RunGenerateFoodAndItem");
        if (curAmountOfFood < maxAmountOfFood)
        {
            var r = Random.Range(0, 4);
            Vector3 foodPos;
            if (r == 0)
                foodPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else if (r <= 1)
                foodPos = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), 0);
            else if (r <= 2)
                foodPos = new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), 0);
            else
                foodPos = new Vector3(Random.Range(-120, 120), Random.Range(-120, 120), 0);
            var newFood = Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)], foodPos,
                Quaternion.identity);
            newFood.transform.parent = GameObject.Find("Foods").transform;
            curAmountOfFood++;
        }

        if (curAmountOfItem < maxAmountOfItem)
        {
            var r = Random.Range(0, 4);
            Vector3 itemPos;
            if (r == 0)
                itemPos = new Vector3(Random.Range(-30, 30), Random.Range(-30, 30), 0);
            else if (r <= 1)
                itemPos = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), 0);
            else if (r <= 2)
                itemPos = new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), 0);
            else
                itemPos = new Vector3(Random.Range(-120, 120), Random.Range(-120, 120), 0);
            var newItem = Instantiate(itemGenerateTarget[Random.Range(0, itemGenerateTarget.Length)], itemPos,
                Quaternion.identity);
            newItem.transform.parent = GameObject.Find("Items").transform;
            curAmountOfItem++;
        }
    }

    #endregion

    #region Snake Movement and De-spawn body part

    //When true, tells the game that the game is reading
    private bool _isReading = false;

    private float _syncTime = 0.25f;

    /// <summary>
    /// Syncs the rotation and position on the foreign player
    /// </summary>
    private void SnakeMovementOnForeignPlayer()
    {
        //Returning if we are not reading at the moment
        if (_isReading == false) return;

        //Syncing the position at the very start
        _syncTime -= Time.deltaTime;
        if (_syncTime > 0)
        {
            transform.position = _foreignPlayerPosition;
            transform.LookAt(pointInWorld);
            return;
        }

        //Turning the foreign player towards the direction
        direction = Vector3.Slerp(direction, mousePosition - transform.position, Time.deltaTime * 2.5f);
        direction.z = 0;
        pointInWorldForeignLagCompensation = Vector3.MoveTowards(
            pointInWorldForeignLagCompensation,
            pointInWorld,
            150.0f * Time.deltaTime
        );
        transform.LookAt(pointInWorldForeignLagCompensation);

        //Moving towards a direction
        transform.position += transform.forward * snakeWalkSpeed * Time.deltaTime;
    }

    /* Make the snake head move forward all the time*/
    private void SnakeMove()
    {
        transform.position += transform.forward * snakeWalkSpeed * Time.deltaTime;
    }

    /* Make the camera follow the snake when it moves*/
    private void CameraFollowSnake()
    {
        if (isPhotonPlayer == false)
        {
            var camera = GameObject.FindGameObjectWithTag("MainCamera").gameObject.transform;
            var velocity = Vector3.zero;
            // Reach from current position to target position smoothly
            camera.position = Vector3.SmoothDamp(camera.position,
                new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10)
                , ref velocity, cameraSmoothTime);
        }
        else if (photonView.IsMine)
        {
            var camera = _multiPlayerCamera.gameObject.transform;
            var velocity = Vector3.zero;
            // Reach from current position to target position smoothly
            camera.position = Vector3.SmoothDamp(camera.position,
                new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10)
                , ref velocity, cameraSmoothTime);
        }
    }

    private void SnakeRun()
    {
        if (bodyParts.Count > 2)
        {
            if (Input.GetMouseButtonDown(0))
            {
                t2 = Time.realtimeSinceStartup;
                if (t2 - t1 < 0.2)
                {
                    isRunning = true;
                    snakeWalkSpeed = snakeRunSpeed;
                }

                t1 = t2;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isRunning = false;
                snakeWalkSpeed = 3.5f + boostSpeed;
            }
        }
        else
        {
            isRunning = false;
            snakeWalkSpeed = 3.5f + boostSpeed;
        }
    }

    private IEnumerator LosingBodyParts()
    {
        yield return new WaitForSeconds(0.8f); // Every 0.8 second lose one body part
        StopCoroutine("LosingBodyParts");
        var lastIndex = bodyParts.Count - 1;
        var lastBodyPart = bodyParts[lastIndex].transform;
        bodyParts.RemoveAt(lastIndex);
        Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)], lastBodyPart.position,
            Quaternion.identity);
        Destroy(lastBodyPart.gameObject);
        curAmountOfFood++;
        foodCounter--;
        length--;
        SnakeScaleChange();
    }

    /* If snake is running, then glowing*/
    private void SnakeGlowing(bool isRunning)
    {
        foreach (var part in bodyParts) part.Find("Glowing").gameObject.SetActive(isRunning);
    }

    /* Choose the way of snake moving*/
    private void ChooseControlMethod(int id)
    {
        switch (id)
        {
            case 1:
                MouseControlSnake();
                break;
            case 2:
                StickControl();
                break;
            case 3:
                DirectionControl();
                break;
        }
    }

    private void MouseControlSnake()
    {
        if (isPhotonPlayer == false)
        {
            var ray = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>()
                .ScreenPointToRay(Input.mousePosition);
            RaycastHit hit; // Store the first obj touched by ray
            Physics.Raycast(ray, out hit, 50.0f); // The third parameter is the max distance
            mousePosition = new Vector3(hit.point.x, hit.point.y, 0);
            direction = Vector3.Slerp(direction, mousePosition - transform.position, Time.deltaTime * 2.5f);
            direction.z = 0;
            pointInWorld = direction.normalized * radius + transform.position;
            transform.LookAt(pointInWorld);
        }
        else if (photonView.IsMine)
        {
            var ray = _multiPlayerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit; // Store the first obj touched by ray
            Physics.Raycast(ray, out hit, 50.0f); // The third parameter is the max distance
            mousePosition = new Vector3(hit.point.x, hit.point.y, 0);
            direction = Vector3.Slerp(direction, mousePosition - transform.position, Time.deltaTime * 2.5f);
            direction.z = 0;
            pointInWorld = direction.normalized * radius + transform.position;
            transform.LookAt(pointInWorld);
        }
    }

    /* Use virtual joystick to control the direction of snake*/
    private void StickControl()
    {
    }

    /* Slide finger to control the direction of snake*/
    private void DirectionControl()
    {
    }

    private void SnakeMoveAdjust()
    {
        var temp = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, 0);
        gameObject.transform.position = temp;
    }

    #endregion

    #region Snake Body Color Functions

    private void ColorSnake(int id)
    {
        switch (id)
        {
            case 1:
                GreenSnake();
                break;
            case 2:
                SeaBlueSnake();
                break;
            case 3:
                FireOrangeSnake();
                break;
        }
    }


    /// <summary>
    /// An RPC that will color the snake
    /// </summary>
    /// <param name="id"></param>
    [PunRPC]
    private void ColorSnakeRPC(int id)
    {
        switch (id)
        {
            case 1:
                for (var i = 0; i < bodyParts.Count; i++)
                {
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite
                        = greenSnakeTexture;
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
                }

                break;
            case 2:
                for (var i = 0; i < bodyParts.Count; i++)
                {
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite
                        = blueSnakeTexture;
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
                }

                break;
            case 3:
                for (var i = 0; i < bodyParts.Count; i++)
                {
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite
                        = orangeSnakeTexture;
                    bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
                }

                break;
        }
    }

    private void GreenSnake()
    {
        //For loop to load the correct skins on the map
        for (var i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite = snakeBodyTexture;
            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
        }

        // for (int i = 0; i < bodyParts.Count; i++)
        // {
        //     if (i % 2 == 0)
        //     {
        //         //bodyParts[i].GetComponent<Renderer>().material = blue;
        //         bodyParts[i].GetComponent<Renderer>().enabled = false;
        //         bodyParts[i].GetComponent<MeshRenderer>().enabled = false;
        //         bodyParts[i].GetComponent<SnakeBodyActions>().
        //             bodySpriteRenderer.sprite = snakeTexture;
        //     }
        // }
    }

    private void SeaBlueSnake()
    {
        //For loop to load the correct skins on the map
        for (var i = 0; i < bodyParts.Count; i++)
        {
            //Returning if the body part has been destroyed
            if (bodyParts[i] == null) break;

            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite = snakeBodyTexture;
            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
        }
    }

    private void FireOrangeSnake()
    {
        //For loop to load the correct skins on the map
        for (var i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sprite = snakeBodyTexture;
            bodyParts[i].GetComponent<SnakeBodyActions>().bodySpriteRenderer.sortingOrder = i;
        }
    }

    #endregion

    #region UI Related

    // added by Yue Chen
    public void ShowAd()
    {
    }

    public void SetScore(int curScore)
    {
        int bestScore;
        PlayerPrefs.SetInt("FinalScore", curScore);
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScore < curScore) PlayerPrefs.SetInt("BestScore", curScore);
    }

    #endregion

   
}
