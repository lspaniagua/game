using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

[Serializable]
public struct Room
{
    public int width;
    public int height;
    [Range(0, 1)]
    public float outlinePercent;
    [Range(0, 1)]
    public float emptyPercent;
    [Range(0, 5)]
    public int smoothTimes;
    public int mapRandomSeed;
    public float tileSize;
    
    [Header("Tile")]
    public Transform tileTransform;

    [Header("Obstacle")]
    public Transform obstacleTransform;
    public int obstacleRandomSeed;
    [Range(0, 1)]
    public float obstaclePercent;

    private Tile[,] _tiles;
    private Tile _mainTile;
    
    public Tile[,] Tiles
    {
        get { return _tiles; }
        set { _tiles = value; }
    }
    
    public Tile MainTile
    {
        get { return _mainTile; }
        set { _mainTile = value; }
    }

    public void InitTiles()
    {
        _tiles = new Tile[width, height];
        
        Random random = new Random(mapRandomSeed.GetHashCode());
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile.Type randomTileType = random.NextDouble() < emptyPercent
                    ? Tile.Type.Empty : Tile.Type.Ground;
                
                _tiles[x, y] = new Tile(randomTileType, x, y);
            }
        }
    }
    
    public void Smooth()
    {
        for (int i = 0; i < smoothTimes; i++)
        {
            foreach (Tile tile in _tiles)
            {
                tile.type = GetNeighbourTileCount(tile.position.x, tile.position.y) > 4
                    ? Tile.Type.Empty : Tile.Type.Ground;
            }
        }
    }

    public void InitMainTile()
    {
        foreach (Tile tile in _tiles)
        {
            if (_mainTile == null && tile.type == Tile.Type.Ground)
            {
                _mainTile = tile;

                return;
            }
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    public int GetNeighbourTileCount(int x, int y)
    {
        int neighbourCount = 0;
        
        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                if (IsInside(neighbourX, neighbourY))
                {
                    if (neighbourX == x && neighbourY == y)
                    {
                        continue;
                    }
                    
                    neighbourCount += _tiles[neighbourX, neighbourY].type == Tile.Type.Empty ? 1 : 0;
                }
                else
                {
                    neighbourCount++;
                }
            }
        }

        return neighbourCount;
    }
    
    public void ConnectRegions()
    {
        List<List<Vector2Int>> regions = GetRegions(Tile.Type.Ground);
        int thresholdSize = 10;
        List<Region> survivingRooms = new List<Region>();

        foreach (List<Vector2Int> region in regions)
        {
            if (region.Count < thresholdSize)
            {
                foreach (Vector2Int position in region)
                {
                    _tiles[position.x, position.y].type = Tile.Type.Empty;
                }
            }
            else
            {
                survivingRooms.Add(new Region(region, this));
            }
        }
        
        ConnectClosedRooms(survivingRooms);
    }
    
    private List<List<Vector2Int>> GetRegions(Tile.Type tileType)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        int[,] mapFlags = new int[width, height];

        foreach (Tile tile in _tiles)
        {
            if (mapFlags[tile.position.x, tile.position.y] != 0 || tile.type != tileType)
            {
                continue;
            }
                
            List<Vector2Int> region = GetRegion(tile);
            regions.Add(region);

            foreach (Vector2Int position in region)
            {
                mapFlags[position.x, position.y] = 1;
            }
        }

        return regions;
    }
    
    private List<Vector2Int> GetRegion(Tile tile)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        int[,] mapFlags = new int[width, height];
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(tile.position.x, tile.position.y));
        mapFlags[tile.position.x, tile.position.y] = 1;

        while (queue.Count > 0)
        {
            Vector2Int position = queue.Dequeue();
            region.Add(position);

            for (int i = position.x - 1; i <= position.x + 1; i++)
            {
                for (int j = position.y - 1; j <= position.y + 1; j++)
                {
                    if (!IsInside(i, j) || i != position.x && j != position.y)
                    {
                        continue;
                    }

                    if (mapFlags[i, j] != 0 || _tiles[i, j].type != tile.type)
                    {
                        continue;
                    }
                    
                    mapFlags[i, j] = 1;
                    queue.Enqueue(new Vector2Int(i, j));
                }
            }
        }

        return region;
    }
    
    private void ConnectClosedRooms(List<Region> regions)
    {
        Vector2Int bestTileA = new Vector2Int();
        Vector2Int bestTileB = new Vector2Int();
        Region bestRegionA = new Region();
        Region bestRegionB = new Region();
        
        foreach (Region regionA in regions)
        {
            bool possibleConnectionFound = false;
            float bestDistance = .0f;
            
            foreach (Region regionB in regions)
            {
                if (regionA == regionB)
                {
                    continue;
                }

                if (regionA.IsConnected(regionB))
                {
                    possibleConnectionFound = false;
                    
                    continue;
                }

                foreach (Vector2Int tileA in regionA.edgeTiles)
                {
                    foreach (Vector2Int tileB in regionB.edgeTiles)
                    {
                        float distanceBetweenRooms = Vector2Int.Distance(tileA, tileB);
                       
                        if (distanceBetweenRooms >= bestDistance && possibleConnectionFound)
                        {
                            continue;
                        }
                        
                        bestDistance = distanceBetweenRooms;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRegionA = regionA;
                        bestRegionB = regionB;
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
    
    private static List<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
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

                if (!IsInside(realX, realY))
                {
                    continue;
                }

                _tiles[realX, realY].type = Tile.Type.Ground;
            }
        }
    }

    public Tile[] GetTilesArrayByType(Tile.Type type)
    {
        List<Tile> tilesList = new List<Tile>();
        
        foreach (var tile in _tiles)
        {
            if (tile.type != type)
            {
                continue;
            }
            
            tilesList.Add(new Tile(tile.type, tile.position.x, tile.position.y));
        }

        return tilesList.ToArray();
    }
    
    public int GetAccessibleTilesCount()
    {
        int accessibleTilesCount = 1;
        bool[,] mapFlags = new bool[width, height];
        Queue<Vector2Int> positionsQueue = new Queue<Vector2Int>();
        positionsQueue.Enqueue(_mainTile.position);
        mapFlags[_mainTile.position.x, _mainTile.position.y] = true;

        while (positionsQueue.Count > 0)
        {
            Vector2Int position = positionsQueue.Dequeue();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = position.x + x;
                    int neighbourY = position.y + y;
                    
                    if (x != 0 && y != 0 || !IsInside(neighbourX, neighbourY))
                    {
                        continue;
                    }
                    
                    Tile neighbourTile = _tiles[neighbourX, neighbourY];
                    
                    if (neighbourTile.type != Tile.Type.Ground || mapFlags[neighbourX, neighbourY])
                    {
                        continue;
                    }

                    if (neighbourTile.obstacle == Tile.Obstacle.Solid)
                    {
                        continue;
                    }

                    mapFlags[neighbourX, neighbourY] = true;
                    positionsQueue.Enqueue(new Vector2Int(neighbourX, neighbourY));
                    accessibleTilesCount++;
                }
            }
        }

        return accessibleTilesCount;
    }
}
