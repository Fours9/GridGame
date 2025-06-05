using UnityEngine;
using UnityEngine.UI;

public class EndTurnButtonHandler : MonoBehaviour
{
    public Button endTurnButton;

    void Update()
    {
        var currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        endTurnButton.interactable = currentUnit != null && currentUnit.isPlayerControlled;
    }

    public void OnEndTurnButtonClick()
    {
        var currentUnit = InitiativeManager.Instance.GetCurrentUnit();
        if (currentUnit != null && currentUnit.isPlayerControlled)
        {
            InitiativeManager.Instance.EndCurrentTurn();
        }
        else
        {
            Debug.LogWarning("EndTurn нажата не на игроке!");
        }
    }
}