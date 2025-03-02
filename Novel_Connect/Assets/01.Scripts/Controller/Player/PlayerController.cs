using DG.Tweening;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static Define;

public class PlayerController : BaseController
{
    private bool init = false;
    public PlayerData data;
    public Playermovement movement;
    public PlayerAttack attack;
    public PlayerSkill[] skills;
    public PlayerSound sound;
    public PlayerElemental elementals;
    public Inventory inventory;
    public PlayerState state;
    public PlayerState beforestate;
    public Transform checkIsGroundTrans;
    public Transform attackTrans;
    public LayerMask groundLayer;
    public LayerMask attackLayer;
    private Dictionary<PlayerState, State<PlayerController>> states;
    private StateMachine<PlayerController> stateMachine;
    public Dictionary<string, int> HASH_ANIMATION;

    public void Init(int _level, string _elementalString)
    {
        elementals = new PlayerElemental(this);
        trans = gameObject.GetOrAddComponent<Transform>();
        spriteRenderer = trans.GetOrAddComponent<SpriteRenderer>();
        animator = trans.GetOrAddComponent<Animator>();
        effectUIAnimator = Util.FindChild<Animator>(gameObject, "EffectUI");
        rb = trans.GetOrAddComponent<Rigidbody2D>();
        inventory = new Inventory();
        checkIsGroundTrans = Util.FindChild<Transform>(gameObject, "CheckIsGroundTrans");
        attackTrans = Util.FindChild<Transform>(gameObject, "AttackTrans");
        groundLayer = LayerMask.GetMask("Ground");
        attackLayer = LayerMask.GetMask("Hitable");
        HASH_ANIMATION = new Dictionary<string, int>();
        HASH_ANIMATION.Add("isRun", Animator.StringToHash("isRun"));
        HASH_ANIMATION.Add("isJump", Animator.StringToHash("isJump"));
        HASH_ANIMATION.Add("isFall", Animator.StringToHash("isFall"));
        HASH_ANIMATION.Add("isAttack", Animator.StringToHash("isAttack"));
        HASH_ANIMATION.Add("AttackCount", Animator.StringToHash("AttackCount"));
        HASH_ANIMATION.Add("isFreeze", Animator.StringToHash("isFreeze"));
        HASH_ANIMATION.Add("isDead", Animator.StringToHash("isDead"));
        HASH_ANIMATION.Add("isDash", Animator.StringToHash("isDash"));

        Managers.Event.OnVoidEvent -= CheckDie;
        Managers.Event.OnVoidEvent += CheckDie;

        Managers.Data.GetPlayerData(_level, (_data) =>
        {
            data = _data;
            status = new ControllerStatus(this);
            status.currentHP = _data.hp;
            status.maxHP = _data.hp;
            status.currentMP = _data.mp;
            status.maxMP = _data.mp;
            status.maxJumpForce = _data.jumpForce;
            status.currentJumpForce = _data.jumpForce;
            status.maxWalkSpeed = _data.walkSpeed;
            status.currentWalkSpeed = _data.walkSpeed;
            status.maxRunSpeed = _data.runSpeed;
            status.currentRunSpeed = _data.runSpeed;
            status.currentAttackForce = _data.force;
            status.isDead = false;
        });
        Elemental _elemental = Util.ParseEnum<Elemental>(_elementalString);
        elementals.ChangeElemental(_elemental, () =>
        {
            states = new Dictionary<PlayerState, State<PlayerController>>();
            states.Add(PlayerState.IDLE, new PlayerStates.Idle());
            states.Add(PlayerState.WALK, new PlayerStates.Walk());
            states.Add(PlayerState.RUN, new PlayerStates.Run());
            states.Add(PlayerState.JUMP, new PlayerStates.JumpStart());
            states.Add(PlayerState.JUMPING, new PlayerStates.Jumping());
            states.Add(PlayerState.FALL, new PlayerStates.Falling());
            states.Add(PlayerState.ATTACK, new PlayerStates.Attack());
            states.Add(PlayerState.CASTSKILL_ONE, new PlayerStates.CastSkill_One());
            states.Add(PlayerState.CASTSKILL_TWO, new PlayerStates.CastSkill_Two());
            states.Add(PlayerState.DASH, new PlayerStates.Dash());
            states.Add(PlayerState.FREEZED, new PlayerStates.Freeze());
            states.Add(PlayerState.DIE, new PlayerStates.Die());
            stateMachine = new StateMachine<PlayerController>(this, states[PlayerState.IDLE]);
            init = true;
        });

    }

    public void Update()
    {
        if (!init) return;
        if (status.isDead) return;
        if (!Managers.Input.isCanControl) return;
        CheckSkillCooltime();
        stateMachine.UpdateState();
        movement.CheckIsGround();
        inventory.CheckOpenUIInventory();
        movement.Update();
        elementals.ChangeMP();
    }

    public override void ChangeDirection(Direction _direction)
    {
        if (direction == _direction) return;
        direction = _direction;
        if (_direction == Direction.Left) transform.eulerAngles = Vector3.zero;
        if (_direction == Direction.Right) transform.eulerAngles = new Vector3(0, 180, 0);
    }

    public void GetFullHP()
    {
        if (!init) return;
        status.currentHP = status.maxHP;
        Managers.Event.OnVoidEvent?.Invoke(VoidEventType.OnChangeHP);
    }

