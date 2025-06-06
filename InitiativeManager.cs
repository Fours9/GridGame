using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InitiativeManager : MonoBehaviour
{
    public static InitiativeManager Instance { get; private set; }

    public List<Unit> turnOrder = new List<Unit>();
    public int currentUnitIndex = 0;
    UnitSpawner unitSpawner;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
    }

    void Start()
    {

    }

    public void BuildInitiativeQueue()
    {
        turnOrder = unitSpawner.unitData
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Initiative)
            .ToList();

        Debug.Log("Очередь инициативы (turnOrder):");
        for (int i = 0; i < turnOrder.Count; i++)
            Debug.Log($"[{i}] {turnOrder[i].UnitObject.name} team={turnOrder[i].team} IsAlive={turnOrder[i].IsAlive}");

        Debug.Log($"BuildInitiativeQueue вызван. unitData.Count = {unitSpawner.unitData.Count}, turnOrder.Count = {turnOrder.Count}");

        if (turnOrder.Count == 0)
        {
            Debug.LogError("BuildInitiativeQueue: Нет юнитов для очереди инициативы! unitData.Count = " + unitSpawner.unitData.Count);
            foreach (var u in unitSpawner.unitData)
                Debug.Log($"Unit: {u.UnitObject?.name}, health={u.health}, IsAlive={u.IsAlive}");
            return;
        }

        currentUnitIndex = 0;
        Debug.Log($"Очередь инициативы построена: {turnOrder.Count} юнитов. Первый — {turnOrder[0]?.UnitObject?.name}");
    }

    public Unit GetCurrentUnit() => turnOrder.Count > 0 ? turnOrder[currentUnitIndex] : null;

    public void EndCurrentTurn()
    {
        // Сбросить синие клетки
        UnitSelectionManager.Instance.ClearAllHighlights();

        Debug.Log($"[EndTurn] Был: {currentUnitIndex} ({turnOrder[currentUnitIndex].UnitObject.name}, team={turnOrder[currentUnitIndex].team})");
        // Сбросить очки движения
        var unit = GetCurrentUnit();
        if (unit != null) unit.stepsUsed = 0;

        // Переходим к следующему живому юниту
        int startIdx = currentUnitIndex;
        do
        {
            currentUnitIndex = (currentUnitIndex + 1) % turnOrder.Count;
            if (turnOrder[currentUnitIndex].IsAlive)
                break;
        } while (currentUnitIndex != startIdx);
        Debug.Log($"[EndTurn] Стал: {currentUnitIndex} ({turnOrder[currentUnitIndex].UnitObject.name}, team={turnOrder[currentUnitIndex].team})");
        Debug.Log($"Передан ход: {turnOrder[currentUnitIndex].UnitObject.name}");


        StartTurn();
    }

    MoveCell GetClosestFreeNeighbor(Main main, MoveCell targetCell)
    {
        var neighbors = Pathfinding.GetNeighbors(targetCell, main.CellData);
        // Только свободные (по ним можно ходить и никто не стоит)
        return neighbors.FirstOrDefault(n => n.IsWalkable && n.OccupyingUnit == null);
    }

    public void StartTurn()
    {
        var currentUnit = GetCurrentUnit();
        UnitController ctrl = null;

        if (currentUnit != null)
        {
            currentUnit.actionPoints = currentUnit.maxActionPoints; // Восстанавливаем AP!
        }

        if (currentUnit != null && currentUnit.UnitObject != null)
            ctrl = currentUnit.UnitObject.GetComponent<UnitController>();

        if (ctrl != null)
            UnitSelectionManager.Instance.SelectUnit(ctrl);
        else
            UnitSelectionManager.Instance.SelectUnit(null);

        if (currentUnit == null)
        {
            Debug.LogWarning("Нет активного юнита для хода! Жду появления юнитов...");
            return;
        }

        Debug.Log($"Ходит юнит: {currentUnit.UnitObject.name} (Initiative {currentUnit.Initiative}), team={currentUnit.team}");

        // Если AI — запускаем ИИ ход
        if (currentUnit.team == TeamType.Enemy)
        {
            Debug.Log("InitiativeManager: Попытка вызвать EnemyAITurn для " + currentUnit.UnitObject.name);
            StartCoroutine(EnemyAITurn(currentUnit));
        }
        // Если игрок — разрешаем управление
        else
        {
            // UI: выделить юнита, включить кнопки, разрешить движение и атаки
            // Здесь можно подсветить активного юнита
        }
    }
    System.Collections.IEnumerator EnemyAITurn(Unit enemyUnit)
    {
        Debug.Log("EnemyAITurn: Зашёл в AI для " + enemyUnit.UnitObject.name);

        Debug.Log($"AI враг {enemyUnit.UnitObject.name} начинает ход.");

        // Найти ближайшего игрока
        Unit targetPlayer = null;
        float minDist = float.MaxValue;
        foreach (var unit in unitSpawner.unitData)
        {
            if (unit.team == TeamType.Player && unit.IsAlive)
            {
                float dist = Vector3Int.Distance(enemyUnit.CurrentCell, unit.CurrentCell);
                if (dist < minDist)
                {
                    minDist = dist;
                    targetPlayer = unit;
                }
            }
        }

        // Если нет живых игроков — пропустить ход
        if (targetPlayer == null)
        {
            Debug.Log("AI: Нет доступных целей.");
            EndCurrentTurn();
            yield break;
        }
        Debug.Log($"AI: ближайший игрок — {targetPlayer.UnitObject.name}");

        // Проверка: можно ли атаковать сразу?
        int chebyshevDist = Mathf.Max(
            Mathf.Abs(enemyUnit.CurrentCell.x - targetPlayer.CurrentCell.x),
            Mathf.Abs(enemyUnit.CurrentCell.y - targetPlayer.CurrentCell.y),
            Mathf.Abs(enemyUnit.CurrentCell.z - targetPlayer.CurrentCell.z)
        );

        if (chebyshevDist <= enemyUnit.attackRange)
        {
            yield return new WaitForSeconds(0.3f);
            enemyUnit.Attack(targetPlayer);
            Debug.Log($"AI: {enemyUnit.UnitObject.name} атакует {targetPlayer.UnitObject.name}");
            yield return new WaitForSeconds(0.3f);
            EndCurrentTurn();
            yield break;
        }

        Main main = FindAnyObjectByType<Main>();
        var pathfinder = FindAnyObjectByType<Pathfinding>();

        // --- Новое: ---
        MoveCell targetCell = main.CellData[targetPlayer.CurrentCell.x, targetPlayer.CurrentCell.y, targetPlayer.CurrentCell.z];
        MoveCell attackCell = GetClosestFreeNeighbor(main, targetCell);
        if (attackCell == null)
        {
            Debug.Log("AI: Нет свободных клеток рядом с игроком!");
            // Вот это новый лог:
            var neighbors = Pathfinding.GetNeighbors(targetCell, main.CellData);
            Debug.Log($"AI: Всего соседей {neighbors.Count}, из них walkable + пустые: " +
                neighbors.Count(n => n.IsWalkable && n.OccupyingUnit == null));
            foreach (var n in neighbors)
                Debug.Log($"AI: Neighbor {n.Position} IsWalkable={n.IsWalkable} OccupyingUnit={(n.OccupyingUnit == null ? "null" : n.OccupyingUnit.name)}");
            EndCurrentTurn();
            yield break;
        }

        List<MoveCell> path = pathfinder.FindPath(enemyUnit.CurrentCell, attackCell.Position, main.CellData);

        if (path == null || path.Count < 2)
        {
            Debug.Log("AI: Нет пути к цели.");
            yield return new WaitForSeconds(0.5f);
            EndCurrentTurn();
            yield break;
        }
        Debug.Log($"AI: путь найден. Длина: {path.Count}");

        int steps = Mathf.Min(enemyUnit.RemainingMovement, path.Count - 1);
        List<MoveCell> movePath = path.GetRange(0, steps + 1);

        UnitMover mover = enemyUnit.UnitObject.GetComponent<UnitMover>();
        if (mover != null)
        {
            Debug.Log($"AI: ПытаетсEnemyAITurn двигаться. Старт...");
            Debug.Log($"[AI] Вызов StartMoving для {enemyUnit.UnitObject.name}, путь длина = {movePath.Count}");
            mover.StartCoroutine(mover.StartMoving(movePath, enemyUnit.UnitObject));
            while (mover.isMoving)
                yield return null;
            Debug.Log("AI: Движение завершено!");
        }
        else
        {
            Debug.LogWarning("AI: Нет UnitMover у врага!");
        }

        chebyshevDist = Mathf.Max(
            Mathf.Abs(enemyUnit.CurrentCell.x - targetPlayer.CurrentCell.x),
            Mathf.Abs(enemyUnit.CurrentCell.y - targetPlayer.CurrentCell.y),
            Mathf.Abs(enemyUnit.CurrentCell.z - targetPlayer.CurrentCell.z)
        );

        if (chebyshevDist <= enemyUnit.attackRange)
        {
            yield return new WaitForSeconds(0.3f);
            enemyUnit.Attack(targetPlayer);
            Debug.Log($"AI: {enemyUnit.UnitObject.name} атакует {targetPlayer.UnitObject.name}");
        }

        yield return new WaitForSeconds(0.3f);
        EndCurrentTurn();
    }
}