using UnityEngine;

namespace SnakeScripts
{
    /// <summary>
    /// A script that will control the token properties
    /// </summary>
    public class TokenScript : MonoBehaviour
    {
        #region Variables for Token Value

        [Header("Variables for the token value")]
        //When true, tells the game that the local snake collided
        //with this token
        public bool localSnakeHasCollided = false;
        
        //A variable for setting the token value
        [SerializeField] private float tokenValue;
        
        //Bet value of token. This will allow us to determine whether or not
        //the current snake can actually absorb the token or not
        private int _betValueOfToken = 0;

        #endregion

        #region Set/Get Token Value

        /// <summary>
        /// A function that will get the value of the token
        /// </summary>
        public float GetTokenValue()
        {
            return tokenValue;
        }

        /// <summary>
        /// A function that sets the value of the token that the
        /// player spawns with
        /// </summary>
        public void SetTokenValueByAdding(float valueToAdd)
        {
            tokenValue += valueToAdd;
        }

        /// <summary>
        /// A function that deducts the value of the token. This is called by an RPC and only when
        /// the player collides with a token that has a bet value greater than its own.
        /// </summary>
        /// <param name="valueToDeduct"></param>
        public void DeductTokenValueBySubtracting(int valueToDeduct)
        {
            tokenValue -= valueToDeduct;
        }

        #endregion

        #region Set/Get Bet Token Value

        /// <summary>
        /// Sets the token bet value
        /// </summary>
        /// <param name="valueToSet"></param>
        public void SetTokenBetValue(int valueToSet)
        {
            _betValueOfToken += valueToSet;
        }

        /// <summary>
        /// Gets bet token value
        /// </summary>
        public int GetTokenBetValue()
        {
            return _betValueOfToken;
        }

        #endregion
    }
}