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

    void Update()
    {
        if (isMoving)  // проверка, движется ли юнит
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime); // перемещение юнита к цели

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) // проверка, достиг ли юнит цели
                isMoving = false; // остановка движения, если цель достигнута
        }
    }

    void Awake()
    {

    }
    public void SetAsActiveUnit()
    {
        player = GameObject.FindWithTag("Select");
    }

    public void StartMoving(List<MoveCell> moveCells)
    {
        if (!Global.Instance.isDone)
        {
            Debug.Log("Другой юнит уже двигается. Жди.");
            return; // нельзя начать новое движение
        }

        Global.Instance.isDone = false; // блокируем движение других юнитов

        if (!isMoving)
            StartCoroutine(MoveThroughCells(moveCells));
    }

    IEnumerator MoveThroughCells(List<MoveCell> moveCells)
    {
        if (moveCells == null || moveCells.Count == 0)
        {
            Debug.LogWarning("Нет клеток или юнит не найден.");
            yield break;
        }

        isMoving = true;

        foreach (var cell in moveCells)
        {
            MoveTo(cell.Position);

            while (Vector3.Distance(player.transform.position, cell.Position) > 0.01f)
            {
                yield return null;
            }

            yield return new WaitForSeconds(delayBetweenSteps);
        }

        Debug.Log("Движение по списку завершено.");

        Global.Instance.isDone = true;

        isMoving = false;
    }

    public void MoveTo(Vector3 newPosition)  // метод для установки новой цели движения
    {
        targetPosition = newPosition;  // установка новой позиции цели
        isMoving = true;  // установка флага движения в true, чтобы начать движение
    }
}
