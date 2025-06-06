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

        Debug.Log("������� ���������� (turnOrder):");
        for (int i = 0; i < turnOrder.Count; i++)
            Debug.Log($"[{i}] {turnOrder[i].UnitObject.name} team={turnOrder[i].team} IsAlive={turnOrder[i].IsAlive}");

        Debug.Log($"BuildInitiativeQueue ������. unitData.Count = {unitSpawner.unitData.Count}, turnOrder.Count = {turnOrder.Count}");

        if (turnOrder.Count == 0)
        {
            Debug.LogError("BuildInitiativeQueue: ��� ������ ��� ������� ����������! unitData.Count = " + unitSpawner.unitData.Count);
            foreach (var u in unitSpawner.unitData)
                Debug.Log($"Unit: {u.UnitObject?.name}, health={u.health}, IsAlive={u.IsAlive}");
            return;
        }

        currentUnitIndex = 0;
        Debug.Log($"������� ���������� ���������: {turnOrder.Count} ������. ������ � {turnOrder[0]?.UnitObject?.name}");
    }

    public Unit GetCurrentUnit() => turnOrder.Count > 0 ? turnOrder[currentUnitIndex] : null;

    public void EndCurrentTurn()
    {
        // �������� ����� ������
        UnitSelectionManager.Instance.ClearAllHighlights();

        Debug.Log($"[EndTurn] ���: {currentUnitIndex} ({turnOrder[currentUnitIndex].UnitObject.name}, team={turnOrder[currentUnitIndex].team})");
        // �������� ���� ��������
        var unit = GetCurrentUnit();
        if (unit != null) unit.stepsUsed = 0;

        // ��������� � ���������� ������ �����
        int startIdx = currentUnitIndex;
        do
        {
            currentUnitIndex = (currentUnitIndex + 1) % turnOrder.Count;
            if (turnOrder[currentUnitIndex].IsAlive)
                break;
        } while (currentUnitIndex != startIdx);
        Debug.Log($"[EndTurn] ����: {currentUnitIndex} ({turnOrder[currentUnitIndex].UnitObject.name}, team={turnOrder[currentUnitIndex].team})");
        Debug.Log($"������� ���: {turnOrder[currentUnitIndex].UnitObject.name}");


        StartTurn();
    }

    MoveCell GetClosestFreeNeighbor(Main main, MoveCell targetCell)
    {
        var neighbors = Pathfinding.GetNeighbors(targetCell, main.CellData);
        // ������ ��������� (�� ��� ����� ������ � ����� �� �����)
        return neighbors.FirstOrDefault(n => n.IsWalkable && n.OccupyingUnit == null);
    }

    public void StartTurn()
    {
        var currentUnit = GetCurrentUnit();
        UnitController ctrl = null;

        if (currentUnit != null)
        {
            currentUnit.actionPoints = currentUnit.maxActionPoints; // ��������������� AP!
        }

        if (currentUnit != null && currentUnit.UnitObject != null)
            ctrl = currentUnit.UnitObject.GetComponent<UnitController>();

        if (ctrl != null)
            UnitSelectionManager.Instance.SelectUnit(ctrl);
        else
            UnitSelectionManager.Instance.SelectUnit(null);

        if (currentUnit == null)
        {
            Debug.LogWarning("��� ��������� ����� ��� ����! ��� ��������� ������...");
            return;
        }

        Debug.Log($"����� ����: {currentUnit.UnitObject.name} (Initiative {currentUnit.Initiative}), team={currentUnit.team}");

        // ���� AI � ��������� �� ���
        if (currentUnit.team == TeamType.Enemy)
        {
            Debug.Log("InitiativeManager: ������� ������� EnemyAITurn ��� " + currentUnit.UnitObject.name);
            StartCoroutine(EnemyAITurn(currentUnit));
        }
        // ���� ����� � ��������� ����������
        else
        {
            // UI: �������� �����, �������� ������, ��������� �������� � �����
            // ����� ����� ���������� ��������� �����
        }
    }
    System.Collections.IEnumerator EnemyAITurn(Unit enemyUnit)
    {
        Debug.Log("EnemyAITurn: ����� � AI ��� " + enemyUnit.UnitObject.name);

        Debug.Log($"AI ���� {enemyUnit.UnitObject.name} �������� ���.");

        // ����� ���������� ������
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

        // ���� ��� ����� ������� � ���������� ���
        if (targetPlayer == null)
        {
            Debug.Log("AI: ��� ��������� �����.");
            EndCurrentTurn();
            yield break;
        }
        Debug.Log($"AI: ��������� ����� � {targetPlayer.UnitObject.name}");

        // ��������: ����� �� ��������� �����?
        int chebyshevDist = Mathf.Max(
            Mathf.Abs(enemyUnit.CurrentCell.x - targetPlayer.CurrentCell.x),
            Mathf.Abs(enemyUnit.CurrentCell.y - targetPlayer.CurrentCell.y),
            Mathf.Abs(enemyUnit.CurrentCell.z - targetPlayer.CurrentCell.z)
        );

        if (chebyshevDist <= enemyUnit.attackRange)
        {
            yield return new WaitForSeconds(0.3f);
            enemyUnit.Attack(targetPlayer);
            Debug.Log($"AI: {enemyUnit.UnitObject.name} ������� {targetPlayer.UnitObject.name}");
            yield return new WaitForSeconds(0.3f);
            EndCurrentTurn();
            yield break;
        }

        Main main = FindAnyObjectByType<Main>();
        var pathfinder = FindAnyObjectByType<Pathfinding>();

        // --- �����: ---
        MoveCell targetCell = main.CellData[targetPlayer.CurrentCell.x, targetPlayer.CurrentCell.y, targetPlayer.CurrentCell.z];
        MoveCell attackCell = GetClosestFreeNeighbor(main, targetCell);
        if (attackCell == null)
        {
            Debug.Log("AI: ��� ��������� ������ ����� � �������!");
            // ��� ��� ����� ���:
            var neighbors = Pathfinding.GetNeighbors(targetCell, main.CellData);
            Debug.Log($"AI: ����� ������� {neighbors.Count}, �� ��� walkable + ������: " +
                neighbors.Count(n => n.IsWalkable && n.OccupyingUnit == null));
            foreach (var n in neighbors)
                Debug.Log($"AI: Neighbor {n.Position} IsWalkable={n.IsWalkable} OccupyingUnit={(n.OccupyingUnit == null ? "null" : n.OccupyingUnit.name)}");
            EndCurrentTurn();
            yield break;
        }

        List<MoveCell> path = pathfinder.FindPath(enemyUnit.CurrentCell, attackCell.Position, main.CellData);

        if (path == null || path.Count < 2)
        {
            Debug.Log("AI: ��� ���� � ����.");
            yield return new WaitForSeconds(0.5f);
            EndCurrentTurn();
            yield break;
        }
        Debug.Log($"AI: ���� ������. �����: {path.Count}");

        int steps = Mathf.Min(enemyUnit.RemainingMovement, path.Count - 1);
        List<MoveCell> movePath = path.GetRange(0, steps + 1);

        UnitMover mover = enemyUnit.UnitObject.GetComponent<UnitMover>();
        if (mover != null)
        {
            Debug.Log($"AI: �������EnemyAITurn ���������. �����...");
            Debug.Log($"[AI] ����� StartMoving ��� {enemyUnit.UnitObject.name}, ���� ����� = {movePath.Count}");
            mover.StartCoroutine(mover.StartMoving(movePath, enemyUnit.UnitObject));
            while (mover.isMoving)
                yield return null;
            Debug.Log("AI: �������� ���������!");
        }
        else
        {
            Debug.LogWarning("AI: ��� UnitMover � �����!");
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
            Debug.Log($"AI: {enemyUnit.UnitObject.name} ������� {targetPlayer.UnitObject.name}");
        }

        yield return new WaitForSeconds(0.3f);
        EndCurrentTurn();
    }
}