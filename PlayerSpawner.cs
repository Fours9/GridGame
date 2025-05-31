using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public void Start()
    {
        Debug.Log("PlayerSpawner waiting...");
        StartCoroutine(SpawnAfterReady());
    }

    IEnumerator SpawnAfterReady()
    {
        // Ждём появления Main
        Main map = null;
        while (map == null)
        {
            map = FindFirstObjectByType<Main>();
            yield return null;
        }

        // Ждём, пока карта будет готова
        while (map.CellData == null || map.CellData == null || map.CellData.Length <= 1)
        {
            yield return null;
        }

        Debug.Log("PlayerSpawner is active!");

        var cellData = map.CellData;
        int sizeX = cellData.GetLength(0);
        int sizeY = cellData.GetLength(1);
        int sizeZ = cellData.GetLength(2);

        int edgeX = sizeX - 1; // правый край по X
        int midY = sizeY / 2;
        int midZ = sizeZ / 2;

        MoveCell bestCell = null;
        float bestDistance = float.MaxValue;

        // Ищем ближайшую walkable клетку к середине правого края
        for (int y = 0; y < sizeY; y++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                MoveCell cell = map.CellData[edgeX, y, z];
                if (cell != null && cell.IsWalkable == true)
                {
                    float dist = Mathf.Abs(y - midY) + Mathf.Abs(z - midZ);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCell = cell;
                    }
                }
            }
        }

        if (bestCell != null)
        {
            Vector3 spawnPosition = new Vector3(bestCell.Position.x, bestCell.Position.y, bestCell.Position.z);
            GameObject player = Instantiate(map.playerPrefab, spawnPosition, Quaternion.identity);
            player.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Не найдено подходящей клетки для спавна на правом краю.");
        }
    }
}

