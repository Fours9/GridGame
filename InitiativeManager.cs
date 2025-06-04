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
        // ��� ���� ���������� � ��� ������ ���
        turnOrder = unitSpawner.unitData
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Initiative)
            .ToList();
        currentUnitIndex = 0;
    }

    public Unit GetCurrentUnit() => turnOrder.Count > 0 ? turnOrder[currentUnitIndex] : null;

    public void EndCurrentTurn()
    {
        // �������� ���� ��������
        var unit = GetCurrentUnit();
        if (unit != null) unit.stepsUsed = 0;

        // ��������� � ���������� ������ �����
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
        // ����������, �������� � UI, ������ ���������� ������ ��� AI
        var currentUnit = GetCurrentUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("��� ��������� ����� ��� ����! ��� ��������� ������...");
            return;
        }

        Debug.Log($"����� ����: {currentUnit.UnitObject.name} (Initiative {currentUnit.Initiative})");

        // ���� AI � ��������� �� ���
        if (currentUnit.team == TeamType.Enemy)
        {
            StartCoroutine(EnemyAITurn(currentUnit));
        }
        // ���� ����� � ��������� ����������
        else
        {
            // UI: �������� �����, �������� ������, ��������� �������� � �����
            // ����� ����� ���������� ��������� �����
        }
    }

    System.Collections.IEnumerator EnemyAITurn(Unit enemyUnit)
    {
        // ����� ���� AI � ����� ������ ��� ��� �����
        yield return new WaitForSeconds(0.5f);

        // ��� ������ ���� ������ ��������/����� ����� � ��� ������ �������
        EndCurrentTurn();
    }
}