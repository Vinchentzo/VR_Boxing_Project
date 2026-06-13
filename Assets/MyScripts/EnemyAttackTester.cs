using UnityEngine;

public class EnemyAttackTester : MonoBehaviour
{
    private enum AttackToTest
    {
        LeftJab,
        RightCross,
        Random
    }

    [SerializeField] private EnemyGuardTargetFollower guardFollower;
    [SerializeField] private AttackToTest attackToTest = AttackToTest.Random;

    [Header("Timing")]
    [SerializeField] private float minAttackInterval = 1.2f;
    [SerializeField] private float maxAttackInterval = 2.6f;

    private float timer;
    private float currentInterval;

    private void Awake()
    {
        PickNextInterval();
    }

    private void Update()
    {
        if (guardFollower == null)
            return;

        timer += Time.deltaTime;

        if (timer < currentInterval)
            return;

        bool started = attackToTest switch
        {
            AttackToTest.LeftJab => guardFollower.TryStartLeftJab(),
            AttackToTest.RightCross => guardFollower.TryStartRightCross(),
            AttackToTest.Random => TryStartRandomAttack(),
            _ => false
        };

        if (started)
        {
            timer = 0f;
            PickNextInterval();
        }
    }

    private bool TryStartRandomAttack()
    {
        float roll = Random.value;

        // First try jab more often.
        // If jab is not valid because of distance, try cross.
        if (roll < 0.65f)
        {
            if (guardFollower.TryStartLeftJab())
                return true;

            return guardFollower.TryStartRightCross();
        }

        // Sometimes try cross first.
        // If cross is not valid because of distance, try jab.
        if (guardFollower.TryStartRightCross())
            return true;

        return guardFollower.TryStartLeftJab();
    }

    private void PickNextInterval()
    {
        currentInterval = Random.Range(minAttackInterval, maxAttackInterval);
    }
}