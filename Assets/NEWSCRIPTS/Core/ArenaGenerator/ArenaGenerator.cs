using UnityEngine;
using UnityEngine.Tilemaps;

public class ArenaGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundMap;
    public Tilemap wallMap;

    [Header("Tiles")]
    public TileBase groundTile;
    public TileBase wallTile;

    [Header("Arena Size (tiles)")]
    public int width = 20;
    public int height = 12;

    public void GenerateArena()
    {
        groundMap.ClearAllTiles();
        wallMap.ClearAllTiles();

        for (int x = -width / 2; x < width / 2; x++)
        {
            for (int y = -height / 2; y < height / 2; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // Walls on edges
                if (x == -width / 2 || x == width / 2 - 1 ||
                    y == -height / 2 || y == height / 2 - 1)
                {
                    wallMap.SetTile(pos, wallTile);
                }
                else
                {
                    groundMap.SetTile(pos, groundTile);
                }
            }
        }
    }

    private void Start()
    {
        GenerateArena();
    }
}
