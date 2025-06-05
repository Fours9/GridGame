using System.Collections.Generic;
using UnityEngine;

public class MouseInputManager : MonoBehaviour
{
    public UnitMover unitMover; // �������� ����� ���������
    public Main main;           // ������ �� ���� ��������� �����

    // ����� �������� ������ �� ���������, ���� ����
    // public UnitSelectionManager unitSelectionManager;

    void Awake()
    {
        if (main == null)
            main = FindAnyObjectByType<Main>();
        if (unitMover == null)
            unitMover = FindAnyObjectByType<UnitMover>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var unitCtrl = hit.collider.GetComponent<UnitController>();
                if (unitCtrl != null)
                {
                    Debug.Log("[ATTACK] ������� ��: " + unitCtrl.unitData.UnitName);
                    TryMoveAndAttack(unitCtrl.unitData);
                    return;
                }

                var cell = hit.collider.GetComponent<GridCellBehaviour>();
                if (cell != null)
                {
                    Debug.Log("[MOVE] ������� �� ������: " + cell.name);
                    // ... ���� ������ ����������� �� ������
                }
            }
        }
    }

    void TryMoveAndAttack(Unit target)
    {
        Unit myUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (myUnit == null || myUnit.team == target.team) return;

        int distance = Mathf.Max(
            Mathf.Abs(myUnit.CurrentCell.x - target.CurrentCell.x),
            Mathf.Abs(myUnit.CurrentCell.y - target.CurrentCell.y),
            Mathf.Abs(myUnit.CurrentCell.z - target.CurrentCell.z)
        );

        if (distance <= myUnit.attackRange)
        {
            Debug.Log($"[ATTACK] ������� {target.UnitName}!");
            myUnit.Attack(target);
        }
        else
        {
            // --- �����: ������ ���� � �������� ������ ---
            MoveCell targetCell = GetNearestFreeAdjacentCell(target.CurrentCell);
            if (targetCell == null)
            {
                Debug.Log("[MOVE] ��� ��������� ������ ����� � ������!");
                return;
            }

            // ���� Pathfinding ����� �� ��� �� �������, ��� � Main:
            var pathfinding = main.GetComponent<Pathfinding>();
            // ���� ��������� ������ � �������� ��� � ����������!
            var path = pathfinding.FindPath(myUnit.CurrentCell, targetCell.Position, main.CellData);

            if (path == null || path.Count == 0)
            {
                Debug.Log("[MOVE] ���� � ����� �� ������!");
                return;
            }

            // ���������! ����� ��������� � ������� ����� (���� �������)
            StartCoroutine(MoveAndAttackCoroutine(path, myUnit, target));
        }
    }

    // �������� ��������� ��������� ������ ������ �����
    MoveCell GetNearestFreeAdjacentCell(Vector3Int enemyCell)
    {
        Vector3Int[] dirs = new Vector3Int[] {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    };

        MoveCell result = null;
        float minDist = float.MaxValue;

        if (main == null) { Debug.LogError("main == null � MouseInputManager!"); return null; }
        if (main.CellData == null) { Debug.LogError("main.CellData == null!"); return null; }

        Unit currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (currentUnit == null) { Debug.LogError("currentUnit == null!"); return null; }

        Vector3Int myCell = currentUnit.CurrentCell;

        foreach (var dir in dirs)
        {
            Vector3Int pos = enemyCell + dir;
            if (!main.IsCellInBounds(pos)) continue;
            MoveCell cell = main.CellData[pos.x, pos.y, pos.z];
            if (cell == null || cell.unitOnCell != null) continue; // ������

            float dist = Vector3Int.Distance(myCell, pos);
            if (dist < minDist)
            {
                minDist = dist;
                result = cell;
            }
        }
        if (result == null) Debug.LogWarning("��� ��������� ������ ����� � ������!");
        return result;
    }

    // ��������: ��� -> ������� ���� �����
    System.Collections.IEnumerator MoveAndAttackCoroutine(List<MoveCell> path, Unit myUnit, Unit target)
    {
        // ���� UnitMover ��������� �����
        var mover = myUnit.UnitObject.GetComponent<UnitMover>();
        if (mover == null)
        {
            Debug.LogError("� ���������� ����� ��� ���������� UnitMover!");
            yield break;
        }

        Debug.Log($"[MoveAndAttack] ����� �������� ��� {myUnit.UnitName}");
        yield return mover.StartMoving(path, myUnit.UnitObject);

        int distance = Mathf.Max(
            Mathf.Abs(myUnit.CurrentCell.x - target.CurrentCell.x),
            Mathf.Abs(myUnit.CurrentCell.y - target.CurrentCell.y),
            Mathf.Abs(myUnit.CurrentCell.z - target.CurrentCell.z)
        );
        if (distance <= myUnit.attackRange)
        {
            if (myUnit.actionPoints > 0)
            {
                myUnit.Attack(target);
                myUnit.actionPoints--;
                Debug.Log($"[ATTACK] ���� �������, actionPoints ������: {myUnit.actionPoints}");
            }
            else
            {
                Debug.Log("[ATTACK] ��� actionPoints, ����� ����������!");
            }
        }
        else
        {
            Debug.Log("[ATTACK] �������, �� �� ������� ���� ��� �����!");
        }
    }
}