using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public List<GameObject> allUnits = new List<GameObject>();

    private Queue<GameObject> turnQueue;

    void Start()
    {
        GenerateTurnOrder();
    }

    void GenerateTurnOrder()
    {
        var sorted = allUnits
            .OrderByDescending(unit => unit.GetComponent<UnitController>().Initiative)
            .ToList();

        turnQueue = new Queue<GameObject>(sorted);

        Debug.Log("Очередность хода:");
        foreach (var unit in turnQueue)
        {
            Debug.Log(unit.name + " (Инициатива: " + unit.GetComponent<UnitController>().Initiative + ")");
        }
    }

    public GameObject GetNextUnit()
    {
        var unit = turnQueue.Dequeue();
        turnQueue.Enqueue(unit); // цикл
        return unit;
    }
}