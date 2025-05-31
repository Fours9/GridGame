using System.Collections.Generic;
using UnityEngine;

public class GridCellBehaviour : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    private bool isHovered = false;
    private bool isPressed = false;

    private Main main;
    private Vector3Int startCoords;
    //private bool isReady = false;

    public MoveCell[,,] CellData => main?.CellData;


    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;

        main = FindAnyObjectByType<Main>(); // <- Добавьте это
    }

    void OnMouseEnter()
    {
        isHovered = true;
        if (!isPressed)
            rend.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        isHovered = false;
        if (!isPressed)
            rend.material.color = originalColor;
    }

    void Update()
    {
        // Проверка ПКМ и наведения мыши
        if (Input.GetMouseButtonDown(1) && isHovered)
        {
            rend.material.color = Color.green;

            GameObject unit = GameObject.FindWithTag("Select");
            if (unit != null)
            {
                Vector3 playerPos = unit.transform.position;
                startCoords = Vector3Int.RoundToInt(playerPos);

                Vector3Int targetCoords = Vector3Int.RoundToInt(transform.position);

                if (main.CellData[targetCoords.x, targetCoords.y, targetCoords.z].IsWalkable)
                {
                    Debug.Log($"Start: {startCoords}, Target: {targetCoords}");
                }

                var pathfinder = FindAnyObjectByType<Pathfinding>();
                if (pathfinder != null)
                {
                    List<MoveCell> path = pathfinder.FindPath(startCoords, targetCoords, CellData);

                    for (int i = 0; i < path.Count; i++)
                    {
                        if (path[i] == null)
                        {
                            Debug.LogWarning($"Null MoveCell found at index {i}. Stopping iteration.");
                            break;
                        }

                        Debug.Log($"MoveCell {i}: {path[i].Position}");

                        UnitMover move = unit.GetComponent<UnitMover>();
                        if (move != null)
                        {
                            move.StartMoving(path);
                        }
                        else
                        {
                            Debug.LogError("UnitMover component not found on unit.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Pathfinding component not found.");
                }
            }
        }
    }

    void OnMouseUp()
    {
        isPressed = false;

        if (isHovered)
            rend.material.color = Color.yellow;
        else
            rend.material.color = originalColor;
    }
}