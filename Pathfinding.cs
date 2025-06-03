using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Клас, що виконує пошук шляху методом A*
/// </summary>
public class Pathfinding : MonoBehaviour
{
    /// <summary>
    /// Знайти шлях від точки start до target серед доступних клітинок.
    /// </summary>
    public List<MoveCell> FindPath(Vector3Int startCoords, Vector3Int targetCoords, MoveCell[,,] CellData)
    {
        // Перевірка коректності координат
        if (!IsValid(startCoords, CellData) || !IsValid(targetCoords, CellData)) return null;

        MoveCell startCell = CellData[startCoords.x, startCoords.y, startCoords.z];
        MoveCell targetCell = CellData[targetCoords.x, targetCoords.y, targetCoords.z];

        if (CellData[startCoords.x, startCoords.y, startCoords.z] == null)
            Debug.LogError("Start координаты не содержат MoveCell!");
        if (CellData[targetCoords.x, targetCoords.y, targetCoords.z] == null)
            Debug.LogError("Target координаты не содержат MoveCell!");

        // Відкритий список (клітинки, які ще не перевірені)
        List<MoveCell> openSet = new List<MoveCell>();
        // Закритий список (вже перевірені клітинки)
        HashSet<MoveCell> closedSet = new HashSet<MoveCell>();

        openSet.Add(startCell);

        // Словники вартостей
        Dictionary<MoveCell, MoveCell> cameFrom = new Dictionary<MoveCell, MoveCell>();
        Dictionary<MoveCell, float> gScore = new Dictionary<MoveCell, float>();
        Dictionary<MoveCell, float> fScore = new Dictionary<MoveCell, float>();

        gScore[startCell] = 0f;
        fScore[startCell] = Heuristic(startCell, targetCell);



        while (openSet.Count > 0)
        {
            // Знаходимо клітинку з найменшим fScore
            MoveCell current = openSet[0];
            foreach (MoveCell cell in openSet)
            {
                if (fScore.ContainsKey(cell) && fScore[cell] < fScore[current])
                    current = cell;
            }

            // Якщо досягли цільової клітинки — будуємо шлях
            if (current == targetCell)
                return ReconstructPath(cameFrom, current);



            openSet.Remove(current);
            closedSet.Add(current);

            foreach (MoveCell neighbor in GetNeighbors(current, CellData))
            {
                if (closedSet.Contains(neighbor)) continue;

                float baseCost = Vector3Int.Distance(current.Position, neighbor.Position);
                float heightPenalty = Mathf.Max(0, neighbor.Position.y - current.Position.y); // штраф за подъем
                float moveCost = baseCost + heightPenalty;

                float tentativeGScore = gScore.ContainsKey(current) ? gScore[current] + moveCost : Mathf.Infinity; // обчислюємо тимчасову вартість шляху

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (tentativeGScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : Mathf.Infinity))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + Heuristic(neighbor, targetCell);
            }
        }

        // Якщо шлях не знайдено
        return null;
    }

    /// <summary>
    /// Побудова фінального шляху із cameFrom словника.
    /// </summary>
    private static List<MoveCell> ReconstructPath(Dictionary<MoveCell, MoveCell> cameFrom, MoveCell current)
    {
        List<MoveCell> totalPath = new List<MoveCell> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            if (current == null) break;
            totalPath.Insert(0, current);
        }

        return totalPath.FindAll(cell => cell != null);
    }

    /// <summary>
    /// Евристика (манхеттенська відстань) між двома клітинками.
    /// </summary>
    private static float Heuristic(MoveCell a, MoveCell b)
    {
        return Vector3Int.Distance(a.Position, b.Position);
    }

    /// <summary>
    /// Отримати сусідів, доступних для переміщення.
    /// </summary>
    /// <summary>
    /// Отримати сусідів, доступних для переміщення.
    /// Забороняє рух по діагоналі в площині XZ, дозволяючи рух по Y.
    /// </summary>
    internal static List<MoveCell> GetNeighbors(MoveCell cell, MoveCell[,,] CellData)
    {
        List<MoveCell> neighbors = new List<MoveCell>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    Vector3Int neighborPos = new Vector3Int(
                        cell.Position.x + dx,
                        cell.Position.y + dy,
                        cell.Position.z + dz
                    );

                    if (!IsValid(neighborPos, CellData))
                        continue;

                    MoveCell neighbor = CellData[neighborPos.x, neighborPos.y, neighborPos.z];
                    if (neighbor == null || !neighbor.IsWalkable || neighbor.OccupyingUnit != null)
                        continue;

                    // --- Строгое ограничение на диагонали по XZ-слою ---
                    if (dx != 0 && dz != 0 && dy == 0)
                    {
                        // Оба прямых соседа ДОЛЖНЫ быть проходимы и не заняты
                        Vector3Int cell1 = new Vector3Int(cell.Position.x + dx, cell.Position.y, cell.Position.z);
                        Vector3Int cell2 = new Vector3Int(cell.Position.x, cell.Position.y, cell.Position.z + dz);

                        bool c1 = IsValid(cell1, CellData) && CellData[cell1.x, cell1.y, cell1.z]?.IsWalkable == true && CellData[cell1.x, cell1.y, cell1.z]?.OccupyingUnit == null;
                        bool c2 = IsValid(cell2, CellData) && CellData[cell2.x, cell2.y, cell2.z]?.IsWalkable == true && CellData[cell2.x, cell2.y, cell2.z]?.OccupyingUnit == null;

                        if (!(c1 && c2))
                            continue;
                    }

                    neighbors.Add(neighbor);
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Перевірка чи координати всередині масиву та не є null.
    /// </summary>
    private static bool IsValid(Vector3Int pos, MoveCell[,,] CellData)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.z >= 0 &&
               pos.x < CellData.GetLength(0) &&
               pos.y < CellData.GetLength(1) &&
               pos.z < CellData.GetLength(2) &&
               CellData[pos.x, pos.y, pos.z] != null;
    }
}





public static class MovementHelper
{
    public static List<MoveCell> GetReachableCells(MoveCell startCell, MoveCell[,,] CellData, int stepsLimit)
    {
        List<MoveCell> reachable = new List<MoveCell>();
        Queue<(MoveCell, int)> queue = new Queue<(MoveCell, int)>();
        HashSet<MoveCell> visited = new HashSet<MoveCell>();

        queue.Enqueue((startCell, 0));
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            var (cell, steps) = queue.Dequeue();
            reachable.Add(cell);

            if (steps >= stepsLimit)
                continue;

            foreach (MoveCell neighbor in Pathfinding.GetNeighbors(cell, CellData))
            {
                if (neighbor == null || !neighbor.IsWalkable || visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, steps + 1)); // ВАЖНО: всегда +1, даже для диагонали!
            }
        }

        return reachable;
    }
}