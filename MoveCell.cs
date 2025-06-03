using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Main;

// Скрипт описывает логику клеток, по которым можно перемещаться в 3D пространстве (для A* или других алгоритмов).
// Каждая клетка имеет координаты, ссылку на визуальный объект и свойства для определения проходимости.
// MoveCell представляет собой логическую единицу, по которой можно перемещаться в 3D пространстве.
// Она содержит координаты, ссылку на визуальный объект и свойства для определения проходимости и стоимости пути.
public class MoveCell
{
    public Vector3Int Position { get; private set; }      // Координаты клетки
    public GameObject CellObject { get; set; }    // Ссылка на визуальный объект клетки
    public bool IsWalkable { get; set; }                  // Можно ли по этой клетке ходить
    public float MoveCost { get; set; }                   // Стоимость перемещения по этой клетке (может быть разной для разных типов клеток)

    public Main.CellType undertype; // Тип клетки, определяемый в основном скрипте
    public GameObject OccupyingUnit { get; set; }

    // Параметры для алгоритма A*:
    public float GCost;                                   // Стоимость пути от начальной клетки до этой
    public float HCost;                                   // Оценка оставшегося расстояния до цели
    public float FCost => GCost + HCost;                  // Суммарная стоимость
    public MoveCell Parent;                               // Ссылка на предыдущую клетку (для восстановления пути)


    // Конструктор и инициализация
    public MoveCell(int x, int y, int z, GameObject cellObject, bool isWalkable, float moveCost, CellType undertype, GameObject OccupyingUnit)  // Конструктор для инициализации клетки
    {
        Position = new Vector3Int(x, y, z); // Инициализация координат клетки
        CellObject = cellObject;  // Ссылка на визуальный объект клетки
        IsWalkable = isWalkable;  // Установка проходимости клетки
        MoveCost = moveCost; // Установка стоимости перемещения по клетке
        this.undertype = undertype; // Установка типа клетки
        this.OccupyingUnit = OccupyingUnit; // Установка занимающего объекта (если есть)
    }



    public void SetOccupied(GameObject unit)
    {
        OccupyingUnit = unit;
        IsWalkable = (unit == null);

        if (unit != null)
        {
            ApplyEffectsTo(unit);
        }
    }

    public void Highlight(Color color)
    {
        if (CellObject != null)
        {
            Renderer renderer = CellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }

    public void ClearHighlight()
    {
        if (CellObject != null)
        {
            var beh = CellObject.GetComponent<GridCellBehaviour>();
            if (beh != null)
                beh.ResetColor(); // <- этот метод уже есть!
        }
    }

    [SerializeField]
    private List<MonoBehaviour> rawEffects = new List<MonoBehaviour>(); // В инспекторе сюда добавляем эффекты

    private List<ICellEffect> cellEffects = new List<ICellEffect>();

    void Awake()
    {
        foreach (var effect in rawEffects)
        {
            if (effect is ICellEffect cellEffect)
                cellEffects.Add(cellEffect);
        }
    }

    private void ApplyEffectsTo(GameObject unit)
    {
        foreach (var effect in cellEffects)
        {
            effect.ApplyEffect(unit);
        }
    }
}

public class Global
{
    private static Global _instance;

    public static Global Instance
    {
        get
        {
            if (_instance == null)
                _instance = new Global();
            return _instance;
        }
    }

    // Переменная, доступная всем
    public bool isDone = true;

    // Закрытый конструктор, чтобы нельзя было создать ещё экземпляр
    private Global() { }
}