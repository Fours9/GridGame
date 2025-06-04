using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InitiativeManager : MonoBehaviour
{
    public static InitiativeManager Instance { get; private set; }

    public List<Unit> turnOrder = new List<Unit>();
    public int currentUnitIndex = 0;
    UnitSpawner unitSpawner;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
    }

    void Start()
    {

    }

    public void BuildInitiativeQueue()
    {
        // Чем выше инициатива — тем раньше ход
        turnOrder = unitSpawner.unitData
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Initiative)
            .ToList();
        currentUnitIndex = 0;
    }

    public Unit GetCurrentUnit() => turnOrder.Count > 0 ? turnOrder[currentUnitIndex] : null;

    public void EndCurrentTurn()
    {
        // Сбросить очки движения
        var unit = GetCurrentUnit();
        if (unit != null) unit.stepsUsed = 0;

        // Переходим к следующему живому юниту
        int startIdx = currentUnitIndex;
        do
        {
            currentUnitIndex = (currentUnitIndex + 1) % turnOrder.Count;
            if (turnOrder[currentUnitIndex].IsAlive)
                break;
        } while (currentUnitIndex != startIdx);

        StartTurn();
    }

    public void StartTurn()
    {
        // Подсветить, показать в UI, выдать управление игроку или AI
        var currentUnit = GetCurrentUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("Нет активного юнита для хода! Жду появления юнитов...");
            return;
        }

        Debug.Log($"Ходит юнит: {currentUnit.UnitObject.name} (Initiative {currentUnit.Initiative})");

        // Если AI — запускаем ИИ ход
        if (currentUnit.team == TeamType.Enemy)
        {
            StartCoroutine(EnemyAITurn(currentUnit));
        }
        // Если игрок — разрешаем управление
        else
        {
            // UI: выделить юнита, включить кнопки, разрешить движение и атаки
            // Здесь можно подсветить активного юнита
        }
    }

    System.Collections.IEnumerator EnemyAITurn(Unit enemyUnit)
    {
        // Здесь твой AI — пусть просто ждёт для теста
        yield return new WaitForSeconds(0.5f);

        // Тут должна быть логика движения/атаки врага — или просто пропуск
        EndCurrentTurn();
    }
}