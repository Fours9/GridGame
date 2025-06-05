using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UIElements;
using static Main;
using static UnityEditor.Progress;

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
    bool canAct; // Флаг, определяющий, может ли юнит действовать в текущем ходе
    public int attackRange = 1; // Дальность атаки юнита, определяет, на каком расстоянии он может атаковать противника
    bool isAlive; // Флаг, определяющий, жив ли юнит
    int networkId; // Идентификатор юнита в сетевой игре, может использоваться для синхронизации состояния между игроками
    bool isLocalPlayer; // Флаг, определяющий, управляет ли юнит локальный игрок или это AI/другой игрок

    public int movementPoints = 5;  // Количество очков движения, которые юнит может использовать за ход
    public int stepsUsed = 0;  // Количество шагов, использованных юнитом в текущем ходе

    public int maxActionPoints = 1; // Максимум очков действия за ход (например, можно расширить для других юнитов)
    public int actionPoints = 1;    // Текущие очки действия

    public bool canMove => RemainingMovement > 0; // Автоматически вычисляется

    public int RemainingMovement => movementPoints - stepsUsed;  // Свойство для получения оставшихся очков движения

    List<Item> Inventory; // Список предметов, которые юнит может использовать или носить с собой   

    public GameObject UnitObject { get; set; }

    public MoveCell undercell; // Тип клетки, определяемый в основном скрипте

    public bool isPlayerControlled = true; // Можно использовать и для AI
    public bool IsSelected = false; // Состояние выбора юнита

    public string UnitName
    {
        get
        {
            return UnitObject != null ? UnitObject.name : "Unnamed";
        }
    }

    public Unit(int x, int y, int z, TeamType team, GameObject unitObject, bool isSelected, MoveCell underCell, int id)  // Конструктор для инициализации клетки
    {
        CurrentCell = new Vector3Int(x, y, z); // Инициализация координат клетки
        UnitObject = unitObject;  // Ссылка на визуальный объект юнита
        IsSelected = isSelected;  // Установка состояния выбора юнита
        undercell = underCell; // Установка типа клетки
        this.team = team;
    }

    public bool IsAlive => health > 0;

    public void Attack(Unit target)
    {
        if (!IsAlive) return;
        if (target == null || !target.IsAlive) return;

        // --- Новое: проверка actionPoints ---
        if (actionPoints <= 0)
        {
            Debug.Log($"{UnitName}: Нет очков действия для атаки!");
            return;
        }

        // Списываем actionPoints за атаку
        actionPoints--;

        // Урон
        target.TakeDamage(this.damage);

        // Можно добавить вызов анимации атаки или эффекта
        Debug.Log($"{UnitName} атакует {target.UnitName}! Осталось actionPoints: {actionPoints}");
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (UnitObject != null)
        {
            var ctrl = UnitObject.GetComponent<UnitController>();
            if (ctrl != null)
                ctrl.ShowDamageFeedback(); // визуальная отдача
        }
        if (health <= 0)
        {
            health = 0;
            isAlive = false;
            Die();
        }
    }

    public void Die()
    {
        // Клетка становится проходимой
        if (CurrentCell != null)
        {
            var main = GameObject.FindFirstObjectByType<Main>();
            if (main != null)
            {
                var cell = main.CellData[CurrentCell.x, CurrentCell.y, CurrentCell.z];
                if (cell != null) cell.SetOccupied(null);
            }
        }

        // Спавним труп, если нужно
        if (UnitObject != null)
        {
            var ctrl = UnitObject.GetComponent<UnitController>();
            if (ctrl != null)
            {
                ctrl.SpawnCorpse(); // отдельный метод — см. ниже
            }
        }
    }
}