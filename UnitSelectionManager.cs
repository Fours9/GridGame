using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    private UnitController selectedUnit;

    UnitSpawner unitSpawner;

    public UnitMover mover;  // ѕрив'€жи у ≥нспектор≥


    private void Awake()
    {
        Instance = this;
        unitSpawner = FindAnyObjectByType<UnitSpawner>();

        mover = FindFirstObjectByType<UnitMover>();  // «найде перший у сцен≥
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Ћ ћ
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
                var ctrl = hit.collider.GetComponent<UnitController>();
                Debug.Log(ctrl != null ? $"UnitController найден: {ctrl.gameObject.name}, isPlayerControlled={ctrl.isPlayerControlled}" : "UnitController Ќ≈ найден!");

                UnitController unit = hit.collider.GetComponent<UnitController>();
                if (unit != null)
                    Debug.Log($"{unit.gameObject.name}: isPlayerControlled={unit.isPlayerControlled}");

                if (unit != null && unit.isPlayerControlled)
                {
                    SelectUnit(unit);
                    ClearAllHighlights(); // —брасываем подсветку всех клеток
                }
                else
                {
                    SelectUnit(null); //  лик был Ќ≈ по юниту
                    ClearAllHighlights();
                }
            }
            else
            {
                SelectUnit(null); //  лик был в "пустоту"
                ClearAllHighlights();
            }
        }
    }

    public void SelectUnit(UnitController unitSelect)
    {
        if (unitSpawner == null)
        {
            Debug.LogError("UnitSpawner не знайдено!");
            return;
        }

        // 1. —Ѕ–ј—џ¬ј≈ћ ѕќƒ—¬≈“ ”
        ClearAllHighlights();

        if (unitSelect != null)
        {
            var unitData = unitSelect.unitData;
            var main = FindAnyObjectByType<Main>(); // или ссылка на Main, если есть
            var startCell = main.CellData[unitData.CurrentCell.x, unitData.CurrentCell.y, unitData.CurrentCell.z];
            int steps = unitData.RemainingMovement;
            var reachable = MovementHelper.GetReachableCells(startCell, main.CellData, steps);

            main.HighlightReachableCells(reachable);
        }
        else
        {
            // ≈сли юнит не выбран Ч сбросить подсветку
            var main = FindAnyObjectByType<Main>();
            main.HighlightReachableCells(null);
        }

        // —кидаЇмо вид≥ленн€ у вс≥х юн≥т≥в
        foreach (Unit unit in unitSpawner.unitData)
        {
            if (unit != null && unit.UnitObject != null)
            {
                unit.IsSelected = false;

                // ќтримуЇмо UnitController конкретного юн≥та
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
                    Debug.LogWarning("UnitMover не знайдено у вибраному юн≥т≥!");
                }

                // 2. ѕќƒ—¬≈“ ј ƒќ—“”ѕЌџ’  Ћ≈“ќ  ƒЋя ’ќƒј:
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
        // ѕолучи Main из сцены, если надо Ч через FindObjectOfType<Main>();
        Main main = FindFirstObjectByType<Main>();
        if (main == null) return;

        if (unit == null || unit.undercell == null)
            return;

        int movesLeft = unit.RemainingMovement;

        // »щем все достижимые клетки BFS-ом
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