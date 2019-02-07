using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Map : MonoBehaviour
{
    private const string TilesHolderName = "Tiles";
    private const string ObstaclesHolderName = "Obstacles";

    [Header("Tile")]
    public Transform tileTransform;

    [Header("Obstacle")]
    public Transform obstacleTransform;
    public int obstacleRandomSeed;
    [Range(0, 1)]
    public float obstaclePercent;

    [Header("Map")]
    public Room[] rooms;
    public int currentRoomIndex;

    private Room _currentRoom;

    private int[,] _map;
    private List<Vector2Int> _tiles;
    private Queue<Vector2Int> _shuffledTiles;
    private Vector2Int _mainTile;
    
    private void Start()
    {
        //GenerateMap();
    }

    public void GenerateMap()
    {
        _currentRoom = rooms[currentRoomIndex];
        
        Transform tilesHolderTransform = DestroyHolderByName(TilesHolderName);
        Transform obstaclesHolderTransform = DestroyHolderByName(ObstaclesHolderName);
        
        _map = new int[_currentRoom.width, _currentRoom.height];
        _tiles = new List<Vector2Int>();

        RandomFillMap();

        for (int i = 0; i < _currentRoom.smoothTimes; i++)
        {
            SmoothMap();
        }
        
        ProcessMap();
        
        for (int x = 0; x < _currentRoom.width; x++)
        {
            for (int y = 0; y < _currentRoom.height; y++)
            {
                if (_map[x, y] == 1)
                {
                    /*if (GetNeighbourTileCount(x, y) < 7)
                    {
                        InstantiateTransform(obstacleTransform, x, y, Vector3.up * .5f, Quaternion.identity, obstaclesHolderTransform);
                    }*/
                    
                    continue;
                }
                
                _tiles.Add(new Vector2Int(x, y));
                
                InstantiateTransform(tileTransform, x, y, Vector3.zero, Quaternion.Euler(Vector3.right * 90), tilesHolderTransform);
            }
        }
        
        if (_tiles.Count == 0)
        {
            return;
        }

        _mainTile = _tiles[0];
        _shuffledTiles = ShuffleArray(_tiles.ToArray(), obstacleRandomSeed);

        int mapObstaclePercent = (int) (_tiles.Count * obstaclePercent);
        int obstacleCount = mapObstaclePercent > _tiles.Count ? _tiles.Count : mapObstaclePercent;
        bool[,] obstacleMap = new bool[_currentRoom.width, _currentRoom.height];
        int currentObstacleCount = 0;

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector2Int randomVector2 = GetRandomVector2Int();
            obstacleMap[randomVector2.x, randomVector2.y] = true;
            currentObstacleCount++;

            if (randomVector2 != _mainTile && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
            {
                InstantiateTransform(obstacleTransform, randomVector2.x, randomVector2.y, Vector3.up * .5f, Quaternion.identity, obstaclesHolderTransform);
            }
            else
            {
                obstacleMap[randomVector2.x, randomVector2.y] = false;
                currentObstacleCount--;
            }
            
        }
    }

    private bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
    {
        int obstacleMapLengthX = obstacleMap.GetLength(0);
        int obstacleMapLengthY = obstacleMap.GetLength(1);
        bool[,] mapFlags = new bool[obstacleMapLengthX, obstacleMapLengthY];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        int accessibleTileCount = 1;
        queue.Enqueue(_mainTile);
        mapFlags[_mainTile.x, _mainTile.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.y + y;

                    if (x != 0 && y != 0)
                    {
                        continue;
                    }

                    if (neighbourX < 0 || neighbourX >= obstacleMapLengthX || neighbourY < 0 || neighbourY >= obstacleMapLengthY)
                    {
                        continue;
                    }
                    
                    if (_map[neighbourX, neighbourY] != 0 || mapFlags[neighbourX, neighbourY] || obstacleMap[neighbourX, neighbourY])
                    {
                        continue;
                    }
                    
                    mapFlags[neighbourX, neighbourY] = true;
                    queue.Enqueue(new Vector2Int(neighbourX, neighbourY));
                    accessibleTileCount++;
                }
            }
        }

        return _tiles.Count - currentObstacleCount == accessibleTileCount;
    }

    private void InstantiateTransform(Transform transform, int x, int y, Vector3 outlinePosition, Quaternion rotation, Transform parent)
    {
        Vector3 position = Vector2IntToVector3(x, y);
        Transform newTransform = Instantiate(transform, position + outlinePosition, rotation);
        newTransform.localScale = Vector3.one * (1 - _currentRoom.outlinePercent) * _currentRoom.tileSize;
        newTransform.parent = parent;
    }

    private Vector3 Vector2IntToVector3(int x, int y)
    {
        return new Vector3(-_currentRoom.width / 2.0f + .5f + x, 0, -_currentRoom.height / 2.0f + .5f + y) * _currentRoom.tileSize;
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

    private void RandomFillMap()
    {
        Random random = new Random(_currentRoom.mapRandomSeed.GetHashCode());

        for (int x = 0; x < _currentRoom.width; x++)
        {
            for (int y = 0; y < _currentRoom.height; y++)
            {
                _map[x, y] = x == 0 || x == _currentRoom.width - 1 || y == 0 || y == _currentRoom.height - 1 ? 1 : random.Next(0, 100) < _currentRoom.emptyPercent ? 1 : 0;
            }
        }
    }

    private void SmoothMap()
    {
        for (int x = 0; x < _currentRoom.width; x++)
        {
            for (int y = 0; y < _currentRoom.height; y++)
            {
                _map[x, y] = GetNeighbourTileCount(x, y) > 4 ? 1 : 0;
            }
        }
    }

    private int GetNeighbourTileCount(int x, int y)
    {
        int neighbourCount = 0;
        
        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                if (IsInsideMap(neighbourX, neighbourY))
                {
                    if (neighbourX != x || neighbourY != y)
                    {
                        neighbourCount += _map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    neighbourCount++;
                }
            }
        }

        return neighbourCount;
    }

    private Queue<Vector2Int> ShuffleArray(Vector2Int[] array, int seed)
    {
        Random random = new Random(seed);

        for (int i = 0; i < array.Length - 1; i++)
        {
            int index = random.Next(i, array.Length);
            Vector2Int item = array[index];
            array[index] = array[i];
            array[i] = item;
        }

        return new Queue<Vector2Int>(array);
    }

    private Vector2Int GetRandomVector2Int()
    {
        Vector2Int randomVector2Int = _shuffledTiles.Dequeue();
        _shuffledTiles.Enqueue(randomVector2Int);

        return randomVector2Int;
    }

    private List<Vector2Int> GetRegion(int x, int y)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        int[,] mapFlags = new int[_currentRoom.width, _currentRoom.height];
        int tileType = _map[x, y];
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(x, y));
        mapFlags[x, y] = 1;

        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();
            tiles.Add(tile);

            for (int i = tile.x - 1; i <= tile.x + 1; i++)
            {
                for (int j = tile.y - 1; j <= tile.y + 1; j++)
                {
                    if (!IsInsideMap(i, j) || i != tile.x && j != tile.y)
                    {
                        continue;
                    }
                    
                    if (mapFlags[i, j] != 0 || _map[i, j] != tileType)
                    {
                        continue;
                    }
                    
                    mapFlags[i, j] = 1;
                    queue.Enqueue(new Vector2Int(i, j));
                }
            }
        }

        return tiles;
    }

    private bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < _currentRoom.width && y >= 0 && y < _currentRoom.height;
    }

    private List<List<Vector2Int>> GetRegions(int tileType)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        int[,] mapFlags = new int[_currentRoom.width, _currentRoom.height];

        for (int x = 0; x < _currentRoom.width; x++)
        {
            for (int y = 0; y < _currentRoom.height; y++)
            {
                if (mapFlags[x, y] != 0 || _map[x, y] != tileType)
                {
                    continue;
                }
                
                List<Vector2Int> region = GetRegion(x, y);
                regions.Add(region);

                foreach (Vector2Int tile in region)
                {
                    mapFlags[tile.x, tile.y] = 1;
                }
            }
        }

        return regions;
    }

    private void ProcessMap()
    {
        List<List<Vector2Int>> regions = GetRegions(0);
        int thresholdSize = 50;
        List<Region> survivingRooms = new List<Region>();

        foreach (List<Vector2Int> region in regions)
        {
            if (region.Count < thresholdSize)
            {
                foreach (Vector2Int tile in region)
                {
                    _map[tile.x, tile.y] = 1;
                }
            }
            else
            {
                survivingRooms.Add(new Region(region, _map));
            }
        }
        
        ConnectClosedRooms(survivingRooms);
    }

    private void ConnectClosedRooms(List<Region> rooms)
    {
        
        Vector2Int bestTileA = new Vector2Int();
        Vector2Int bestTileB = new Vector2Int();
        Region bestRegionA = new Region();
        Region bestRegionB = new Region();
        
        foreach (Region roomA in rooms)
        {
            bool possibleConnectionFound = false;
            float bestDistance = .0f;
            
            foreach (Region roomB in rooms)
            {
                if (roomA == roomB)
                {
                    continue;
                }

                if (roomA.IsConnected(roomB))
                {
                    possibleConnectionFound = false;
                    
                    continue;
                }

                foreach (Vector2Int tileA in roomA.edgeTiles)
                {
                    foreach (Vector2Int tileB in roomB.edgeTiles)
                    {
                        float distanceBetweenRooms = Vector2Int.Distance(tileA, tileB);
                       
                        if (distanceBetweenRooms >= bestDistance && possibleConnectionFound)
                        {
                            continue;
                        }
                        
                        bestDistance = distanceBetweenRooms;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRegionA = roomA;
                        bestRegionB = roomB;
                        possibleConnectionFound = true;
                    }
                }
            }
            
            if (possibleConnectionFound)
            {
                CreateConnection(bestRegionA, bestRegionB, bestTileA, bestTileB);
            }
        }
    }

    private void CreateConnection(Region regionA, Region regionB, Vector2Int tileA, Vector2Int tileB)
    {
        Region.ConnectRegions(regionA, regionB);

        List<Vector2Int> line = GetLine(tileA, tileB);

        foreach (Vector2Int point in line)
        {
            DrawCircle(point, 2);
        }
    }

    private void DrawCircle(Vector2Int point, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radius * radius)
                {
                    continue;
                }
                
                int realX = point.x + x;
                int realY = point.y + y;

                if (IsInsideMap(realX, realY))
                {
                    _map[realX, realY] = 0;
                }
            }
        }
    }

    private List<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> line = new List<Vector2Int>();

        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);

            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);
        }

        int gradientAccumulation = longest / 2;

        for (int i = 0; i < longest; i++)
        {
            line.Add(new Vector2Int(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;

            if (gradientAccumulation < longest)
            {
                continue;
            }
            
            if (inverted)
            {
                x += gradientStep;
            }
            else
            {
                y += gradientStep;
            }
                
            gradientAccumulation -= longest;
        }

        return line;
    }
}
