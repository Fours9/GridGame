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

public class Unit : MonoBehaviour
{
    public TeamType team;
    public int health = 100;
    public int damage = 20;
    public int Initiative = 10;
    public Vector3Int CurrentCell;
    public GameObject UnitObject { get; private set; }

    public MoveCell undertype; // Тип клетки, определяемый в основном скрипте

    public bool isPlayerControlled = true; // Можно использовать и для AI
    public bool IsSelected { get; set; }

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public Unit(GameObject unitObject, bool isSelected, MoveCell underCell)  // Конструктор для инициализации клетки
    {
        UnitObject = unitObject;  // Ссылка на визуальный объект юнита
        IsSelected = isSelected;  // Установка состояния выбора юнита
        undertype = underCell; // Установка типа клетки
    }
}