using UnityEngine;

[DisallowMultipleComponent]
public class HitZone : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;
}