using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class UnitController : MonoBehaviour
{

    public GameObject DeadPrefabPlayer;
    public GameObject DeadPrefabEnemy;

    private Coroutine damageRoutine;

    public Unit unitData;
    private Renderer rend;
    private Color originalColor;
    private Color defaultColor;
    private bool isHighlighted = false;

    UnitSpawner unitSpawner;

    public Vector3Int CurrentCell;

    public bool isSelected = false;

    private bool _isPlayerControlled;
    public bool isPlayerControlled
    {
        get => _isPlayerControlled;
        set => _isPlayerControlled = value;
    }

    private void Start()
    {
        rend = GetComponent<Renderer>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
        originalColor = rend.material.color;

        Debug.Log($"{gameObject.name}: isPlayerControlled при старте = {isPlayerControlled}");
        if (rend != null)
            defaultColor = rend.material.color;
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

    public void ShowDamageFeedback()
    {
        if (damageRoutine != null) StopCoroutine(damageRoutine);
        damageRoutine = StartCoroutine(DamageColorRoutine());
    }

    IEnumerator DamageColorRoutine()
    {
        rend.material.color = Color.magenta;
        yield return new WaitForSeconds(1f);
        rend.material.color = originalColor;
    }

    public void SpawnCorpse()
    {
        Vector3 pos = transform.position;
        GameObject corpse = null;
        if (unitData.team == TeamType.Player && DeadPrefabPlayer != null)
            corpse = Instantiate(DeadPrefabPlayer, pos, Quaternion.identity);
        else if (unitData.team == TeamType.Enemy && DeadPrefabEnemy != null)
            corpse = Instantiate(DeadPrefabEnemy, pos, Quaternion.identity);
        Destroy(gameObject);
    }



    void OnMouseEnter()
    {
        // Подсвечиваем врага, если у игрока выбран свой юнит и он ходит
        var selectedUC = UnitSelectionManager.Instance.GetSelectedUnit();
        var activeUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (unitData != null && selectedUC != null &&
            selectedUC.unitData.team != unitData.team && activeUnit == selectedUC.unitData)
        {
            Highlight(Color.Lerp(Color.red, Color.yellow, 0.5f)); // Оранжевый
            isHighlighted = true;
        }
    }

    void OnMouseExit()
    {
        if (isHighlighted)
        {
            Highlight(defaultColor);
            isHighlighted = false;
        }
    }

    void OnMouseDown()
    {

    }

    public void Highlight(Color col)
    {
        if (rend != null)
            rend.material.color = col;
    }
}