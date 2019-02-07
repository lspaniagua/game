using UnityEngine;

public class Tile
{
    public enum Type
    {
        Empty,
        Ground
    }
    
    public enum Obstacle
    {
        None,
        Empty,
        Solid
    }

    public Type type;
    public Obstacle obstacle;
    public Vector2Int position;

    public Tile(Type type, int x, int y)
    {
        this.type = type;
        obstacle = Obstacle.None;
        position = new Vector2Int(x, y);
    }
}
