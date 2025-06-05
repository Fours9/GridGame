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

    public bool isReachable = false; // Клетка достижима?
    private Color reachableColor = Color.blue;
    private Color hoverColor = Color.yellow;
    private Color clickColor = Color.green;

    public Color defaultColor;
    public Color highlightColor = new Color(1.0f, 0.5f, 0f, 1f); // Оранжевый
    public Color attackColor = Color.white; // Белый

    private Renderer rend;

    public MoveCell myCell;
    public MoveCell[,,] CellData => main?.CellData;

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;

        // Защита от “черного старта”
        if (defaultColor.a == 0) // Если не задан явно — использовать оригинал
            defaultColor = originalColor;

        rend.material.color = defaultColor;

        main = FindAnyObjectByType<Main>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();

        if (myCell == null) Debug.LogError($"myCell не назначен у {gameObject.name}!", this);
    }
    void OnMouseEnter()
    {
        isHovered = true;
        rend.material.color = hoverColor;

        var selectedUC = GetSelectedUnitController();
        if (myCell.unitOnCell != null && selectedUC != null && myCell.unitOnCell.team != selectedUC.unitData.team)
            rend.material.color = highlightColor; // оранжевый
    }

    void OnMouseExit()
    {
        isHovered = false; // <-- Добавить!
        ResetColor(); // Вернуть исходный (или синий для reachable)
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
        //Debug.Log($"[OnMouseOver] unitOnCell={myCell.unitOnCell?.team}, name={myCell.unitOnCell?.UnitObject?.name}");
        var selectedUC = GetSelectedUnitController();
        var targetUnit = myCell.unitOnCell; // <-- только так!
        if (Input.GetMouseButtonDown(1) && targetUnit != null && selectedUC != null && targetUnit.team != selectedUC.unitData.team)
        {
            //TryAttack(targetUnit);
        }
        string unitName = targetUnit != null ? targetUnit.UnitName : "(Пусто)";
        //Debug.Log($"[OnMouseOver] unitOnCell={unitName}, name={this.name}");
    }

    public void SetReachableHighlight(bool enable)
    {
        isReachable = enable;
        ResetColor(); // Всегда сбрасывай цвет корректно
    }

    void Update()
    {
        // 1. Зажата ПКМ над ЛЮБОЙ клеткой — подсветить зелёным
        if (isHovered && Input.GetMouseButtonDown(1))
        {
            isRightMouseHeld = true;
            rend.material.color = clickColor;
        }

        // 2. Пока держим — пусть остаётся зелёной (можно не обязательно)
        if (isHovered && isRightMouseHeld && Input.GetMouseButton(1))
        {
            rend.material.color = clickColor;
        }

        // 3. Отпустили — если курсор над клеткой, запустить движение
        if (isHovered && isRightMouseHeld && Input.GetMouseButtonUp(1))
        {
            isRightMouseHeld = false;
            ResetColor(); // Вернуть исходный/синий

            TryMoveUnitHere(); // ДВИЖЕНИЕ всегда, если клетка walkable
        }

        // 4. Если ушли с клетки мышкой — сбросить зелёный
        if (!isHovered && isRightMouseHeld)
        {
            isRightMouseHeld = false;
            ResetColor();
        }
    }

    public void TryMoveUnitHere()
    {
        // Получаем контроллер выбранного юнита
        var selectedUC = GetSelectedUnitController();
        if (selectedUC == null) return;
        Unit selectedUnit = selectedUC.unitData;

        // !!! ДОБАВЬ ЭТО !!!
        if (selectedUnit.team != TeamType.Player)
            return; // Игнорируем попытки двигать врага

        // Проверяем, имеет ли право на ход
        if (selectedUnit != InitiativeManager.Instance.GetCurrentUnit()) return;

        GameObject unitObj = selectedUnit.UnitObject;
        if (unitObj == null) return;

        Vector3 playerPos = unitObj.transform.position;
        startCoords = Vector3Int.RoundToInt(playerPos);

        Vector3Int targetCoords = Vector3Int.RoundToInt(transform.position);

        if (main == null || main.CellData == null)
        {
            Debug.LogError("main или main.CellData не проинициализированы!");
            return;
        }

        if (targetCoords.x < 0 || targetCoords.y < 0 || targetCoords.z < 0 ||
            targetCoords.x >= main.width || targetCoords.y >= main.mapHeight || targetCoords.z >= main.height)
        {
            Debug.LogError("targetCoords вне границ! " + targetCoords);
            return;
        }

        var targetCell = main.CellData[targetCoords.x, targetCoords.y, targetCoords.z];
        if (targetCell == null || !targetCell.IsWalkable)
        {
            Debug.LogWarning("Целевая клетка не доступна!");
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
                move.StartCoroutine(move.StartMoving(path, selectedUnit.UnitObject));
            }
        }
    }

    private IEnumerator ResetCellColorAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        ResetColor(); // <-- а не rend.material.color = originalColor;
    }

    public void ResetColor()
    {
        if (isReachable)
            rend.material.color = reachableColor; // Синяя если достижима
        else
            rend.material.color = defaultColor;   // Обычный цвет
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
        if (activeUC == null)
        {
            Debug.LogWarning("activeUC == null");
            return;
        }
        Unit myUnit = activeUC.unitData;

        Unit activeUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (myUnit != activeUnit)
        {
            Debug.LogWarning($"myUnit != activeUnit: myUnit={myUnit.UnitName}, activeUnit={activeUnit.UnitName}");
            return;
        }

        int distance = Mathf.Max(
            Mathf.Abs(myUnit.CurrentCell.x - target.CurrentCell.x),
            Mathf.Abs(myUnit.CurrentCell.y - target.CurrentCell.y),
            Mathf.Abs(myUnit.CurrentCell.z - target.CurrentCell.z)
        );
        Debug.Log($"Атака: distance={distance}, attackRange={myUnit.attackRange}");

        if (distance <= myUnit.attackRange)
        {
            Debug.Log($"Атака по врагу: {target.UnitName}");
            myUnit.Attack(target);
            Debug.Log("Прямая атака!");
            return;
        }
        else
        {
            Debug.Log($"Враг вне досягаемости. distance={distance}, attackRange={myUnit.attackRange}");
        }
    }

    int GetDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z); // Манхэттен
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