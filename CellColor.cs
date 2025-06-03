using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellBehaviour : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isRightMouseHeld = false;

    private Main main;
    private Vector3Int startCoords;
    UnitSpawner unitSpawner;
    private GameObject unit;

    public bool isReachable = false; // ������ ���������?
    private Color reachableColor = Color.blue;
    private Color hoverColor = Color.yellow;
    private Color clickColor = Color.green;

    public MoveCell[,,] CellData => main?.CellData; 

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;

        main = FindAnyObjectByType<Main>();
        unitSpawner = FindAnyObjectByType<UnitSpawner>();
    }

    void OnMouseEnter()
    {
        isHovered = true; // <-- ��������!
        rend.material.color = hoverColor; // ������ ������ ��� ���������
    }

    void OnMouseExit()
    {
        isHovered = false; // <-- ��������!
        ResetColor(); // ������� �������� (��� ����� ��� reachable)
    }

    void OnMouseUp()
    {
        if (isReachable)
            rend.material.color = reachableColor;
        else
            ResetColor();
    }


    public void SetReachableHighlight(bool enable)
    {
        isReachable = enable;
        ResetColor(); // ������ ��������� ���� ���������
    }

    void Update()
    {
        // 1. ������ ��� ��� ����� ������� � ���������� ������
        if (isHovered && Input.GetMouseButtonDown(1))
        {
            isRightMouseHeld = true;
            rend.material.color = clickColor;
        }

        // 2. ���� ������ � ����� ������� ������ (����� �� �����������)
        if (isHovered && isRightMouseHeld && Input.GetMouseButton(1))
        {
            rend.material.color = clickColor;
        }

        // 3. ��������� � ���� ������ ��� �������, ��������� ��������
        if (isHovered && isRightMouseHeld && Input.GetMouseButtonUp(1))
        {
            isRightMouseHeld = false;
            ResetColor(); // ������� ��������/�����

            TryMoveUnitHere(); // �������� ������, ���� ������ walkable
        }

        // 4. ���� ���� � ������ ������ � �������� ������
        if (!isHovered && isRightMouseHeld)
        {
            isRightMouseHeld = false;
            ResetColor();
        }
    }

    private void TryMoveUnitHere()
    {
        foreach (Unit unitSelect in unitSpawner.unitData)
        {
            if (unitSelect.IsSelected == true)
                unit = unitSelect.UnitObject;

            if (unit != null)
            {
                Vector3 playerPos = unit.transform.position;
                startCoords = Vector3Int.RoundToInt(playerPos);

                Vector3Int targetCoords = Vector3Int.RoundToInt(transform.position);

                if (main.CellData[targetCoords.x, targetCoords.y, targetCoords.z].IsWalkable)
                    Debug.Log($"Start: {startCoords}, Target: {targetCoords}");

                var pathfinder = FindAnyObjectByType<Pathfinding>();
                if (pathfinder != null)
                {
                    List<MoveCell> path = pathfinder.FindPath(startCoords, targetCoords, CellData);

                    if (path == null || path.Count == 0)
                    {
                        Debug.LogWarning("Pathfinding returned no valid path.");
                        return;
                    }

                    UnitMover move = unit.GetComponent<UnitMover>();
                    if (move != null)
                    {
                        path = path.FindAll(cell => cell != null);
                        if (path.Count == 0) return;
                        move.StartMoving(path);
                    }
                }
            }
        }
    }

    private IEnumerator ResetCellColorAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        ResetColor(); // <-- � �� rend.material.color = originalColor;
    }

    public void ResetColor()
    {
        if (isReachable)
            rend.material.color = reachableColor;
        else
            rend.material.color = originalColor;
    }

    public void ClearAllHighlights()
    {
        Main main = FindObjectOfType<Main>();
        if (main == null) return;
        foreach (var cell in main.CellData)
            if (cell != null && cell.CellObject != null)
            {
                var cellScript = cell.CellObject.GetComponent<GridCellBehaviour>();
                if (cellScript != null)
                    cellScript.ResetColor();
            }
    }
}