    public override void GetDamage(float _damage)
    {
        if (!init) return;
        if (status.isDead) return;
        status.currentHP -= _damage;
        Managers.Routine.StartCoroutine(GetDamageRoutine());
        Managers.Sound.PlaySoundEffect(SoundProfile_Effect.Player_Damaged);
        Managers.Event.OnVoidEvent?.Invoke(VoidEventType.OnChangeHP);
    }

    public IEnumerator GetDamageRoutine()
    {
        if(state != PlayerState.FREEZED)
        {
            spriteRenderer.color = new Color32(255, 122, 122, 255);
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = Color.white;
        }
    }

    public override void Hit(Transform _attackerTrans, float _damage)
    {
        if (status.isDead) return;
        Managers.Screen.Shake(2,0.2f);
        GetDamage(_damage);
    }

    public override void SetPosition(Vector2 _position)
    {
        trans.position = _position;
    }

    public void ChangeState(PlayerState _state, bool _isChangeSameState = false)
    {
        if (state == _state)
        {
            if (_isChangeSameState)
                stateMachine.ChangeState(states[_state]);
            return;
        }
        beforestate = state;
        state = _state;
        stateMachine.ChangeState(states[_state]);
    }

    public void ChangeStateWithDelay(PlayerState _nextState,float _delayTime)
    {
        Managers.Routine.StartCoroutine(ChangeStateWithDelayRoutine(_nextState, _delayTime));
    }

    private IEnumerator ChangeStateWithDelayRoutine(PlayerState _nextState, float _delayTime)
    {
        yield return new WaitForSeconds(_delayTime);
        ChangeState(_nextState);
    }

    public void ChangeStateWithAnimtionTime(PlayerState _nextState)
    {
        Managers.Routine.StartCoroutine(ChangeStateWithAnimtionTimeRoutine(_nextState));
    }

    private IEnumerator ChangeStateWithAnimtionTimeRoutine(PlayerState _nextState)
    {
        yield return new WaitForSeconds(0.05f);
        float animationPlayTime = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationPlayTime - 0.05f);
        ChangeState(_nextState);
    }
    public override void KnockBack()
    {

    }

    public override void KnockBack(float _force)
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(checkIsGroundTrans.position,checkIsGroundTrans.localScale);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackTrans.position, attackTrans.localScale);
    }

    public void CheckDie(VoidEventType _eventType)
    {
        if (status.isDead) return;
        if (_eventType != VoidEventType.OnChangeHP) return;
        if (status.currentHP <= 0)
            ChangeState(PlayerState.DIE);
    }

    public override void Die()
    {
        Managers.Event.OnVoidEvent?.Invoke(VoidEventType.OnDeadPlayer);
        Stop();
        status.isDead = true;
    }

    protected override IEnumerator DieRoutine()
    {
        yield return null;
    }
    public void AnimationEvent_Attack()
    {
        attack.Attack();
    }
    public void AnimationEvent()
    {
        if (state == PlayerState.ATTACK) attack.Attack();
        if (state == PlayerState.DASH) Managers.Routine.StartCoroutine(movement.DashRoutine());
    }
    public void AnimationEvent_End()
    {
        if (state == PlayerState.ATTACK) ChangeState(PlayerState.IDLE);
        if (state == PlayerState.DASH) ChangeState(PlayerState.IDLE);
    }

    public void CheckSkillCooltime()
    {
        if(skills.Length > 0)
        {
            skills[0]?.CheckCoolTime();
            skills[1]?.CheckCoolTime();
        }
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (!inventory.isCanGet) return;
        if(collision.CompareTag("Item"))
        {
            Managers.Input.CheckInput(Managers.Input.pickupItemKey, (_inputType) => 
            {
                if (_inputType != InputType.HOLD) return;
                inventory.isCanGet = false;
                ItemController item = collision.GetOrAddComponent<ItemController>();
                item.PutInInventory(trans);
                Managers.Routine.StartCoroutine(inventory.CheckCanGetRoutine());
            });
        }
    }

    public override void Freeze()
    {
        if (state == PlayerState.DIE) return;
        ChangeState(PlayerState.FREEZED);
        if (attack.attackCoroutine != null)
            Managers.Routine.StopCoroutine(attack.attackCoroutine);
        attack.currentAttackCount = 0;
        changeStateCoroutine = Managers.Routine.StartCoroutine(StopFreeze());
        spriteRenderer.color = Color.blue;
    }

    public override void SetFreezeUI()
    {
        effectUIAnimator.SetInteger("FreezeCount", status.freezeCount);
    }

    public IEnumerator StopFreeze()
    {
        yield return new WaitForSeconds(2.5f);
        spriteRenderer.DOColor(Color.white, 0.5f);
        yield return new WaitForSeconds(0.5f);
        ChangeState(PlayerState.IDLE);
    }
}
public enum PlayerState
{
    IDLE, WALK, RUN, JUMP, JUMPING, FALL, ATTACK , CASTSKILL_ONE, CASTSKILL_TWO, DASH, FREEZED, DIE
}

[System.Serializable]
public class PlayerData
{
    public int level;
    public int levelUpExp;
    public float hp;
    public float mp;
    public float force;
    public float walkSpeed;
    public float runSpeed;
    public float jumpForce;



    public PlayerData(int _level)
    {
        Managers.Data.GetPlayerData(_level, (data) =>
        {
            level = data.level;
            levelUpExp = data.levelUpExp;
            hp = data.hp;
            mp = data.mp;
            force = data.force;
            walkSpeed = data.walkSpeed;
            runSpeed = data.runSpeed;
            jumpForce = data.jumpForce;
        });
    }
}

[System.Serializable]
public class PlayerSaveData
{
    public int level;
    public string elemental;
}
