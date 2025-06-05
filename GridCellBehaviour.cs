using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GridCellBehaviour : MonoBehaviour
{
    private Color originalColor;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isRightMouseHeld = false;

    private Main main;
    private Vector3Int startCoords;
    UnitSpawner unitSpawner;
    private GameObject unit;

    public bool isReachable = false; // ������ ���������?
    private Color reachableColor = Color.blue;
    private Color hoverColor = Color.yellow;
    private Color clickColor = Color.green;

    public Color defaultColor;
    public Color highlightColor = new Color(1.0f, 0.5f, 0f, 1f); // ���������
    public Color attackColor = Color.white; // �����

    private Renderer rend;

    public Unit unitOnCell;

    public MoveCell[,,] CellData => main?.CellData;

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;

        // ������ �� �������� �������
        if (defaultColor.a == 0) // ���� �� ����� ���� � ������������ ��������
            defaultColor = originalColor;

        rend.material.color = defaultColor;

        main = FindAnyObjectByType<Main>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
    }
    void OnMouseEnter()
    {
        isHovered = true;
        rend.material.color = hoverColor;

        var selectedUC = GetSelectedUnitController();
        if (unitOnCell != null && selectedUC != null && unitOnCell.team != selectedUC.unitData.team)
            rend.material.color = highlightColor; // ���������
    }

    void OnMouseExit()
    {
        isHovered = false; // <-- ��������!
        ResetColor(); // ������� �������� (��� ����� ��� reachable)
    }

    void OnMouseUp()
    {
        if (isReachable)
            rend.material.color = reachableColor;
        else
            ResetColor();
    }

    void OnMouseOver()
    {
        var selectedUC = GetSelectedUnitController();
        if (Input.GetMouseButtonDown(1) && unitOnCell != null && unitOnCell.team != selectedUC.unitData.team)
        {
            rend.material.color = attackColor; // �����
                                               // ������ ������ ����� (��. ����)
            TryAttack(unitOnCell);
        }
    }

    public void SetReachableHighlight(bool enable)
    {
        isReachable = enable;
        ResetColor(); // ������ ��������� ���� ���������
    }

    void Update()
    {
        // 1. ������ ��� ��� ����� ������� � ���������� ������
        if (isHovered && Input.GetMouseButtonDown(1))
        {
            isRightMouseHeld = true;
            rend.material.color = clickColor;
        }

        // 2. ���� ������ � ����� ������� ������ (����� �� �����������)
        if (isHovered && isRightMouseHeld && Input.GetMouseButton(1))
        {
            rend.material.color = clickColor;
        }

        // 3. ��������� � ���� ������ ��� �������, ��������� ��������
        if (isHovered && isRightMouseHeld && Input.GetMouseButtonUp(1))
        {
            isRightMouseHeld = false;
            ResetColor(); // ������� ��������/�����

            TryMoveUnitHere(); // �������� ������, ���� ������ walkable
        }

        // 4. ���� ���� � ������ ������ � �������� ������
        if (!isHovered && isRightMouseHeld)
        {
            isRightMouseHeld = false;
            ResetColor();
        }
    }

    private void TryMoveUnitHere()
    {
        // �������� ���������� ���������� �����
        var selectedUC = GetSelectedUnitController();
        if (selectedUC == null) return;
        Unit selectedUnit = selectedUC.unitData;

        // !!! ������ ��� !!!
        if (selectedUnit.team != TeamType.Player)
            return; // ���������� ������� ������� �����

        // ���������, ����� �� ����� �� ���
        if (selectedUnit != InitiativeManager.Instance.GetCurrentUnit()) return;

        GameObject unitObj = selectedUnit.UnitObject;
        if (unitObj == null) return;

        Vector3 playerPos = unitObj.transform.position;
        startCoords = Vector3Int.RoundToInt(playerPos);

        Vector3Int targetCoords = Vector3Int.RoundToInt(transform.position);

        if (main == null || main.CellData == null)
        {
            Debug.LogError("main ��� main.CellData �� �������������������!");
            return;
        }

        if (targetCoords.x < 0 || targetCoords.y < 0 || targetCoords.z < 0 ||
            targetCoords.x >= main.width || targetCoords.y >= main.mapHeight || targetCoords.z >= main.height)
        {
            Debug.LogError("targetCoords ��� ������! " + targetCoords);
            return;
        }

        var targetCell = main.CellData[targetCoords.x, targetCoords.y, targetCoords.z];
        if (targetCell == null || !targetCell.IsWalkable)
        {
            Debug.LogWarning("������� ������ �� ��������!");
            return;
        }

        Debug.Log($"Start: {startCoords}, Target: {targetCoords}");

        var pathfinder = FindAnyObjectByType<Pathfinding>();
        if (pathfinder != null)
        {
            List<MoveCell> path = pathfinder.FindPath(startCoords, targetCoords, CellData);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("Pathfinding returned no valid path.");
                return;
            }

            UnitMover move = unitObj.GetComponent<UnitMover>();
            if (move != null)
            {
                path = path.FindAll(cell => cell != null);
                if (path.Count == 0) return;
                move.StartMoving(path);
            }
        }
    }

    private IEnumerator ResetCellColorAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        ResetColor(); // <-- � �� rend.material.color = originalColor;
    }

    public void ResetColor()
    {
        if (isReachable)
            rend.material.color = reachableColor; // ����� ���� ���������
        else
            rend.material.color = defaultColor;   // ������� ����
    }

    public void ClearAllHighlights()
    {
        Main main = FindFirstObjectByType<Main>();
        if (main == null) return;
        foreach (var cell in main.CellData)
            if (cell != null && cell.CellObject != null)
            {
                var cellScript = cell.CellObject.GetComponent<GridCellBehaviour>();
                if (cellScript != null)
                    cellScript.ResetColor();
            }
    }

    void TryAttack(Unit target)
    {
        var activeUC = GetSelectedUnitController();
        if (activeUC == null) return;
        Unit myUnit = activeUC.unitData;

        Unit activeUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (myUnit != activeUnit) return;

        int distance = GetDistance(myUnit.CurrentCell, target.CurrentCell);
        if (distance <= myUnit.attackRange)
        {
            myUnit.Attack(target);
            Debug.Log("������ �����!");
            return;
        }

        // ����� ������ ��� �����
        List<Vector3Int> possibleAttackCells = GetCellsAround(target.CurrentCell, myUnit.attackRange);
        MoveCell moveTarget = null;
        List<MoveCell> pathToCell = null;
        var pathfinder = FindAnyObjectByType<Pathfinding>();

        foreach (var cellPos in possibleAttackCells)
        {
            if (cellPos.x < 0 || cellPos.y < 0 || cellPos.z < 0 ||
                cellPos.x >= main.width || cellPos.y >= main.height || cellPos.z >= main.mapHeight)
                continue;

            var targetCell = main.CellData[cellPos.x, cellPos.y, cellPos.z];
            if (targetCell == null || !targetCell.IsWalkable) continue;

            var path = pathfinder.FindPath(myUnit.CurrentCell, cellPos, CellData);

            if (path != null && path.Count - 1 <= myUnit.RemainingMovement)
            {
                moveTarget = targetCell;
                pathToCell = path;
                break;
            }
        }

        if (moveTarget != null && pathToCell != null)
        {
            // ���������, � ����� �������� � ����� (����� ����� callback/�����, �� ��� ������ ����� ������ ����� ��������)
            UnitMover mover = activeUC.gameObject.GetComponent<UnitMover>();
            if (mover != null)
            {
                mover.StartMoving(pathToCell);
                // ������� �������� �������� ��������� ��������, � ����� myUnit.Attack(target);
            }
        }
        else
        {
            Debug.Log("��� ��������� ������ ��� �����! ������� ������.");
        }
    }

    int GetDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z); // ���������
    }

    List<Vector3Int> GetCellsAround(Vector3Int center, int range)
    {
        var cells = new List<Vector3Int>();
        for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
                for (int dz = -range; dz <= range; dz++)
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz) <= range)
                        cells.Add(center + new Vector3Int(dx, dy, dz));
        return cells;
    }

    UnitController GetSelectedUnitController()
    {
        foreach (Unit unit in unitSpawner.unitData)
            if (unit.IsSelected && unit.isPlayerControlled && unit.UnitObject != null)
                return unit.UnitObject.GetComponent<UnitController>();
        return null;
    }
}