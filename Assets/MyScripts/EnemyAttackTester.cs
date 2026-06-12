using UnityEngine;

public class EnemyAttackTester : MonoBehaviour
{
    [SerializeField] private EnemyGuardTargetFollower guardFollower;
    [SerializeField] private float jabInterval = 2f;

    private float timer;

    private void Update()
    {
        if (guardFollower == null)
            return;

        timer += Time.deltaTime;

        if (timer >= jabInterval)
        {
            if (guardFollower.TryStartJab())
            {
                timer = 0f;
            }
        }
    }
}