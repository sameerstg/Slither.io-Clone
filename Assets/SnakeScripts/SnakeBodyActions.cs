using System;
using UnityEngine;
using System.Collections;
using Photon.Pun;

public class SnakeBodyActions : MonoBehaviour, IPunObservable
{
    #region Variabless

    //Stores the head
    public GameObject parentHead;
    
    [SerializeField] private PhotonView photonView;
    [SerializeField] private bool isPhotonPlayer;

    private SnakeMovement m_LocalSnakeMovementScript;
    
    private int myOrder; // The order of this part in the whole snake
    private Transform head; // The location of snake head
    private Transform headForeign;
    private Transform bodyForeign;
    private Vector3 movementVelocity; // The velocity of current part
    [Range(0.0f, 1.0f)] public float smoothTime = 0.2f;

    public SpriteRenderer bodySpriteRenderer;

    //Photon Serialization variables
    private Vector3 _foreignHeadPosition = new Vector3();
    private Vector3 _foreignBodyPosition = new Vector3();
    private Vector3 _foreignPlayerRotation = new Vector3();
    private int _foreignHeadViewId = 0;
    private int _foreignBodyViewId = 0;

    #endregion

    #region Photon RPC and Serailization Functions

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!isPhotonPlayer) return;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.localScale);
            stream.SendNext(transform.rotation.eulerAngles);
            stream.SendNext(myOrder);
            stream.SendNext(_foreignHeadViewId);
            stream.SendNext(_foreignBodyViewId);
            stream.SendNext(smoothTime);
            stream.SendNext(_isCollidingWithSomething);

            if (myOrder != 0)
            {
                stream.SendNext(head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1].position);
            }
        }
        else if (stream.IsReading)
        {
            _foreignHeadPosition = (Vector3)stream.ReceiveNext();
            transform.localScale = (Vector3)stream.ReceiveNext();
            _foreignPlayerRotation = (Vector3)stream.ReceiveNext();
            myOrder = (int)stream.ReceiveNext();
            _foreignHeadViewId = (int)stream.ReceiveNext();
            _foreignBodyViewId = (int)stream.ReceiveNext();
            smoothTime = (float)stream.ReceiveNext();
            _isCollidingWithSomething = (bool)stream.ReceiveNext();

            if (myOrder != 0)
            {
                _foreignBodyPosition = (Vector3)stream.ReceiveNext();
            }

            _isReading = true;
        }
    }

    /// <summary>
    /// An RPC that will run on the foreign player to get some components
    /// </summary>
    [PunRPC]
    private void StartOnForeignPlayer()
    {
        //If statement that will prevent the code from running if we are the local player
        if(photonView.IsMine == true) return;
        
        //Finding all players
        var getPlayers = GameObject.FindGameObjectsWithTag("Player");

        //Getting the local head
        foreach (GameObject player in getPlayers)
        {
            PhotonView playerPhotonView = player.GetComponent<PhotonView>();
            if (playerPhotonView.IsMine)
            {
                print("Foreign head found");
                head = playerPhotonView.gameObject.transform;
                break;
            }
        }

        //Try catch to get all components required
        try
        {
            for (int i = 0; i < head.GetComponent<SnakeMovement>().bodyParts.Count; i++)
            {
                if (gameObject == head.GetComponent<SnakeMovement>().bodyParts[i].gameObject)
                {
                    myOrder = i;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            //print("Component Reference was missing " + e.Message);
        }
    }

    /// <summary>
    /// Finds the local head for the foreign player
    /// </summary>
    [PunRPC]
    private void FindLocalHeadOnForeign(int photonViewOfHead)
    {
        PhotonView foreignHead = PhotonNetwork.GetPhotonView(photonViewOfHead);
        GameObject headGameObject = foreignHead.gameObject;
        parentHead = headGameObject;
    }

    //When true, tells the game that this script is reading
    private bool _isReading = false;
    //When true, tells the foreign player that the position was synced
    private bool _hasSyncedPosition = false;
    //A variable that will tell the game for how long the network will sync
    private float _syncTime = 0.25f;

    /// <summary>
    /// A function that will sync the position and rotation of the foreign player
    /// </summary>
    private IEnumerator SyncForeignPlayerPosition()
    {
        if (_isReading == false) yield break;

        //Getting head
        PhotonView headView = PhotonView.Find(_foreignHeadViewId);
        
        //Syncing the position at the very start
        _syncTime -= Time.deltaTime;
        if (_syncTime > 0)
        {
            transform.position = _foreignHeadPosition;
            transform.LookAt(_foreignHeadPosition);
            yield break;
        }

        //If we still don't have the head foreign
        if (headForeign == null)
        {
            //Foreign head value setting
            headForeign = headView.transform;
        }
        
        //Head foreign position
        var headForeignPosition = headForeign.position;

        //Getting movement direction and then moving the foreign player
        transform.position = Vector3.SmoothDamp(transform.position,
            headForeignPosition, ref movementVelocity, smoothTime);
        transform.LookAt(headForeignPosition);
    }

    /// <summary>
    /// Syncs the body position with another body position
    /// </summary>
    private IEnumerator SyncForeignPlayerPosition2()
    {
        if (_isReading == false) yield break;

        //Getting head
        PhotonView bodyView = PhotonView.Find(_foreignBodyViewId);
        
        //Syncing the position at the very start
        _syncTime -= Time.deltaTime;
        if (_syncTime > 0)
        {

            if(bodyView == false)
            {
                transform.position = _foreignBodyPosition;
                transform.LookAt(_foreignBodyPosition);
                yield break;
            }

            bodyForeign = bodyView.transform;
            
            transform.position = _foreignBodyPosition;
            transform.LookAt(_foreignBodyPosition);
            yield break;
        }

        //If we still don't have the body foreign
        if (bodyForeign == null)
        {
            bodyForeign = bodyView.transform;
        }

        //Getting the body foreign position
        var bodyForeignPosition = bodyForeign.position;

        //Getting movement direction and then moving the foreign player
        transform.position = Vector3.SmoothDamp(transform.position,
            bodyForeignPosition, ref movementVelocity, smoothTime/3);
        transform.LookAt(bodyForeignPosition);
    }

    #endregion

    #region Sprite Renderer and collider functions

    [Header("Sprite Renderer Functions and Collider Variables")]
    //When true, tells the game that the collider is not colliding with anything
    private bool _isCollidingWithSomething = false;
    
    /// <summary>
    /// Enables the local collider when the snake is not colliding with anything
    /// </summary>
    public IEnumerator EnableLocalBodyCollider()
    {
        while (_isCollidingWithSomething == true)
        {
            yield return new WaitForEndOfFrame();
        }
        
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = false;
        
        MakeBodyOpaque();
    }

    /// <summary>
    /// Enables body collider on the foreign player
    /// </summary>
    public void EnableColliderOnForeignBody()
    {
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = false;
        
        MakeBodyOpaque();
    }
    
    /// <summary>
    /// Disables the local collider
    /// </summary>
    public void DisableLocalCollider()
    {
        SphereCollider localSphereCollider = GetComponent<SphereCollider>();
        localSphereCollider.isTrigger = true;
        
        MakeBodyTransparent();
    }
    
    /// <summary>
    /// Makes the body transparent for a while
    /// </summary>
    public void MakeBodyTransparent()
    {
        bodySpriteRenderer.color = new Color(
            255,
            255,
            255,
            0.35f
        );
    }

    /// <summary>
    /// Makes the body opaque
    /// </summary>
    public void MakeBodyOpaque()
    {
        bodySpriteRenderer.color = new Color(
            255,
            255,
            255,
            1.0f
        );
    }

    #endregion
    
    #region Unity Functions

    private void OnTriggerStay(Collider other)
    {
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false) return;
            
            //Colliding with gameObject
            if (!other.CompareTag("Player") || !other.CompareTag("Snake") ||
                !other.CompareTag("Body"))
            {
                _isCollidingWithSomething = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false) return;
            
            //Colliding with gameObject
            if (!other.CompareTag("Player") || !other.CompareTag("Snake") ||
                !other.CompareTag("Body"))
            {
                _isCollidingWithSomething = false;
            }
        }
    }

    void Start()
    {
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false)
            {
                return;
            }

            //Finding all players
            var getPlayers = GameObject.FindGameObjectsWithTag("Player");

            //Getting the local head
            foreach (GameObject player in getPlayers)
            {
                PhotonView playerPhotonView = player.GetComponent<PhotonView>();
                if (playerPhotonView.IsMine)
                {
                    //print("Local head found");
                    head = playerPhotonView.gameObject.transform;
                    parentHead = playerPhotonView.gameObject;

                    //Setting up boost speed
                    m_LocalSnakeMovementScript = head.GetComponent<SnakeMovement>();

                    //Trying to find a foreign player
                    photonView.RPC(
                        "FindLocalHeadOnForeign", RpcTarget.OthersBuffered, playerPhotonView.ViewID
                    );
                    break;
                }
            }

            for (int i = 0; i < head.GetComponent<SnakeMovement>().bodyParts.Count; i++)
            {
                if (gameObject == head.GetComponent<SnakeMovement>().bodyParts[i].gameObject)
                {
                    myOrder = i;
                    break;
                }
            }
            
            photonView.RPC(
                "StartOnForeignPlayer",
                RpcTarget.AllBuffered
            );

            return;
        }

        head = GameObject.FindGameObjectWithTag("Player").gameObject.transform;
        for (int i = 0; i < head.GetComponent<SnakeMovement>().bodyParts.Count; i++)
        {
            if (gameObject == head.GetComponent<SnakeMovement>().bodyParts[i].gameObject)
            {
                myOrder = i;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        if (isPhotonPlayer)
        {
            if (photonView.IsMine == false)
            {
                if (myOrder == 0)
                {
                    StartCoroutine(SyncForeignPlayerPosition());
                }
                else
                {
                    StartCoroutine(SyncForeignPlayerPosition2());
                }
                return;
            }

            // If the body part is the first one, then it follows the head
            if (myOrder == 0)
            {
                transform.position = Vector3.SmoothDamp(transform.position,
                    head.position, ref movementVelocity, smoothTime);
                // Rotates the transform so the forward vector points at target's current position
                transform.LookAt(head.transform.position);

                //Getting the photon view of the head
                PhotonView headView = head.GetComponent<PhotonView>();
                _foreignHeadViewId = headView.ViewID;
            }
            // If not, then it follows previous body part
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position,
                    head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1].position,
                    ref movementVelocity, smoothTime / 3);
                transform.LookAt(head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1].position);

                //Getting the photon view of the body
                PhotonView bodyPartView = head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1]
                    .GetComponent<PhotonView>();
                _foreignBodyViewId = bodyPartView.ViewID;
            }

            return;
        }

        // If the body part is the first one, then it follows the head
        if (myOrder == 0)
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                head.position, ref movementVelocity, smoothTime);
            // Rotates the transform so the forward vector points at target's current position
            transform.LookAt(head.transform.position);
        }
        // If not, then it follows previous body part
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1].position,
                ref movementVelocity, smoothTime / 3);
            transform.LookAt(head.GetComponent<SnakeMovement>().bodyParts[myOrder - 1].position);
        }
    }

    #endregion
}