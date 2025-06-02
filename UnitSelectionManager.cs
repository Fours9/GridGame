using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    private UnitController selectedUnit;

    UnitSpawner unitSpawner;

    public UnitMover mover;  // Прив'яжи у інспекторі


    private void Awake()
    {
        Instance = this;
        unitSpawner = FindAnyObjectByType<UnitSpawner>();

        mover = FindFirstObjectByType<UnitMover>();  // Знайде перший у сцені
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ЛКМ
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitController unit = hit.collider.GetComponent<UnitController>();

                if (unit != null && unit.isPlayerControlled)
                {
                    SelectUnit(unit);
                }
                else
                {
                    SelectUnit(null); // Клик был НЕ по юниту
                }
            }
            else
            {
                SelectUnit(null); // Клик был в "пустоту"
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

        // Скидаємо виділення у всіх юнітів
        foreach (Unit unit in unitSpawner.unitData)
        {
            if (unit != null && unit.UnitObject != null)
            {
                unit.IsSelected = false;

                // Отримуємо UnitController конкретного юніта
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
                    Debug.LogWarning("UnitMover не знайдено у вибраному юніті!");
                }
            }
        }
    }

    public UnitController GetSelectedUnit()
    {
        return selectedUnit;
    }
}