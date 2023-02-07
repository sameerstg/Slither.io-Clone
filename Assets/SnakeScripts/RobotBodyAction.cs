using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotBodyAction : MonoBehaviour {
    private int myOrder;    // The order of this part in the whole snake
    private Transform robotHead;     // The location of snake head
    private Vector3 movementVelocity;   // The velocity of current part
    [Range(0.0f, 1.0f)]
    public float smoothTime = 0.2f;    // The smooth time when a body part follows head
    public int myHeadId;

    void Start()
    {
        if (GameObject.Find(this.name))
        {
            string name = (this.name).TrimEnd("body".ToCharArray());
            robotHead = GameObject.Find(name).gameObject.transform;
            for (int j = 0; j < robotHead.GetComponent<RobotAction>().robotBody.Count; j++)
            {
                if (gameObject == robotHead.GetComponent<RobotAction>().robotBody[j].gameObject)
                {
                    myOrder = j;
                    break;
                }
            }
        }
    }

    [Header("Snake Body Texture Related Variables")]
    //Body Texture Sprite
    [SerializeField] private SpriteRenderer bodyTextureSprite;
    
    /// <summary>
    /// Changes the texture according to the value passed
    /// </summary>
    public void ChangeTexture(Sprite textureToApply, int sortOrder)
    {
        bodyTextureSprite.sprite = textureToApply;
        bodyTextureSprite.sortingOrder = sortOrder;
    }

    void FixedUpdate()
    {
        // If the body part is the first one, then it follows the head
        if (myOrder == 0)
        {
            transform.position = Vector3.SmoothDamp(transform.position, 
                robotHead.position, ref movementVelocity, smoothTime);
            // Rotates the transform so the forward vector points at target's current position
            //transform.LookAt(robotHead.transform.position);

            Vector3 rotationTarget = new Vector3(0, 0, robotHead.eulerAngles.z);
            
            // transform.rotation = Quaternion.Euler(0,0,robotHead.eulerAngles.z);

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.Euler(rotationTarget), 
                2.5f);
        }
        // If not, then it follows previous body part
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                robotHead.GetComponent<RobotAction>().robotBody[myOrder - 1].position, 
                ref movementVelocity, smoothTime / 3);
            Vector3 robotAction = robotHead.GetComponent<RobotAction>().robotBody[myOrder - 1].position;
            Transform robotTransform = robotHead.GetComponent<RobotAction>().robotBody[myOrder - 1].transform;
            //transform.LookAt(robotHead.GetComponent<RobotAction>().robotBody[myOrder - 1].position);
            
            Vector3 rotationTarget = new Vector3(0, 0, robotTransform.eulerAngles.z);
            
            transform.rotation = Quaternion.Euler(0,0,rotationTarget.z);
            
            // transform.rotation = Quaternion.RotateTowards(transform.rotation,
            //     Quaternion.Euler(rotationTarget),
            //     2.5f);
        }
    }
}
