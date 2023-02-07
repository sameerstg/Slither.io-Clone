using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace SnakeScripts
{
    /// <summary>
    /// Activates the long reach ability on the player that needs it
    /// </summary>
    public class LongReachAbilityScript : MonoBehaviour
    {
        //Photon view reference
        private PhotonView _photonView;
        //Transform
        private Transform _thisTransform;
        
        //Raycast hit
        private RaycastHit _longReachRayHit;
        //private ray
        private Ray _longReachRay;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            _thisTransform = transform;
            _longReachRay = new Ray(_thisTransform.position, _thisTransform.forward);
        }

        private void FixedUpdate()
        {
            //Returning if we are not the local player
            if (_photonView.IsMine == false) return;

            //Updating ray origin
            _longReachRay = new Ray(_thisTransform.position, _thisTransform.forward);
            
            if (Physics.SphereCast(
                _longReachRay, 
                1.5f, 
                out _longReachRayHit,
                3.0f))
            {
                if (_longReachRayHit.collider.CompareTag("Food") ||
                    _longReachRayHit.collider.CompareTag("Token"))
                {
                    //Getting the boolean and checking its value
                    bool particleIsMoving 
                        = _longReachRayHit.collider.GetComponent<ItemPropertiesScript>().isBeingMoved;

                    //Returning if the particle is already moving
                    if (particleIsMoving == true)
                    {
                        return;
                    }

                    //Starting the co-routine to make the food particle moves
                    //towards this head
                    _longReachRayHit.collider.GetComponent<ItemPropertiesScript>().isBeingMoved = true;
                    StartCoroutine(MoveFoodTowardsPlayer(_longReachRayHit.collider.gameObject));
                    print("A food particle was long reached");
                }
            }
        }

        /// <summary>
        /// This routine moves the food particle towards this gameObject
        /// </summary>
        /// <param name="foodParticleToMove"></param>
        /// <returns></returns>
        private IEnumerator MoveFoodTowardsPlayer(GameObject foodParticleToMove)
        {
            //Running a while loop to make sure tha food particle keeps going towards the player
            //as long as it is active
            while (foodParticleToMove == true)
            {
                if ((foodParticleToMove.transform.position - _thisTransform.position).magnitude > 0)
                {
                    foodParticleToMove.transform.position = Vector3.MoveTowards(
                        foodParticleToMove.transform.position,
                        _thisTransform.position,
                        6.25f * Time.fixedDeltaTime
                    );
                }
                
                yield return new WaitForSeconds(0.02f);
            }
            
            yield break;
        }
    }
}