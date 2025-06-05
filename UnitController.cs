using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class UnitController : MonoBehaviour
{
    public Unit unitData;

    UnitSpawner unitSpawner;

    public Vector3Int CurrentCell;

    public bool isSelected = false;

    private bool _isPlayerControlled;
    public bool isPlayerControlled
    {
        get => _isPlayerControlled;
        set => _isPlayerControlled = value;
    }

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


    private Renderer rend;
    private Color originalColor;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
        originalColor = rend.material.color;

        Debug.Log($"{gameObject.name}: isPlayerControlled при старте = {isPlayerControlled}");
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

    public void HighlightAsEnemy()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        // Жёлтый цвет (можешь подобрать свой оттенок)
        rend.material.color = Color.yellow;
    }
}