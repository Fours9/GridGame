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

        StartTurn();
    }

    public void StartTurn()
    {
        var currentUnit = GetCurrentUnit();
        Debug.Log($"[TURN DEBUG] Ходит юнит: {currentUnit.UnitObject.name}, team={currentUnit.team}, isPlayerControlled={currentUnit.isPlayerControlled}");

        Debug.Log("InitiativeManager: StartTurn вызывается! [DEBUG]");
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
        int manhattanDist = Mathf.Abs(enemyUnit.CurrentCell.x - targetPlayer.CurrentCell.x)
                          + Mathf.Abs(enemyUnit.CurrentCell.y - targetPlayer.CurrentCell.y)
                          + Mathf.Abs(enemyUnit.CurrentCell.z - targetPlayer.CurrentCell.z);

        if (manhattanDist <= enemyUnit.attackRange)
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
        List<MoveCell> path = pathfinder.FindPath(enemyUnit.CurrentCell, targetPlayer.CurrentCell, main.CellData);

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
            mover.StartMoving(movePath);
            while (mover.isMoving)
                yield return null;
            Debug.Log("AI: Движение завершено!");
        }
        else
        {
            Debug.LogWarning("AI: Нет UnitMover у врага!");
        }

        manhattanDist = Mathf.Abs(enemyUnit.CurrentCell.x - targetPlayer.CurrentCell.x)
                      + Mathf.Abs(enemyUnit.CurrentCell.y - targetPlayer.CurrentCell.y)
                      + Mathf.Abs(enemyUnit.CurrentCell.z - targetPlayer.CurrentCell.z);

        if (manhattanDist <= enemyUnit.attackRange)
        {
            yield return new WaitForSeconds(0.3f);
            enemyUnit.Attack(targetPlayer);
            Debug.Log($"AI: {enemyUnit.UnitObject.name} атакует {targetPlayer.UnitObject.name}");
        }

        yield return new WaitForSeconds(0.3f);
        EndCurrentTurn();
    }
}