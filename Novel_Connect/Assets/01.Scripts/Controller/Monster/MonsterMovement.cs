using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;

public abstract class MonsterMovement
{
    protected MonsterController monster;
    public Coroutine checkDetecteCoroutine;
    public abstract void CheckMove();
    public void StopCheckMove()
    {
        if(checkDetecteCoroutine != null)
        {
            Managers.Routine.StopCoroutine(checkDetecteCoroutine);
            checkDetecteCoroutine = null;
        }
    }
    public abstract void Move();
    public void LookAtTarget()
    {
        if(Mathf.Abs(monster.targetTrans.position.x - monster.trans.position.x) > 0.2f)
            monster.LookAtTarget();
    }

    public void KnockBack()
    {

    }

    public abstract void StopMoveCoroutine();
}

namespace MonsterMovements
{
    public class BaseMovement : MonsterMovement
    {
        public BaseMovement(MonsterController _monster)
        {
            monster = _monster;
            checkDetecteCoroutine = null;
        }

        public override void CheckMove()
        {
            
        }


        public override void Move()
        {

        }

        public override void StopMoveCoroutine()
        {
            throw new System.NotImplementedException();
        }
    }

    public class Ghost_Bat : MonsterMovement
    {
        public  Coroutine jumpCoroutine;
        public Ghost_Bat(MonsterController _monster)
        {
            monster= _monster;
            checkDetecteCoroutine = null;
        }

        public override void CheckMove()
        {
            checkDetecteCoroutine = Managers.Routine.StartCoroutine(CheckMoveRoutine());
        }

        private IEnumerator CheckMoveRoutine()
        {
            if(monster != null)
            {
                Collider2D[] colliders = Physics2D.OverlapBoxAll(monster.detecteTrans.position,monster.detecteTrans.localScale,0,monster.attackLayer);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].CompareTag("Player"))
                    {
                        monster.ChangeState(MonsterState.FOLLOW);
                        monster.SetTarget(colliders[i].transform);
                        if(checkDetecteCoroutine != null) Managers.Routine.StopCoroutine(this.checkDetecteCoroutine);
                        break;
                    }
                }
                yield return new WaitForSeconds(0.5f);
                checkDetecteCoroutine = Managers.Routine.StartCoroutine(this.CheckMoveRoutine());
            }
        }

        public override void Move()
        {
            monster.rb.velocity = new Vector2(monster.status.currentWalkSpeed * Time.fixedDeltaTime * 10 * (int)monster.direction, monster.rb.velocity.y);
        }

        private IEnumerator Jump()
        {
            yield return new WaitForSeconds(3);
            monster.rb.AddForce(new Vector2(0,3),ForceMode2D.Impulse);
        }

        public void StartJump()
        {
            jumpCoroutine = Managers.Routine.StartCoroutine(Jump());
        }

        public override void StopMoveCoroutine()
        {
            if(checkDetecteCoroutine != null)
                Managers.Routine.StopCoroutine(checkDetecteCoroutine);
            if(jumpCoroutine != null)
                Managers.Routine.StopCoroutine(jumpCoroutine);
        }
    }
}

