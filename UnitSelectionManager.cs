using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    private UnitController selectedUnit;

    UnitSpawner unitSpawner;

    public UnitMover mover;  // ����'��� � ���������


    private void Awake()
    {
        Instance = this;
        unitSpawner = FindAnyObjectByType<UnitSpawner>();

        mover = FindFirstObjectByType<UnitMover>();  // ������ ������ � ����
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ���
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
                var ctrl = hit.collider.GetComponent<UnitController>();
                Debug.Log(ctrl != null ? $"UnitController ������: {ctrl.gameObject.name}, isPlayerControlled={ctrl.isPlayerControlled}" : "UnitController �� ������!");

                UnitController unit = hit.collider.GetComponent<UnitController>();
                if (unit != null)
                    Debug.Log($"{unit.gameObject.name}: isPlayerControlled={unit.isPlayerControlled}");

                if (unit != null && unit.isPlayerControlled)
                {
                    SelectUnit(unit);
                    ClearAllHighlights(); // ���������� ��������� ���� ������
                }
                else
                {
                    SelectUnit(null); // ���� ��� �� �� �����
                    ClearAllHighlights();
                }
            }
            else
            {
                SelectUnit(null); // ���� ��� � "�������"
                ClearAllHighlights();
            }
        }
    }

    public void SelectUnit(UnitController unitSelect)
    {
        if (unitSpawner == null)
        {
            Debug.LogError("UnitSpawner �� ��������!");
            return;
        }

        // 1. ���������� ���������
        ClearAllHighlights();

        if (unitSelect != null)
        {
            var unitData = unitSelect.unitData;
            var main = FindAnyObjectByType<Main>(); // ��� ������ �� Main, ���� ����
            var startCell = main.CellData[unitData.CurrentCell.x, unitData.CurrentCell.y, unitData.CurrentCell.z];
            int steps = unitData.RemainingMovement;
            var reachable = MovementHelper.GetReachableCells(startCell, main.CellData, steps);

            main.HighlightReachableCells(reachable);
        }
        else
        {
            // ���� ���� �� ������ � �������� ���������
            var main = FindAnyObjectByType<Main>();
            main.HighlightReachableCells(null);
        }

        // ������� �������� � ��� ����
        foreach (Unit unit in unitSpawner.unitData)
        {
            if (unit != null && unit.UnitObject != null)
            {
                unit.IsSelected = false;

                // �������� UnitController ����������� ����
                UnitController ctrl = unit.UnitObject.GetComponent<UnitController>();
                if (ctrl != null)
                {
                    ctrl.Deselect();
                }
            }
        }

        selectedUnit = unitSelect;

        if (unitSelect == null)
            return;

        foreach (Unit unit in unitSpawner.unitData)
        {
            if (unit != null && unit.UnitObject == unitSelect.gameObject)
            {
                unit.IsSelected = true;

                UnitController ctrl = unitSelect.GetComponent<UnitController>();
                if (ctrl != null)
                {
                    ctrl.Select();
                }

                UnitMover mover = unitSelect.GetComponent<UnitMover>();
                if (mover != null)
                {
                    mover.SetAsActiveUnit();
                }
                else
                {
                    Debug.LogWarning("UnitMover �� �������� � ��������� ���!");
                }

                // 2. ��������� ��������� ������ ��� ����:
                ShowAvailableMoves(unit);

            }
        }
    }

    public UnitController GetSelectedUnit()
    {
        return selectedUnit;
    }

    public void ShowAvailableMoves(Unit unit)
    {
        // ������ Main �� �����, ���� ���� � ����� FindObjectOfType<Main>();
        Main main = FindFirstObjectByType<Main>();
        if (main == null) return;

        if (unit == null || unit.undercell == null)
            return;

        int movesLeft = unit.RemainingMovement;

        // ���� ��� ���������� ������ BFS-��
        var reachableCells = MovementHelper.GetReachableCells(unit.undercell, main.CellData, movesLeft);

        foreach (var cell in reachableCells)
            cell.Highlight(Color.blue);
    }

    public void ClearAllHighlights()
    {
        Main main = FindAnyObjectByType<Main>();
        if (main == null) return;
        foreach (var cell in main.CellData)
        {
            if (cell != null && cell.CellObject != null)
            {
                var gridCellBehaviour = cell.CellObject.GetComponent<GridCellBehaviour>();
                if (gridCellBehaviour != null)
                    gridCellBehaviour.ResetColor();
            }
        }
    }
}