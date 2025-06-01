using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UnitMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 targetPosition;
    private bool isMoving = false;

    private GameObject player;
    public float delayBetweenSteps = 0.1f;

    private Coroutine moveCoroutine;

    private bool shouldStop = false;

    private MoveCell currentCell;

    void Update()
    {
        if (isMoving)  // ��������, �������� �� ����
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime); // ����������� ����� � ����

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) // ��������, ������ �� ���� ����
                isMoving = false; // ��������� ��������, ���� ���� ����������
        }

        if (Input.GetMouseButtonDown(1)) // ������ ������ ����
        {
            if (isMoving == true && IsActiveUnit())
            {
                shouldStop = true;
            }
        }
    }

    void Awake()
    {

    }
    public void SetAsActiveUnit()
    {
        player = GameObject.FindWithTag("Select");
    }

    public bool IsActiveUnit()
    {
        return player == this.gameObject;
    }

    public void StartMoving(List<MoveCell> moveCells)
    {
        if (!Global.Instance.isDone)
        {
            Debug.Log("������ ���� ��� ���������. ���.");
            return; // ������ ������ ����� ��������
        }

        Global.Instance.isDone = false; // ��������� �������� ������ ������

        if (!isMoving)
            moveCoroutine = StartCoroutine(MoveThroughCells(moveCells));
    }

    IEnumerator MoveThroughCells(List<MoveCell> moveCells)
    {
        if (moveCells == null || moveCells.Count == 0)
        {
            Debug.LogWarning("��� ������ ��� ���� �� ������.");
            yield break;
        }

        isMoving = true;
        shouldStop = false;

        foreach (var cell in moveCells)
        {
            if (shouldStop)
            {
                Debug.Log("�������� ����������� ����� �������: " + cell.Position);
                break;
            }
            if (currentCell != null)
            {
                currentCell.SetOccupied(null); // ���������� ���������� ������
            }

            currentCell = cell;
            currentCell.SetOccupied(this.gameObject); // ������ ����� ������

            MoveTo(cell.Position);

            while (Vector3.Distance(player.transform.position, cell.Position) > 0.01f)
            {
                yield return null;
            }

            yield return new WaitForSeconds(delayBetweenSteps);
        }

        Debug.Log("�������� �� ������ ���������.");

        Global.Instance.isDone = true;

        isMoving = false;
    }

    public void MoveTo(Vector3 newPosition)  // ����� ��� ��������� ����� ���� ��������
    {
        targetPosition = newPosition;  // ��������� ����� ������� ����
        isMoving = true;  // ��������� ����� �������� � true, ����� ������ ��������
    }
}
