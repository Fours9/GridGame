using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Main;

public enum TeamType
{
    Player,
    Enemy,
    Neutral
}

public class Unit
{
    public TeamType team;
    public int health = 100;
    public int damage = 20;
    public int Initiative = 10;
    public Vector3Int CurrentCell;
    public int id; // Уникальный идентификатор юнита, может использоваться для поиска или управления
    public GameObject UnitObject { get; private set; }

    public MoveCell undercell; // Тип клетки, определяемый в основном скрипте

    public bool isPlayerControlled = true; // Можно использовать и для AI
    public bool IsSelected = false; // Состояние выбора юнита

    public Unit(TeamType team, GameObject unitObject, bool isSelected, MoveCell underCell, int id)  // Конструктор для инициализации клетки
    {
        UnitObject = unitObject;  // Ссылка на визуальный объект юнита
        IsSelected = isSelected;  // Установка состояния выбора юнита
        undercell = underCell; // Установка типа клетки
    }
}