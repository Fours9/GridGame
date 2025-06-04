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
            main = FindAnyObjectByType<Main>();

        while (main.CellData == null || main.CellData.Length < 1)
            yield return null;

        SpawnUnits(playerCount, TeamType.Player);
        SpawnUnits(enemyCount, TeamType.Enemy);

        // ДВА КАДРА ЖДЁМ!
        yield return null;
        yield return null;
        Debug.Log($"unitData.Count после спавна = {unitData.Count}");
        foreach (var u in unitData) Debug.Log($"Юнит: {u.UnitObject?.name}, health={u.health}, IsAlive={u.IsAlive}");


        Debug.Log($"unitData.Count после спавна = {unitData.Count}");

        // Только теперь инициатива!
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

    void SpawnUnits(int count, TeamType team)
    {
        int sizeX = main.CellData.GetLength(0);
        int sizeY = main.CellData.GetLength(1);
        int sizeZ = main.CellData.GetLength(2);

        int startX = team == TeamType.Player ? 0 : sizeX - 1;
        int spawned = 0;

        for (int z = 0; z < sizeZ && spawned < count; z++)
        {
            for (int y = 0; y < sizeY && spawned < count; y++)
            {
                var cell = main.CellData[startX, y, z];
                if (cell != null && cell.IsWalkable)
                {
                    var unit = Instantiate(unitPrefab, cell.Position, Quaternion.identity);

                    // *** Сразу именуй правильно! ***
                    unit.name = (team == TeamType.Player ? "Player_Unit_" : "Enemy_Unit_") + spawned;

                    cell.SetOccupied(unit);

                    Unit units = new Unit(startX, y, z, team, unit, false, cell, unit.GetInstanceID());
                    units.isPlayerControlled = (team == TeamType.Player);
                    units.IsSelected = false;
                    unitData.Add(units);

                    Debug.Log($"Добавлен юнит: {units.UnitObject?.name}, health={units.health}, IsAlive={units.IsAlive}");

                    var controller = unit.GetComponent<UnitController>();
                    if (controller != null)
                    {
                        controller.unitData = units;
                        controller.isPlayerControlled = (team == TeamType.Player);
                        Debug.Log($"{controller.gameObject.name}: controller.isPlayerControlled присвоено {controller.isPlayerControlled}, team={team}");
                    }

                    spawned++;
                }
            }
        }

        foreach (var u in unitData)
            Debug.Log($"[SPAWN DEBUG] {u.UnitObject.name}, team={u.team}, isPlayerControlled={u.isPlayerControlled}");
    }
}