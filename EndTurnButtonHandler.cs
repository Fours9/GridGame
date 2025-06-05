using UnityEngine;
using UnityEngine.UI;

public class EndTurnButtonHandler : MonoBehaviour
{
    public Button endTurnButton;

    void Update()
    {
        var currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        bool canPress = false;
        if (currentUnit != null && currentUnit.isPlayerControlled)
        {
            // Получаем UnitMover этого юнита
            var mover = currentUnit.UnitObject.GetComponent<UnitMover>();
            if (mover != null)
            {
                canPress = !mover.isMoving;
            }
            else
            {
                canPress = true; // Если не нашли, пусть кнопка работает (или false — на твой выбор)
            }
        }
        endTurnButton.interactable = canPress;
    }

    public void OnEndTurnButtonClick()
    {
        var currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (currentUnit != null && currentUnit.isPlayerControlled)
        {
            var mover = currentUnit.UnitObject.GetComponent<UnitMover>();
            if (mover != null && mover.isMoving)
            {
                Debug.LogWarning("Нельзя завершить ход, пока юнит двигается!");
                return;
            }
            InitiativeManager.Instance.EndCurrentTurn();
        }
        else
        {
            Debug.LogWarning("EndTurn нажата не на игроке!");
        }
    }
}