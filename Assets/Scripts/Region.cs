using System.Collections.Generic;
using UnityEngine;

public class Region
{
    public readonly List<Vector2Int> edgeTiles;
        
    private readonly List<Region> _connectedRegions;
        
    public Region() {}

    public Region(List<Vector2Int> tiles, int[,] map)
    {
        _connectedRegions = new List<Region>();
        edgeTiles = new List<Vector2Int>();
            
        foreach (Vector2Int tile in tiles)
        {
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (x != tile.x && y != tile.y || map[x, y] != 1)
                    {
                        continue;
                    }
                        
                    if (!edgeTiles.Contains(tile))
                    {
                        edgeTiles.Add(tile);
                    }
                }
            }
        }
    }
    
    public Region(List<Vector2Int> region, Room room)
    {
        _connectedRegions = new List<Region>();
        edgeTiles = new List<Vector2Int>();
            
        foreach (Vector2Int position in region)
        {
            for (int x = position.x - 1; x <= position.x + 1; x++)
            {
                for (int y = position.y - 1; y <= position.y + 1; y++)
                {
                    if (!room.IsInside(x, y))
                    {
                        continue;
                    }
                    
                    if (x != position.x && y != position.y || room.Tiles[x, y].type != Tile.Type.Empty || edgeTiles.Contains(position))
                    {
                        continue;
                    }
                        
                    edgeTiles.Add(position);
                }
            }
        }
    }

    public static void ConnectRegions(Region regionA, Region regionB)
    {
        regionA._connectedRegions.Add(regionB);
        regionB._connectedRegions.Add(regionA);
    }

    public bool IsConnected(Region region)
    {
        return _connectedRegions.Contains(region);
    }
}
