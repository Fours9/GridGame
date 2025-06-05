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
            // �������� UnitMover ����� �����
            var mover = currentUnit.UnitObject.GetComponent<UnitMover>();
            if (mover != null)
            {
                canPress = !mover.isMoving;
            }
            else
            {
                canPress = true; // ���� �� �����, ����� ������ �������� (��� false � �� ���� �����)
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
                Debug.LogWarning("������ ��������� ���, ���� ���� ���������!");
                return;
            }
            InitiativeManager.Instance.EndCurrentTurn();
        }
        else
        {
            Debug.LogWarning("EndTurn ������ �� �� ������!");
        }
    }
}