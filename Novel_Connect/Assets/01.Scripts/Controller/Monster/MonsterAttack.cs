using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MonsterAttack
{
    public float canAttackDistance;
    public float attackDelay;
    public float canAttackDelay;
    public bool isCanAttack;
    public Coroutine attackCoroutine;
    protected MonsterController monster;
    public abstract void CheckAttack();
    public abstract void StartAttack();
    public abstract void Attack();
    public abstract IEnumerator AttackRoutine();
}

namespace MonsterAttacks
{

    public class BaseAttack : MonsterAttack
    {
        public BaseAttack(MonsterController _monster)
        {
            monster = _monster;
            isCanAttack = true;
        }
        public override void CheckAttack()
        {
            if (!isCanAttack) return;
            if (Mathf.Abs(monster.trans.position.x - monster.targetTrans.position.x) < monster.attack.canAttackDistance)
            {
                monster.ChangeState(MonsterState.ATTACK);
                return;
            }
        }

        public override void StartAttack()
        {
            isCanAttack = false;
            monster.LookAtTarget();
            monster.sound.PlayAttackSound();
        }

        public override void Attack()
        {
            monster.LookAtTarget();
            Collider2D[] colliders = Physics2D.OverlapBoxAll(monster.attackTrans.position, monster.attackTrans.localScale, 0, monster.attackLayer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].CompareTag("Player"))
                {
                    Managers.Battle.DamageCalculate(monster, Managers.Object.Player, monster.status.currentAttackForce);
                    break;
                }
            }
        }

        public override IEnumerator AttackRoutine()
        {
            monster.ChangeState(MonsterState.FOLLOW);
            yield return new WaitForSeconds(canAttackDelay);
            isCanAttack = true;
            attackCoroutine = null;
        }
    }

    public class Bat : MonsterAttack
    {
        public Bat(MonsterController _monster)
        {
            monster = _monster;
            isCanAttack = true;
        }
        public override void CheckAttack()
        {
            if (!isCanAttack) return;
            if (Mathf.Abs(monster.trans.position.x - monster.targetTrans.position.x) < monster.attack.canAttackDistance)
            {
                monster.ChangeState(MonsterState.ATTACK);
                return;
            }
        }

        public override void StartAttack()
        {
            isCanAttack = false;
            monster.LookAtTarget();
            monster.sound.PlayAttackSound();
        }

        public override void Attack()
        {
            Collider2D[] colliders = Physics2D.OverlapBoxAll(monster.attackTrans.position, monster.attackTrans.localScale, 0, monster.attackLayer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].CompareTag("Player"))
                {
                    Managers.Battle.DamageCalculate(monster, Managers.Object.Player, monster.status.currentAttackForce);
                    break;
                }
            }
        }

        public override IEnumerator AttackRoutine()
        {
            monster.ChangeState(MonsterState.FOLLOW);
            yield return new WaitForSeconds(canAttackDelay);
            isCanAttack = true;
            attackCoroutine = null;
        }
    }
}
