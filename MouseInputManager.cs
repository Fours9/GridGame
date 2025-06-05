using System.Collections.Generic;
using UnityEngine;

public class MouseInputManager : MonoBehaviour
{
    public UnitMover unitMover; // Подключи через инспектор
    public Main main;           // Ссылка на твой генератор карты

    // Можно добавить ссылки на менеджеры, если надо
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
                    Debug.Log("[ATTACK] Наведен на: " + unitCtrl.unitData.UnitName);
                    TryMoveAndAttack(unitCtrl.unitData);
                    return;
                }

                var cell = hit.collider.GetComponent<GridCellBehaviour>();
                if (cell != null)
                {
                    Debug.Log("[MOVE] Наведен на клетку: " + cell.name);
                    // ... твоя логика перемещения по клетке
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
            Debug.Log($"[ATTACK] Атакуем {target.UnitName}!");
            myUnit.Attack(target);
        }
        else
        {
            // --- Новое: строим путь к соседней клетке ---
            MoveCell targetCell = GetNearestFreeAdjacentCell(target.CurrentCell);
            if (targetCell == null)
            {
                Debug.Log("[MOVE] Нет свободных клеток рядом с врагом!");
                return;
            }

            // Если Pathfinding висит на том же объекте, что и Main:
            var pathfinding = main.GetComponent<Pathfinding>();
            // Если отдельный объект — подключи его в инспекторе!
            var path = pathfinding.FindPath(myUnit.CurrentCell, targetCell.Position, main.CellData);

            if (path == null || path.Count == 0)
            {
                Debug.Log("[MOVE] Путь к врагу не найден!");
                return;
            }

            // Двигаемся! Когда закончили — пробуем атаку (если подошёл)
            StartCoroutine(MoveAndAttackCoroutine(path, myUnit, target));
        }
    }

    // Получить ближайшую свободную клетку вокруг врага
    MoveCell GetNearestFreeAdjacentCell(Vector3Int enemyCell)
    {
        Vector3Int[] dirs = new Vector3Int[] {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    };

        MoveCell result = null;
        float minDist = float.MaxValue;

        if (main == null) { Debug.LogError("main == null в MouseInputManager!"); return null; }
        if (main.CellData == null) { Debug.LogError("main.CellData == null!"); return null; }

        Unit currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (currentUnit == null) { Debug.LogError("currentUnit == null!"); return null; }

        Vector3Int myCell = currentUnit.CurrentCell;

        foreach (var dir in dirs)
        {
            Vector3Int pos = enemyCell + dir;
            if (!main.IsCellInBounds(pos)) continue;
            MoveCell cell = main.CellData[pos.x, pos.y, pos.z];
            if (cell == null || cell.unitOnCell != null) continue; // Занято

            float dist = Vector3Int.Distance(myCell, pos);
            if (dist < minDist)
            {
                minDist = dist;
                result = cell;
            }
        }
        if (result == null) Debug.LogWarning("Нет свободных клеток рядом с врагом!");
        return result;
    }

    // Корутина: идём -> атакуем если можем
    System.Collections.IEnumerator MoveAndAttackCoroutine(List<MoveCell> path, Unit myUnit, Unit target)
    {
        // Берём UnitMover активного юнита
        var mover = myUnit.UnitObject.GetComponent<UnitMover>();
        if (mover == null)
        {
            Debug.LogError("У выбранного юнита нет компонента UnitMover!");
            yield break;
        }

        Debug.Log($"[MoveAndAttack] Старт движения для {myUnit.UnitName}");
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
                Debug.Log($"[ATTACK] Юнит атакует, actionPoints теперь: {myUnit.actionPoints}");
            }
            else
            {
                Debug.Log("[ATTACK] Нет actionPoints, атака невозможна!");
            }
        }
        else
        {
            Debug.Log("[ATTACK] Подошёл, но не хватает хода для удара!");
        }
    }
}