using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// ������ ��������� ������ ������, �� ������� ����� ������������ � 3D ������������ (��� A* ��� ������ ����������).
// ������ ������ ����� ����������, ������ �� ���������� ������ � �������� ��� ����������� ������������.
// MoveCell ������������ ����� ���������� �������, �� ������� ����� ������������ � 3D ������������.
// ��� �������� ����������, ������ �� ���������� ������ � �������� ��� ����������� ������������ � ��������� ����.
public class MoveCell
{
    public Vector3Int Position { get; private set; }      // ���������� ������
    public GameObject CellObject { get; private set; }    // ������ �� ���������� ������ ������
    public bool IsWalkable { get; set; }                  // ����� �� �� ���� ������ ������
    public float MoveCost { get; set; }                   // ��������� ����������� �� ���� ������ (����� ���� ������ ��� ������ ����� ������)

    // ��������� ��� ��������� A*:
    public float GCost;                                   // ��������� ���� �� ��������� ������ �� ����
    public float HCost;                                   // ������ ����������� ���������� �� ����
    public float FCost => GCost + HCost;                  // ��������� ���������
    public MoveCell Parent;                               // ������ �� ���������� ������ (��� �������������� ����)

    // ����������� � �������������
    public MoveCell(int x, int y, int z, GameObject cellObject, bool isWalkable, float moveCost)  // ����������� ��� ������������� ������
    {
        Position = new Vector3Int(x, y, z); // ������������� ��������� ������
        CellObject = cellObject;  // ������ �� ���������� ������ ������
        IsWalkable = isWalkable;  // ��������� ������������ ������
        MoveCost = moveCost; // ��������� ��������� ����������� �� ������
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