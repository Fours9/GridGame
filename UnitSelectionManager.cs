using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    private UnitController selectedUnit;

    private void Awake()
    {
        Instance = this;
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
                    selectedUnit.Deselect(); // Клик был НЕ по юниту
                }
            }
            else
            {
                selectedUnit.Deselect(); // Клик был в "пустоту"
            }
        }
    }

    public void SelectUnit(UnitController unit)
    {
        if (selectedUnit != null)
            selectedUnit.Deselect();

        selectedUnit = unit;
        selectedUnit.Select();
    }

    public UnitController GetSelectedUnit()
    {
        return selectedUnit;
    }
}