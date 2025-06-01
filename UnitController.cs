using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class UnitController : MonoBehaviour
{
    private Unit unitData;

    public void TakeDamage(int amount)
    {
        unitData.health -= amount;
        Debug.Log($"{gameObject.name} получил урон. Осталось здоровья: {unitData.health}");

        if (unitData.health <= 0)
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

    public bool GetisSelect()
    {
        return isSelected;
    }
}