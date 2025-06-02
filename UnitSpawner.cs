using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public List<Unit> unitData = new List<Unit>();

    public GameObject unitPrefab;
    public Main main;

    public int playerCount = 3;
    public int enemyCount = 3;

    void Start()
    {
        unitData.Clear(); // Очистка списка юнитов перед началом
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        while (main == null)
            main = FindFirstObjectByType<Main>();

        while (main.CellData == null || main.CellData.Length < 1)
            yield return null;

        SpawnUnits(playerCount, TeamType.Player);
        SpawnUnits(enemyCount, TeamType.Enemy);
    }

    void SpawnUnits(int count, TeamType team)
    {
        int sizeX = main.CellData.GetLength(0);
        int sizeY = main.CellData.GetLength(1);
        int sizeZ = main.CellData.GetLength(2);

        int startX = team == TeamType.Player ? 0 : sizeX - 1; //

        int spawned = 0;

        for (int z = 0; z < sizeZ && spawned < count; z++)
        {
            for (int y = 0; y < sizeY && spawned < count; y++)
            {
                var cell = main.CellData[startX, y, z];
                if (cell != null && cell.IsWalkable)
                {
                    var unit = Instantiate(unitPrefab, cell.Position, Quaternion.identity);
                    //var controller = unit.GetComponent<Unit>();
                    //controller.team = team;
                    //controller.CurrentCell = cell.Position;

                    Unit units = new Unit(startX, y, z, team ,unit, false, cell, unit.GetInstanceID());
                    unitData.Add(units); // Добавляем юнит в список

                    unit.name = team + "_Unit_" + spawned;
                    spawned++;
                }
            }
        }
    }
}