using UnityEngine;
using UnityEngine.UI;

public class EndTurnButtonHandler : MonoBehaviour
{
    public Button endTurnButton;

    void Update()
    {
        var unit = InitiativeManager.Instance?.GetCurrentUnit();
        endTurnButton.interactable = (unit != null && unit.team == TeamType.Player);
    }

    public void OnEndTurnButtonClick()
    {
        var unit = InitiativeManager.Instance?.GetCurrentUnit();
        if (unit != null && unit.team == TeamType.Player)
        {
            InitiativeManager.Instance.EndCurrentTurn();
            Debug.Log("������ ������ '��������� ���'");
        }
        else
        {
            Debug.Log("������ �� ��� ������! ������ ������������.");
        }
    }
}