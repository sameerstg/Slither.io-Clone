using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using SnakeScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Photon_Multiplayer_Scripts.Photon.Gameplay_Scripts
{
    /// <summary>
    /// A script that will be used on the Photon multiplayer scene to control the snake
    /// </summary>
    public class SnakeMovementPhoton : MonoBehaviour
    {
        #region Photon Variables

        [Header("Photon Variables")]
        //Photon view
        private PhotonView _photonView;

        #endregion

        #region Variables

        //Body parts spawn
        public GameObject snakeBodyParts;

        //The texture of this snake head
        [FormerlySerializedAs("snakeTexture")] public Sprite snakeBodyTexture;

        public List<Transform>
            bodyParts = new List<Transform>(); // Records the location information of body parts of the snake

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
        public int length;
        public float tokensCollected;

        public int curAmountOfRobot, maxAmountOfRobot = 30; // The max amount of robots in the map
        public GameObject[] robotGenerateTarget; // Store the objects of robot snakes

        /* Generate food points every few seconds until there are enough points on the map*/
        public int curAmountOfFood, maxAmountOfFood = 600; // The max amount of food in the map
        public int curAmountOfItem, maxAmountOfItem = 60; // The max amount of item in the map
        public GameObject[] foodGenerateTarget; // Store the objects of food points
        public GameObject[] itemGenerateTarget; // Store the objects of item
        public GameObject stone; // the object of stone

        /* Choose the skin of snake*/
        public Material blue, red, orange;

        private float
            bodyPartSmoothTime = 0.2f; //Called in OnCollisionEnter(), the same value as in SnakeBodyActions.cs

        private float bodySmoothTime; // Called in SetBodySizeAndSmoothTime()

        private readonly float
            cameraGrowRate =
                0.03f; // Called in OnCollisionEnter(), when snake gets larger, camera size gets larger as well

        private readonly float cameraSmoothTime = 0.13f; // Called in CameraFollowSnake()

        private readonly float foodGenerateEveryXSecond = 0.1f; // Generate a food point every 3 seconds

        //Called in OnCollisionEnter(), determine after eating how many food points will the snake add a body part
        private readonly int[] foodUpArray = { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 10 };
        private readonly float growRate = 0.1f; //Called in OnCollisionEnter(), how much to grow snake size

        private bool isRunning; // Called in SnakeRun()

        //	##### added by Yue Chen #####
        private int moveWay; // It determines how to control the movement of snake, gained from initial interface

        private string nickName;

        /* Sanke moves toward finger*/
        private Vector3 pointInWorld, mousePosition, direction;
        private readonly float radius = 20.0f;

        //Called in SizeUp(), determine after eating how much food the snake will grow its size
        private readonly int[] sizeUpArray =
            { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 5096, 10192, 20348, 999999 };

        private int skinID; // It determines the skin of the snake, gained from initial interface
        private readonly float snakeRunSpeed = 7.0f; // Called in SnakeRun()

        private float snakeWalkSpeed = 3.5f; // Called in SnakeMove()

        /* Make the snake run when it should run, and lose parts*/
        private float t1;
        private float t2;

        #endregion

        #region Unity Functions

        private void Awake()
        {
            if (_photonView.IsMine == false)
            {
                return;
            }

            SpawnBodyParts();
        }

        private void Start()
        {
            if (_photonView.IsMine == false)
            {
                return;
            }

            //Generating food in the game if we are the master client
            if (PhotonNetwork.IsMasterClient)
            {
                GenerateFoodBeforeBegin();
                //GenerateRobotBeforeBegin();
            }

            //	##### added by Yue Chen #####
            moveWay = PlayerPrefs.GetInt("moveWayID",
                1); // It determines how to control the movement of snake, gained from initial interface
            // It determines the skin of the snake, gained from initial interface
            skinID = PlayerPrefs.GetInt("skinID", 1);
            nickName = PlayerPrefs.GetString("nickname", "");
        }

        private void Update()
        {
            if (_photonView.IsMine == false)
            {
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
            if (_photonView.IsMine == false)
            {
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

        private void OnCollisionEnter(Collision obj)
        {
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
                        var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                        findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
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
                        var findGameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                        findGameObjectWithTag.GetComponent<Camera>().orthographicSize +=
                            findGameObjectWithTag.GetComponent<Camera>().orthographicSize * cameraGrowRate;
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
                    var newFood = Instantiate(foodGenerateTarget[UnityEngine.Random.Range(0, foodGenerateTarget.Length)],
                        lastBodyPart.position, Quaternion.identity);
                    newFood.transform.parent = GameObject.Find("Foods").transform;
                    Destroy(lastBodyPart.gameObject);
                }

                var head = GameObject.FindGameObjectWithTag("Player");
                Destroy(head);
                //	##### added by Yue Chen #####
                // if (PlayerPrefs.GetInt("removeAds", 0) == 0) ShowAd();
                // SceneManager.LoadScene("Menu");
            }
            else if (obj.transform.CompareTag("Body") || obj.transform.CompareTag("Robot"))
            {
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
                        }
                        //Last token spawned, token handling
                        if (lastIndex == middleBodyPartNum)
                        {
                            TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                            tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokensCollected / 3));
                        }

                        Destroy(lastBodyPart.gameObject);
                        continue;
                    }
                    //Generating food particle
                    var newFood =
                        Instantiate(foodGenerateTarget[UnityEngine.Random.Range(0, foodGenerateTarget.Length)], lastBodyPart.position,
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

        #endregion

        #region Snake Core Features

        private IEnumerator ReturnToMainMenu()
        {
            print("Returning to main menu");

            yield return new WaitForSeconds(3.5f);

            SceneManager.LoadScene("Menu", LoadSceneMode.Single);

            yield break;
        }

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

        /// <summary>
        /// A function that will spawn body parts and will also assign the body parts
        /// to the snake's head
        /// </summary>
        private void SpawnBodyParts()
        {
            //Spawning body parts
            GameObject spawnedBodyParts = Instantiate(snakeBodyParts, transform.position, Quaternion.identity);
            spawnedBodyParts.name = "SnakeBodies";

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
                var camera = GameObject.FindGameObjectWithTag("MainCamera");
                camera.GetComponent<Camera>().orthographicSize -=
                    camera.GetComponent<Camera>().orthographicSize * cameraGrowRate;
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
                bodySmoothTime = bodyPartSmoothTime;
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

        private void GenerateFoodAndItem()
        {
            StartCoroutine("RunGenerateFoodAndItem", foodGenerateEveryXSecond);
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

        /* Make the snake head move forward all the time*/
        private void SnakeMove()
        {
            transform.position += transform.forward * snakeWalkSpeed * Time.deltaTime;
        }

        /* Make the camera follow the snake when it moves*/
        private void CameraFollowSnake()
        {
            var camera = GameObject.FindGameObjectWithTag("MainCamera").gameObject.transform;
            var velocity = Vector3.zero;
            // Reach from current position to target position smoothly
            camera.position = Vector3.SmoothDamp(camera.position,
                new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10)
                , ref velocity, cameraSmoothTime);
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
                    snakeWalkSpeed = 3.5f;
                }
            }
            else
            {
                isRunning = false;
                snakeWalkSpeed = 3.5f;
            }

            if (isRunning) StartCoroutine("LosingBodyParts");
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
            var ray = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>()
                .ScreenPointToRay(Input.mousePosition);
            RaycastHit hit; // Store the first obj touched by ray
            Physics.Raycast(ray, out hit, 50.0f); // The third parameter is the max distance
            mousePosition = new Vector3(hit.point.x, hit.point.y, 0);
            direction = Vector3.Slerp(direction, mousePosition - transform.position, Time.deltaTime * 50.0f);
            direction.z = 0;
            pointInWorld = direction.normalized * radius + transform.position;
            transform.LookAt(pointInWorld);
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

        private void GreenSnake()
        {
            //For loop to load the correct skins on the map
            for (var i = 0; i < bodyParts.Count; i++)
            {
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sprite = snakeBodyTexture;
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sortingOrder = i;
            }
        }

        private void SeaBlueSnake()
        {
            //For loop to load the correct skins on the map
            for (var i = 0; i < bodyParts.Count; i++)
            {
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sprite = snakeBodyTexture;
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sortingOrder = i;
            }
        }

        private void FireOrangeSnake()
        {
            //For loop to load the correct skins on the map
            for (var i = 0; i < bodyParts.Count; i++)
            {
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sprite = snakeBodyTexture;
                bodyParts[i].GetComponent<SnakeBodyActions>().
                    bodySpriteRenderer.sortingOrder = i;
            }            
        }

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
}