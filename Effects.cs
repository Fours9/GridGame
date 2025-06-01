using UnityEngine;


public interface ICellEffect
{
    void ApplyEffect(GameObject unit);
}


public class BurnEffect : MonoBehaviour, ICellEffect
{
    public int damageAmount = 20;

    public void ApplyEffect(GameObject unit)
    {
        var health = unit.GetComponent<UnitController>();
        if (health != null)
        {
            health.TakeDamage(damageAmount);
            Debug.Log($"{unit.name} получил {damageAmount} урона от горящей клетки!");
        }
    }
}