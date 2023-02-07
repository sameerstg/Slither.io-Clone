using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using SnakeScripts;
using Random = UnityEngine.Random;

public class RobotAction : MonoBehaviour {

    public List<Transform> robotBody = new List<Transform>();   // Records the location information of body parts of the snake

    public Transform addRobotBody;   // Called in OnCollisionEnter(), it is the thing added behind the snake after eating fo

    public int SkinId;
    //private bool isRunning; // Called in SnakeRun()
    private float robotWalkSpeed = 3.5f; // Called in SnakeMove()
    public Vector3 curSize = Vector3.one;  //Called in SizeUp()
    private float bodyPartSmoothTime = 0.2f; //Called in OnCollisionEnter(), the same value as in SnakeBodyActions.cs
    private float currentRotation; //Called in RobotRandomMove()
    private float rotationSensitivity = 50.0f; //Called in RobotRandomMove()
    private int tokenValue;
    private Vector3 startingPosition;

    public GameObject[] foodGenerateTarget;     // Store the objects of food points
  

    // Use this for initialization
    void Start ()
    {
        startingPosition = transform.position;
        
        tokenValue = 50;
        InitiateBodies();
    }
	
	// Update is called once per frame
	void Update () {
       ColorRobot(SkinId);
    }

    void FixedUpdate()
    {
        RobotMove();
        SetBodySizeAndSmoothTime();
        RobotRandomMove();
    }

    // Initinate two body parts for each robot
    void InitiateBodies()
    {
        Vector3 currentPos;
        if (robotBody.Count == 0)
        {
            currentPos = transform.position;
        }
        else
        {
            currentPos = robotBody[robotBody.Count - 1].position;
        }
        Transform newPart1 = Instantiate(addRobotBody, currentPos, Quaternion.identity) as Transform;
        newPart1.parent = GameObject.Find("RobotBodies").transform;
        newPart1.name = this.name + "body";
        robotBody.Add(newPart1);
        Transform newPart2 = Instantiate(addRobotBody, currentPos, Quaternion.identity) as Transform;
        newPart2.parent = GameObject.Find("RobotBodies").transform;
        newPart2.name = this.name + "body";
        robotBody.Add(newPart2);
    }

