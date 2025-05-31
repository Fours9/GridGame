using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TeamType { Player, Enemy }

public class UnitController : MonoBehaviour
{
    public TeamType team;
    public int health = 100;
    public int damage = 20;
    public Vector3Int CurrentCell;
    private UnitMover unit;

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} погиб");
        Destroy(gameObject);
        // Позже: уведомление TurnManager'у
    }

    public bool isPlayerControlled = true; // Можно использовать и для AI
    public bool isSelected = false;

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public void Select()
    {
        isSelected = true;
        rend.material.color = Color.green;
        gameObject.tag = "Select"; // 👉 Устанавливаем тег

        UnitMover mover = GetComponent<UnitMover>();
        mover.SetAsActiveUnit();
    }

    public void Deselect()
    {
        isSelected = false;
        rend.material.color = Color.white;
        gameObject.tag = "Untagged"; // 👉 Сбрасываем тег
    }
}