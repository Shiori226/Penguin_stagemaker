using UnityEngine;

public interface IItemEffect
{
    void ApplyEffect(Collider other, string itemName);
}
