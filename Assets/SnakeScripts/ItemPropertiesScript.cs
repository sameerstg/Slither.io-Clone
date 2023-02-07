using UnityEngine;

namespace SnakeScripts
{
    /// <summary>
    /// A script that will store the item properties
    /// </summary>
    public class ItemPropertiesScript : MonoBehaviour
    {
        /// <summary>
        /// When this is set to true, it tells the game that
        /// this food item is being moved by a player
        /// </summary>
        public bool isBeingMoved;
    }
}