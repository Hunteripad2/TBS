using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMap : MonoBehaviour
{
    [HideInInspector] public UnitController selectedUnit;
    [HideInInspector] private Transform unitFolder;
    [HideInInspector] private bool pathIsHighlighted;
    [HideInInspector] private Tile[,] tiles;
    [HideInInspector] private List<Unit> units;

    [Header("Map")]
    [SerializeField] private int mapSizeX = 10;
    [SerializeField] private int mapSizeZ = 10;

    [Header("Tiles")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] public Color defaultColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] public Color pathColor = new Color(0f, 1f, 0f, 0.25f);

    [Header("Impassable")]
    [SerializeField] public Color impassableColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private Vector2[] impassableTiles;

    [Header("Units")]
    [SerializeField] private GameObject unitPrefab;

    private void Start()
    {
        unitFolder = GameObject.FindGameObjectWithTag("UnitFolder").transform;

        GenerateTilesData();
        GenerateTilesVisual();
        GenerateUnitsData();

        selectedUnit = GameObject.FindGameObjectWithTag("PlayerUnit").GetComponent<UnitController>();
    }

    private void FixedUpdate()
    {
        if (selectedUnit.currentPath != null && !pathIsHighlighted)
        {
            HighlightCurrentPath();
        }
    }

    private void GenerateTilesData()
    {
        tiles = new Tile[mapSizeX, mapSizeZ];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeZ; z++)
            {
                tiles[x, z] = new Tile(this, x, z);
            }
        }

        foreach (Vector2 pos in impassableTiles)
        {
            tiles[(int)pos.x, (int)pos.y].passable = false;
        }

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeZ; z++)
            {
                if (x > 0 && tiles[x - 1, z].passable)
                {
                    tiles[x, z].neighbours.Add(tiles[x - 1, z]);
                }
                if (x < mapSizeX - 1 && tiles[x + 1, z].passable)
                {
                    tiles[x, z].neighbours.Add(tiles[x + 1, z]);
                }
                if (z > 0 && tiles[x, z - 1].passable)
                {
                    tiles[x, z].neighbours.Add(tiles[x, z - 1]);
                }
                if (z < mapSizeZ - 1 && tiles[x, z + 1].passable)
                {
                    tiles[x, z].neighbours.Add(tiles[x, z + 1]);
                }
            }
        }
    }

    private void GenerateTilesVisual()
    {
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeZ; z++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, tiles[x, z].height, z), Quaternion.Euler(90f, 0f, 0f), gameObject.transform);
                tile.GetComponent<TileComponent>().tileData = tiles[x, z];
                tiles[x, z].tileComponent = tile.GetComponent<TileComponent>();

                if (tiles[x, z].passable)
                {
                    tile.GetComponent<SpriteRenderer>().color = defaultColor;
                }
                else
                {
                    tile.GetComponent<SpriteRenderer>().color = impassableColor;
                }
            }
        }
    }

    private void GenerateUnitsData()
    {
        units = new List<Unit>();

        units.Add(new Unit(tiles[0, 0], 0));

        GameObject unit = Instantiate(unitPrefab, Vector3.zero, Quaternion.identity, unitFolder);
        unit.GetComponent<UnitController>().unitData = units[0];
    }

    public void GeneratePathTo(int posX, int posZ)
    {
        selectedUnit.currentPath = null;
        selectedUnit.currentPathId = 0;
        pathIsHighlighted = false;

        Dictionary<Tile, float> distanceTo = new Dictionary<Tile, float>();
        Dictionary<Tile, Tile> prev = new Dictionary<Tile, Tile>();

        List<Tile> uncheckedTiles = new List<Tile>();

        Tile sourceTile = tiles[selectedUnit.currentTile.posX, selectedUnit.currentTile.posZ];
        Tile targetTile = tiles[posX, posZ];

        distanceTo[sourceTile] = 0;
        prev[sourceTile] = null;

        foreach (Tile tile in tiles)
        {
            if (tile != sourceTile)
            {
                distanceTo[tile] = Mathf.Infinity;
                prev[tile] = null;
            }

            uncheckedTiles.Add(tile);
        }

        while (uncheckedTiles.Count > 0)
        {
            Tile tile = uncheckedTiles[0];

            foreach (Tile possibleTile in uncheckedTiles)
            {
                if (distanceTo[possibleTile] < distanceTo[tile])
                {
                    tile = possibleTile;
                }
            }

            if (tile == targetTile)
            {
                break;
            }

            uncheckedTiles.Remove(tile);

            foreach (Tile neighbour in tile.neighbours)
            {
                float alt = distanceTo[tile] + tile.DistanceTo(neighbour);

                if (alt < distanceTo[neighbour])
                {
                    distanceTo[neighbour] = alt;
                    prev[neighbour] = tile;
                }
            }
        }

        if (prev[targetTile] == null)
        {
            return;
        }

        List<Tile> currentPath = new List<Tile>();
        Tile currentTile = targetTile;

        while (currentTile != null)
        {
            currentPath.Add(currentTile);
            currentTile = prev[currentTile];
        }

        currentPath.Reverse();

        selectedUnit.currentPath = currentPath;

        //selectedUnit.SetNewPosition(new Vector3(posX, 0, posZ));
    }

    private void HighlightCurrentPath()
    {
        pathIsHighlighted = true;

        foreach (Tile tile in selectedUnit.currentPath)
        {
            tile.tileComponent.SetColor(pathColor);
        }
    }
}