    /* When the head encounters an object, figure out what to do*/
    void OnCollisionEnter(Collision obj)
    {
        if (obj.transform.CompareTag("Food"))
        {
            Destroy(obj.gameObject);
            // The contents in 'if' shouldn't be executed in logic as we always have several body parts
            Vector3 currentPos;
            if (robotBody.Count == 0)
            {
                currentPos = transform.position;
            }
            else
            {
                currentPos = robotBody[robotBody.Count - 1].position;
                Transform newPart = Instantiate(addRobotBody, currentPos, Quaternion.identity) as Transform;
                newPart.parent = GameObject.Find("RobotBodies").transform;
                newPart.name = this.name + "body";
                robotBody.Add(newPart);
            }
        }
        else if (obj.transform.CompareTag("Token"))
        {
            print("A token was collected");
            Destroy(obj.gameObject);
            Vector3 currentPos;
            if (robotBody.Count == 0)
            {
                currentPos = transform.position;
            }
            else
            {
                currentPos = robotBody[robotBody.Count - 1].position;
                Transform newPart = Instantiate(addRobotBody, currentPos, Quaternion.identity) as Transform;
                newPart.parent = GameObject.Find("RobotBodies").transform;
                newPart.name = this.name + "body";
                robotBody.Add(newPart);
            }
        }
        else if (obj.transform.tag == "Boundary")
        {
            while (robotBody.Count > 0)
            {
                int lastIndex = robotBody.Count - 1;
                Transform lastBodyPart = robotBody[lastIndex].transform;
                robotBody.RemoveAt(lastIndex);
                Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                    lastBodyPart.position, Quaternion.identity);
                Destroy(lastBodyPart.gameObject);
            }
            GameObject head = GameObject.Find(this.name);
            Destroy(head);
        }
        else if ((obj.transform.tag == "Snake") 
                 || (obj.transform.tag == "Player") 
                 || (obj.transform.tag == "Robot"))
        {
            int middleBodyPartNum = 0;
            int endBodyPartNum = 0;
            
            //Checking if number is even
            if (robotBody.Count % 2 == 0)
            {
                print("Snake that died had even number of parts" +
                      " and body parts were " + robotBody.Count);
                
                //Setting body part
                middleBodyPartNum = Mathf.RoundToInt(robotBody.Count / 2);
                endBodyPartNum = robotBody.Count - 1;
            }
            else
            {
                print("Snake that died had an odd number of parts" +
                      " and body parts were " + robotBody.Count);
                //Setting body part
                middleBodyPartNum = Mathf.RoundToInt(robotBody.Count / 2);
                endBodyPartNum = robotBody.Count - 1;
            }
            
            while (robotBody.Count > 0)
            {
                int lastIndex = robotBody.Count - 1;
                Transform lastBodyPart = robotBody[lastIndex].transform;
                robotBody.RemoveAt(lastIndex);
                //Generating token
                if (lastIndex == middleBodyPartNum || lastIndex == endBodyPartNum)
                {
                    //Generating token here
                    var tokenSpawned = Instantiate(foodGenerateTarget[3],
                        lastBodyPart.position, Quaternion.identity);

                    //Middle token spawned, token handling
                    if (lastIndex == middleBodyPartNum)
                    {
                        TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                        tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokenValue / 3));
                    }
                    //Last token spawned, token handling
                    if (lastIndex == middleBodyPartNum)
                    {
                        TokenScript tokenScript = tokenSpawned.GetComponent<TokenScript>();
                        tokenScript.SetTokenValueByAdding(Mathf.RoundToInt(tokenValue / 3));
                    }
                    
                    Destroy(lastBodyPart.gameObject);
                    continue;
                }
                //Generating food particle
                Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                    lastBodyPart.position, Quaternion.identity);
                Destroy(lastBodyPart.gameObject);
            }
            
            GameObject head = GameObject.Find(this.name);
            
            //Generating token here
            var spawnedToken = Instantiate(foodGenerateTarget[3],
                head.transform.position, Quaternion.identity);
            TokenScript tokenScriptRef = spawnedToken.GetComponent<TokenScript>();
            tokenScriptRef.SetTokenValueByAdding(Mathf.RoundToInt(tokenValue / 3));
            
            Destroy(head);
        }
        else if ((obj.transform.tag == "Body"))
        {
            bool isMyself = false;
            Transform myself = obj.gameObject.transform;
            foreach (Transform part in robotBody)
            {
                if (part.Equals(myself))
                    isMyself = true;
            }
            if (isMyself == false)
            {
                while (robotBody.Count > 0)
                {
                    int lastIndex = robotBody.Count - 1;
                    Transform lastBodyPart = robotBody[lastIndex].transform;
                    robotBody.RemoveAt(lastIndex);
                    Instantiate(foodGenerateTarget[Random.Range(0, foodGenerateTarget.Length)],
                        lastBodyPart.position, Quaternion.identity);
                    Destroy(lastBodyPart.gameObject);
                }
                GameObject head = GameObject.Find(this.name);
                Destroy(head);
            }
        }
        else if (obj.transform.CompareTag("Item"))
        {
            Destroy(obj.transform.gameObject);
        }
    }

    /* Make the snake head move forward all the time*/
    void RobotMove()
    {
        transform.position += transform.up * robotWalkSpeed * Time.deltaTime;
    }

    /* Set the size and smooth time of snake body parts every frame*/
    void SetBodySizeAndSmoothTime()
    {
        transform.localScale = curSize;
        foreach (Transform part in robotBody)
        {
            part.localScale = curSize;
            part.GetComponent<RobotBodyAction>().smoothTime = bodyPartSmoothTime;
        }
    }
   
    /* The robot snake moves randomly*/
    void RobotRandomMove()
    {
        int rotateInterval = Random.Range(3, 5);
        StartCoroutine("RunSnakeRandomRotate", rotateInterval);
    }

    // Robot rotate ramdomly while moving
    IEnumerator RunSnakeRandomRotate(float interval)
    {
        yield return new WaitForSeconds(interval);
        StopCoroutine("RunSnakeRandomRotate");

        float deltaTime = Random.Range(1, 5);
        int dir = Random.Range(0, 2);
        if (dir == 0)
        {
            currentRotation += rotationSensitivity * deltaTime;
        }
        if (dir == 1)
        {
            currentRotation -= rotationSensitivity * deltaTime;
        }

        transform.rotation = Quaternion.Euler(
            new Vector3(transform.rotation.x, transform.rotation.y, currentRotation));
    }


    [Header("Robot skin and texture related variables")]
    public Material blue, red, orange;

    public SpriteRenderer headSpriteRenderer;
    public Sprite[] headSprites;
    public Sprite[] bodySprites;

    void ColorRobot(int id)
    {
        switch (id)
        {
            case 1: GreenSnake(); break;
            case 2: SeaBlueSnake(); break;
            case 3: FireOrangeSnake(); break;
        }
    }
    void GreenSnake()
    {
        headSpriteRenderer.sprite = headSprites[0];
        
        //For loop to load the correct skins on the map
        for (var i = 0; i < robotBody.Count; i++)
        {
            robotBody[i].GetComponent<RobotBodyAction>().
                ChangeTexture(bodySprites[0], i);
        }
        
        // for (int i = 0; i < robotBody.Count; i++)
        // {
        //     if (i % 2 == 0)
        //     {
        //         robotBody[i].GetComponent<Renderer>().material = blue;
        //     }
        // }
    }
    void SeaBlueSnake()
    {
        headSpriteRenderer.sprite = headSprites[1];
        
        //For loop to load the correct skins on the map
        for (var i = 0; i < robotBody.Count; i++)
        {
            robotBody[i].GetComponent<RobotBodyAction>().
                ChangeTexture(bodySprites[1], i);
        }
        
        // for (int i = 0; i < robotBody.Count; i++)
        // {
        //     if (i % 2 == 0)
        //     {
        //         robotBody[i].GetComponent<Renderer>().material = red;
        //     }
        // }
    }
    void FireOrangeSnake()
    {
        headSpriteRenderer.sprite = headSprites[2];
        
        //For loop to load the correct skins on the map
        for (var i = 0; i < robotBody.Count; i++)
        {
            robotBody[i].GetComponent<RobotBodyAction>().
                ChangeTexture(bodySprites[2], i);
        }
        
        // for (int i = 0; i < robotBody.Count; i++)
        // {
        //     if (i % 2 == 0)
        //     {
        //         robotBody[i].GetComponent<Renderer>().material = orange;
        //     }
        // }
    }
}
