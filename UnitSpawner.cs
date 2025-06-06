using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum Side
{
    Any,
    Top,
    Bottom,
    Left,
    Right
}

public class UnitSpawner : MonoBehaviour
{
    public List<Unit> unitData = new List<Unit>();

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public Main main;

    public int playerCount = 3;
    public int enemyCount = 3;

    public Side playerSpawnSide = Side.Top; // можно задать в Inspector
    public Side enemySpawnSide = Side.Any;

    void Start()
    {
        unitData.Clear();
        StartCoroutine(SpawnWhenReady());
    }

    public void SetPlayerSpawnSide(int sideIndex)
    {
        playerSpawnSide = (Side)sideIndex;
        Debug.Log($"Сторона спавна игрока изменена: {playerSpawnSide}");
    }

    IEnumerator SpawnWhenReady()
    {
        while (main == null)
            main = FindAnyObjectByType<Main>();
        while (main.CellData == null || main.CellData.Length < 1)
            yield return null;

        SpawnUnitsOnRoads(playerCount, TeamType.Player, playerSpawnSide);
        SpawnUnitsOnRoads(enemyCount, TeamType.Enemy, enemySpawnSide);

        yield return null; yield return null;
        Debug.Log($"unitData.Count после спавна = {unitData.Count}");
        foreach (var u in unitData) Debug.Log($"Юнит: {u.UnitObject?.name}, health={u.health}, IsAlive={u.IsAlive}");

        if (InitiativeManager.Instance != null)
        {
            InitiativeManager.Instance.BuildInitiativeQueue();
            InitiativeManager.Instance.StartTurn();
        }
        else
        {
            Debug.LogError("InitiativeManager.Instance не найден на сцене!");
        }
    }

    public void SpawnUnitsOnRoads(int count, TeamType team, Side side = Side.Any)
    {
        Main main = FindFirstObjectByType<Main>();
        var roadCells = GetRoadCellsForSpawn(main, side, count);

        System.Random rnd = new System.Random();
        for (int i = 0; i < count; i++)
        {
            if (roadCells.Count == 0) break;

            int idx = rnd.Next(roadCells.Count);
            MoveCell cell = roadCells[idx];
            roadCells.RemoveAt(idx);

            // **ПОВТОРНАЯ ПРОВЕРКА**
            if (cell.OccupyingUnit != null)
            {
                i--;
                continue;
            }

            GameObject prefab = (team == TeamType.Player) ? playerPrefab : enemyPrefab;
            GameObject unitObj = Instantiate(prefab, cell.Position, Quaternion.identity);
            unitObj.name = (team == TeamType.Player ? "Player_Unit_" : "Enemy_Unit_") + i;

            // ВАЖНО: Сразу пометить клетку занятой
            cell.SetOccupied(unitObj);

            var unit = new Unit(cell.Position.x, cell.Position.y, cell.Position.z, team, unitObj, false, cell, unitObj.GetInstanceID());
            unit.isPlayerControlled = (team == TeamType.Player);
            unit.IsSelected = false;
            unitData.Add(unit);

            var controller = unitObj.GetComponent<UnitController>();
            if (controller != null)
            {
                controller.unitData = unit;
                controller.isPlayerControlled = (team == TeamType.Player);
                Debug.Log($"{controller.gameObject.name}: controller.isPlayerControlled присвоено {controller.isPlayerControlled}, team={team}");
            }
        }
    }

    private List<MoveCell> GetRoadCellsForSpawn(Main main, Side side, int requiredCount)
    {
        int w = main.width;
        int h = main.height;
        int mh = main.mapHeight;
        int borderDepth = 4;
        int maxBorderDepth = Mathf.Max(w, h) / 2;

        List<MoveCell> result = new List<MoveCell>();

        while (borderDepth <= maxBorderDepth)
        {
            result.Clear();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < mh; y++)
                {
                    for (int z = 0; z < h; z++)
                    {
                        var cell = main.CellData[x, y, z];
                        if (cell == null) continue;

                        var allowedTypes = new[] { Main.CellType.Gray, Main.CellType.Brown, Main.CellType.StoneRoad };
                        if (!allowedTypes.Contains(cell.undertype) || cell.OccupyingUnit != null)
                            continue;

                        bool fits = false;
                        switch (side)
                        {
                            case Side.Top: fits = z >= h - borderDepth; break;
                            case Side.Bottom: fits = z < borderDepth; break;
                            case Side.Left: fits = x < borderDepth; break;
                            case Side.Right: fits = x >= w - borderDepth; break;
                            case Side.Any: fits = true; break;
                        }
                        if (fits)
                            result.Add(cell);
                    }
                }
            }

            if (result.Count >= requiredCount)
                break; // Хватило мест для спавна

            borderDepth += 2; // Расширяем зону и пробуем снова
        }

        // Если после расширения клеток всё равно мало — вернём все, что нашли (лучше, чем ничего)
        return result;
    }

    private bool CellHasNeighbor(MoveCell cell, MoveCell[,,] allCells)
    {
        var pos = cell.Position;

        int[][] offsets = new int[][]
        {
        new int[] {1,0,0},
        new int[] {-1,0,0},
        new int[] {0,1,0},
        new int[] {0,-1,0},
        new int[] {0,0,1},
        new int[] {0,0,-1}
        };

        foreach (var o in offsets)
        {
            int nx = pos.x + o[0], ny = pos.y + o[1], nz = pos.z + o[2];
            if (nx >= 0 && ny >= 0 && nz >= 0 && nx < allCells.GetLength(0) && ny < allCells.GetLength(1) && nz < allCells.GetLength(2))
                if (allCells[nx, ny, nz] != null && allCells[nx, ny, nz].IsWalkable)
                    return true;
        }
        return false;
    }
}