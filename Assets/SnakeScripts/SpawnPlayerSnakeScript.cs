using System;
using UnityEngine;
using UnityEngine.UI;

namespace SnakeScripts
{
    /// <summary>
    /// A script that will spawn a snake head based on the number passed in the editor
    /// </summary>
    public class SpawnPlayerSnakeScript : Singleton<SpawnPlayerSnakeScript>
    {
        [Header("Scene variable references")]
        //Reference to count and tokens text
        [SerializeField] private Text countText;
        [SerializeField] private Text tokensText;
        
        [Header("Snake Spawn Variables")]

        //Snake heads list
        [SerializeField] private GameObject[] snakeHeadList;

        //Spawn position
        private Vector3 _snakeSpawnPosition;
        public GameObject levelFail;
        public MiniMapController miniMap;
        private void Start()
        {
            _snakeSpawnPosition = new Vector3(-1.37f, 4.6f, 0);
            SpawnSnakeInScene();
            miniMap.target = GameObject.FindGameObjectWithTag("Player").transform;
        }
      
        /// <summary>
        /// This function spawns a player snake based on the skinID
        /// </summary>
        private void SpawnSnakeInScene()
        {
            int skinID = PlayerPrefs.GetInt("skinID", 1);

            GameObject playerSnake;
            
            //Switching according to skin id
            switch (skinID)
            {
                case 1:
                    playerSnake = 
                        Instantiate(snakeHeadList[0], _snakeSpawnPosition, Quaternion.identity);
                    playerSnake.GetComponent<SnakeMovement>().countText = countText;
                    playerSnake.GetComponent<SnakeMovement>().tokenText = tokensText;
                    playerSnake.GetComponent<SnakeMovement>().tokenText = tokensText;
                    break;
                case 2:
                    playerSnake = 
                        Instantiate(snakeHeadList[1], _snakeSpawnPosition, Quaternion.identity);
                    playerSnake.GetComponent<SnakeMovement>().countText = countText;
                    playerSnake.GetComponent<SnakeMovement>().tokenText = tokensText;
                    break;
                case 3:
                    playerSnake = 
                        Instantiate(snakeHeadList[2], _snakeSpawnPosition, Quaternion.identity);
                    playerSnake.GetComponent<SnakeMovement>().countText = countText;
                    playerSnake.GetComponent<SnakeMovement>().tokenText = tokensText;
                    break;
            }
        }
    }
}