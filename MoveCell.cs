using UnityEngine;

// Скрипт описывает логику клеток, по которым можно перемещаться в 3D пространстве (для A* или других алгоритмов).
// Каждая клетка имеет координаты, ссылку на визуальный объект и свойства для определения проходимости.
// MoveCell представляет собой логическую единицу, по которой можно перемещаться в 3D пространстве.
// Она содержит координаты, ссылку на визуальный объект и свойства для определения проходимости и стоимости пути.
public class MoveCell
{
    public Vector3Int Position { get; private set; }      // Координаты клетки
    public GameObject CellObject { get; private set; }    // Ссылка на визуальный объект клетки
    public bool IsWalkable { get; set; }                  // Можно ли по этой клетке ходить
    public float MoveCost { get; set; }                   // Стоимость перемещения по этой клетке (может быть разной для разных типов клеток)

    // Параметры для алгоритма A*:
    public float GCost;                                   // Стоимость пути от начальной клетки до этой
    public float HCost;                                   // Оценка оставшегося расстояния до цели
    public float FCost => GCost + HCost;                  // Суммарная стоимость
    public MoveCell Parent;                               // Ссылка на предыдущую клетку (для восстановления пути)

    // Конструктор и инициализация
    public MoveCell(int x, int y, int z, GameObject cellObject, bool isWalkable, float moveCost)  // Конструктор для инициализации клетки
    {
        Position = new Vector3Int(x, y, z); // Инициализация координат клетки
        CellObject = cellObject;  // Ссылка на визуальный объект клетки
        IsWalkable = isWalkable;  // Установка проходимости клетки
        MoveCost = moveCost; // Установка стоимости перемещения по клетке
    }
}