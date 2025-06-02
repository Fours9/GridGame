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
                UnitController unit = hit.collider.GetComponent<UnitController>();

                if (unit != null && unit.isPlayerControlled)
                {
                    SelectUnit(unit);
                }
                else
                {
                    SelectUnit(null); // ���� ��� �� �� �����
                }
            }
            else
            {
                SelectUnit(null); // ���� ��� � "�������"
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
            }
        }
    }

    public UnitController GetSelectedUnit()
    {
        return selectedUnit;
    }
}