using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Main;

// ������ ��������� ������ ������, �� ������� ����� ������������ � 3D ������������ (��� A* ��� ������ ����������).
// ������ ������ ����� ����������, ������ �� ���������� ������ � �������� ��� ����������� ������������.
// MoveCell ������������ ����� ���������� �������, �� ������� ����� ������������ � 3D ������������.
// ��� �������� ����������, ������ �� ���������� ������ � �������� ��� ����������� ������������ � ��������� ����.
public class MoveCell
{
    public Vector3Int Position { get; private set; }      // ���������� ������
    public GameObject CellObject { get; set; }    // ������ �� ���������� ������ ������
    public bool IsWalkable { get; set; }                  // ����� �� �� ���� ������ ������
    public float MoveCost { get; set; }                   // ��������� ����������� �� ���� ������ (����� ���� ������ ��� ������ ����� ������)

    public Main.CellType undertype; // ��� ������, ������������ � �������� �������
    public GameObject OccupyingUnit { get; set; }

    // ��������� ��� ��������� A*:
    public float GCost;                                   // ��������� ���� �� ��������� ������ �� ����
    public float HCost;                                   // ������ ����������� ���������� �� ����
    public float FCost => GCost + HCost;                  // ��������� ���������
    public MoveCell Parent;                               // ������ �� ���������� ������ (��� �������������� ����)


    // ����������� � �������������
    public MoveCell(int x, int y, int z, GameObject cellObject, bool isWalkable, float moveCost, CellType undertype, GameObject OccupyingUnit)  // ����������� ��� ������������� ������
    {
        Position = new Vector3Int(x, y, z); // ������������� ��������� ������
        CellObject = cellObject;  // ������ �� ���������� ������ ������
        IsWalkable = isWalkable;  // ��������� ������������ ������
        MoveCost = moveCost; // ��������� ��������� ����������� �� ������
        this.undertype = undertype; // ��������� ���� ������
        this.OccupyingUnit = OccupyingUnit; // ��������� ����������� ������� (���� ����)
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
                beh.ResetColor(); // <- ���� ����� ��� ����!
        }
    }

    [SerializeField]
    private List<MonoBehaviour> rawEffects = new List<MonoBehaviour>(); // � ���������� ���� ��������� �������

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

    // ����������, ��������� ����
    public bool isDone = true;

    // �������� �����������, ����� ������ ���� ������� ��� ���������
    private Global() { }
}