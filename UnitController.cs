using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class UnitController : MonoBehaviour
{
    private Unit unitData;

    UnitSpawner unitSpawner;

    public Vector3Int CurrentCell;
    public TeamType team;

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
    private Color originalColor;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
        originalColor = rend.material.color;
    }

    public void Select()
    {
        rend.material.color = Color.green;
    }

    public void Deselect()
    {
        rend.material.color = originalColor;
    }

    public bool GetisSelect()
    {
        return isSelected;
    }
}