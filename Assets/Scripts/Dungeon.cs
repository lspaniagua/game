using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Dungeon : MonoBehaviour
{
    private const string TilesHolderName = "Tiles";
    private const string ObstaclesHolderName = "Obstacles";
    
    [Header("Dungeon")]
    public Room[] rooms;
    public int currentRoomIndex;

    private Room _currentRoom;
    private Queue<Tile> _shuffledTiles;

    public void GenerateDungeon()
    {
        Transform tilesHolderTransform = DestroyHolderByName(TilesHolderName);
        Transform obstaclesHolderTransform = DestroyHolderByName(ObstaclesHolderName);
        
        _currentRoom = rooms[currentRoomIndex];
        _currentRoom.InitTiles();
        _currentRoom.Smooth();
        _currentRoom.ConnectRegions();
        _currentRoom.InitMainTile();

        foreach (Tile tile in _currentRoom.Tiles)
        {
            if (tile.type == Tile.Type.Empty)
            {
                if (_currentRoom.GetNeighbourTileCount(tile.position.x, tile.position.y) < 7)
                {
                    InstantiateTransform(_currentRoom.obstacleTransform, tile.position.x, tile.position.y, Vector3.up * .5f, Quaternion.identity, obstaclesHolderTransform);
                }
                
                continue;
            }
                
            InstantiateTransform(_currentRoom.tileTransform, tile.position.x, tile.position.y, Vector3.zero, Quaternion.Euler(Vector3.right * 90), tilesHolderTransform);
        }

        _shuffledTiles = ShuffleArray(_currentRoom.GetTilesArrayByType(Tile.Type.Ground), _currentRoom.obstacleRandomSeed);

        int obstacleCount = (int) (_currentRoom.Tiles.Length * _currentRoom.obstaclePercent);
        int currentObstacleCount = _shuffledTiles.Count;
        
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector2Int randomPosition = GetRandomVector2Int();
            Tile randomTile = _currentRoom.Tiles[randomPosition.x, randomPosition.y];
            randomTile.obstacle = Tile.Obstacle.Solid;
            currentObstacleCount--;

            if (randomPosition != _currentRoom.MainTile.position && currentObstacleCount == _currentRoom.GetAccessibleTilesCount())
            {
                InstantiateTransform(_currentRoom.obstacleTransform, randomPosition.x, randomPosition.y, Vector3.up * .5f, Quaternion.identity, obstaclesHolderTransform);
            }
            else
            {
                randomTile.obstacle = Tile.Obstacle.None;
                currentObstacleCount++;
            }
        }
    }

    private Transform DestroyHolderByName(string holderName)
    {
        Transform holderTransform = transform.Find(holderName);

        if (holderTransform)
        {
            DestroyImmediate(holderTransform.gameObject);
        }

        holderTransform = new GameObject(holderName).transform;
        holderTransform.parent = transform;

        return holderTransform;
    }
    
    private void InstantiateTransform(Transform prefabTransform, int x, int y, Vector3 outlinePosition, Quaternion rotation, Transform parent)
    {
        Vector3 position = Vector2IntToVector3(x, y);
        Transform newTransform = Instantiate(prefabTransform, position + outlinePosition, rotation);
        newTransform.localScale = Vector3.one * (1 - _currentRoom.outlinePercent) * _currentRoom.tileSize;
        newTransform.parent = parent;
    }
    
    private Vector3 Vector2IntToVector3(int x, int y)
    {
        return new Vector3(-_currentRoom.width / 2.0f + .5f + x, 0, -_currentRoom.height / 2.0f + .5f + y) * _currentRoom.tileSize;
    }
    
    private Queue<Tile> ShuffleArray(Tile[] array, int seed)
    {
        Random random = new Random(seed);

        for (int i = 0; i < array.Length - 1; i++)
        {
            int index = random.Next(i, array.Length);
            Tile item = array[index];
            array[index] = array[i];
            array[i] = item;
        }

        return new Queue<Tile>(array);
    }
    
    private Vector2Int GetRandomVector2Int()
    {
        Tile tile = _shuffledTiles.Dequeue();
        _shuffledTiles.Enqueue(tile);

        return tile.position;
    }
}
