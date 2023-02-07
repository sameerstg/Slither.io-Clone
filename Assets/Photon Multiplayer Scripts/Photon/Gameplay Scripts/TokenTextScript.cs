using System.Collections;
using UnityEngine;

namespace Photon_Multiplayer_Scripts.Photon.Gameplay_Scripts
{
    public class TokenTextScript : MonoBehaviour
    {
        #region Popup Text Animation

        private IEnumerator PopupTextAnimation()
        {
            //We will have a 2 second popup time
            float animationTime = 3.5f;

            //While loop to run to make the text go up
            while (animationTime > 0)
            {
                transform.position += Vector3.up * 0.010f;
                
                //Deducting animation time
                animationTime -= 0.02f;
                yield return new WaitForEndOfFrame();
            }
            
            gameObject.SetActive(false);
        }

        #endregion
        
        #region Unity Functions

        private void OnEnable()
        {
            StartCoroutine(PopupTextAnimation());
        }

        #endregion
    }
}