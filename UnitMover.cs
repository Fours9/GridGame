using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System; // обязательно для Environment


public class UnitMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 targetPosition;
    private bool isMoving = false;

    private GameObject player;
    public float delayBetweenSteps = 0.1f;

    private Coroutine moveCoroutine;
    private bool canInterrupt = false;

    Main main;

    private bool _shouldStop = false;
    private bool shouldStop
    {
        get { return _shouldStop; }
        set
        {
            Debug.Log($"shouldStop меняется на {value} в {gameObject.name}, Stack: {Environment.StackTrace}");
            _shouldStop = value;
        }
    }

    private MoveCell currentCell;
    UnitSpawner unitSpawner;

    void Update()
    {
        if (!IsActiveUnit()) return;

        if (Input.GetMouseButtonDown(1))
            Debug.LogWarning("Input.GetMouseButtonDown(1) СРАБОТАЛ в кадре!", this);

        if (Input.GetMouseButtonDown(1) && isMoving && canInterrupt)
        {
            StopMoving();
            Debug.Log("ПКМ: Останавливаем движение.");
        }
    }


    public void StopMoving()
    {
        shouldStop = true;
        Debug.LogWarning("StopMoving вызван! StackTrace:\n" + Environment.StackTrace, this);
    }


    void Awake()
    {
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
        main = FindAnyObjectByType<Main>(); // Без типа Main в начале!
    }

    public void SetAsActiveUnit()
    {
        //player = GameObject.FindWithTag("Select");

        foreach (Unit unit in unitSpawner.unitData)
        {
            if (unit.IsSelected == true)
            {
                player = unit.UnitObject; // находим игрока по тегу "Select"
            }
        }
    }

    public bool IsActiveUnit()
    {
        return player == this.gameObject;
    }

    public void StartMoving(List<MoveCell> moveCells)
    {
        SetAsActiveUnit();

        // Получаем доступ к Unit (например, unitData)
        UnitController uc = player.GetComponent<UnitController>();
        if (uc == null) return;
        Unit unitData = uc.unitData;

        if (unitData.RemainingMovement <= 0)
        {
            Debug.Log("У юнита закончились очки движения!");
            return; // Блокируем движение
        }

        if (isMoving)
        {
            Debug.Log("StartMoving: Уже движется, не стартуем новую корутину!");
            return;
        }

        canInterrupt = false;
        StartCoroutine(StartInterruptDelay());

        if (!Global.Instance.isDone)
        {
            Debug.Log("Другой юнит уже двигается. Жди.");
            return;
        }

        Global.Instance.isDone = false;

        Debug.Log($"isMoving={isMoving}, Global.Instance.isDone={Global.Instance.isDone}");

        moveCoroutine = StartCoroutine(MoveThroughCells(moveCells));
    }

    private IEnumerator StartInterruptDelay()
    {
        yield return new WaitForSeconds(0.2f); // 0.2 секунды после старта — разрешаем прерывать
        canInterrupt = true;
    }

    IEnumerator MoveThroughCells(List<MoveCell> moveCells)
    {
        Debug.Log("=== Корутина Start ===");

        if (moveCells == null || moveCells.Count == 0)
        {
            Debug.LogWarning("Нет клеток или юнит не найден.");
            yield break;
        }

        if (player == null)
        {
            Debug.LogError("player is null в UnitMover! Проверь выбор юнита и SetAsActiveUnit().");
            yield break;
        }

        UnitController uc = player.GetComponent<UnitController>();
        if (uc == null)
        {
            Debug.LogError("UnitController не найден на выбранном player!");
            yield break;
        }

        Unit unitData = uc.unitData;
        if (unitData == null)
        {
            Debug.LogError("unitData в UnitController не инициализирован!");
            yield break;
        }

        isMoving = true;
        shouldStop = false;

        // --------- 1. Освобождаем стартовую клетку (если надо) -----------
        // Найди стартовую клетку, если нужно её освободить
        MoveCell prevCell = null;
        if (moveCells.Count > 0)
            prevCell = moveCells[0];
        if (prevCell != null)
            prevCell.SetOccupied(null);

        // ---------------------------------------------------------------

        if (Vector3.Distance(player.transform.position, moveCells[0].Position) > 0.01f)
            player.transform.position = moveCells[0].Position;

        Debug.Log($"moveCells.Count = {moveCells.Count}, путь:");

        Debug.Log($"player.transform.position: {player.transform.position}, moveCells[0]: {moveCells[0].Position}, moveCells[1]: {moveCells[1].Position}");
        Debug.Log($"distance to moveCells[1]: {Vector3.Distance(player.transform.position, moveCells[1].Position)}");

        if (moveCells.Exists(cell => cell == null))
        {
            Debug.LogError("В маршруте есть null-клетки! Проверь Pathfinding и CellData.");
        }

        moveCells = moveCells.FindAll(cell => cell != null);

        int maxSteps = unitData.movementPoints; // Ограничение на количество клеток
        int stepsDone = 0;

        for (int i = 1; i < moveCells.Count && stepsDone < maxSteps; i++)
        {

            MoveCell nextCell = moveCells[i];

            // Проверяем лимит движения
            if (stepsDone >= maxSteps)
            {
                Debug.Log("Достигнут лимит movementPoints! Останавливаем движение.");
                break;
            }

            currentCell = moveCells[i];
            if (currentCell == null)
            {
                Debug.LogError($"moveCells[{i}] == null. Прерываю движение!");
                isMoving = false;
                shouldStop = false;
                Global.Instance.isDone = true;
                break;
            }

            // ----- ВОТ ЗДЕСЬ -----
            // Обновляем координаты, уменьшаем очки движения ПЕРЕД анимацией
            unitData.CurrentCell = nextCell.Position;
            unitData.stepsUsed++; // или уменьшай RemainingMovement

            // --- Обновить синюю зону СРАЗУ ---
            UpdateReachableCellsAfterMove(unitData, main);

            bool stopAfterThisCell = false;

            while (Vector3.Distance(player.transform.position, currentCell.Position) > 0.01f)
            {
                if (shouldStop)
                    stopAfterThisCell = true;

                Vector3 dir = currentCell.Position - player.transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    player.transform.rotation = Quaternion.LookRotation(dir);

                player.transform.position = Vector3.MoveTowards(
                    player.transform.position,
                    currentCell.Position,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            player.transform.position = currentCell.Position;
            yield return new WaitForSeconds(delayBetweenSteps);

            nextCell.SetOccupied(player);

            if (prevCell != null && prevCell != nextCell)
                prevCell.SetOccupied(null);

            prevCell = nextCell;

            if (stopAfterThisCell)
            {
                Debug.Log("Остановились ровно на клетке!");
                isMoving = false;
                shouldStop = false;
                Global.Instance.isDone = true;
                break;
            }
        }

        isMoving = false;
        shouldStop = false;
        Global.Instance.isDone = true;

        // После завершения движения:
        UpdateReachableCellsAfterMove(unitData, main);

        Debug.Log("=== Корутина End === (флаги сброшены)");
    }

    private void UpdateReachableCellsAfterMove(Unit unitData, Main main)
    {
        if (main == null)
            main = FindAnyObjectByType<Main>();
        if (unitData == null) return;

        // Сброс всей подсветки
        foreach (var cell in main.CellData)
            if (cell != null && cell.CellObject != null)
                cell.CellObject.GetComponent<GridCellBehaviour>()?.SetReachableHighlight(false);

        // Теперь выделяем новые клетки по оставшимся очкам движения
        int left = unitData.RemainingMovement;
        if (left <= 0) return;

        MoveCell curCell = main.CellData[unitData.CurrentCell.x, unitData.CurrentCell.y, unitData.CurrentCell.z];
        var reachable = MovementHelper.GetReachableCells(curCell, main.CellData, left);

        foreach (var cell in reachable)
            if (cell != null && cell.CellObject != null)
                cell.CellObject.GetComponent<GridCellBehaviour>()?.SetReachableHighlight(true);
    }


    public void MoveTo(Vector3 newPosition)  // метод для установки новой цели движения
    {
        targetPosition = newPosition;  // установка новой позиции цели
        isMoving = true;  // установка флага движения в true, чтобы начать движение
    }
}
