using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class ObjectManager
{
    private PlayerController player;    // 플레이어 변수 선언
    public PlayerController Player      // 플레이어 프로퍼티 선언
    {
        get 
        {
            if (player == null)
            {
                GameObject go = GameObject.Find("Player");
                if(go == null)
                    SpawnPlayer(Vector3.zero);
            }
            return player; 
        } 
        set { player = value; }
    }
    public HashSet<MonsterController> Monsters { get; } = new HashSet<MonsterController>();     // 몬스터 해쉬 셋
    private Transform monsterTrasnform;
    public Transform MonsterTransform                                                           // 몬스터 스폰 위치 선언
    {
        get
        {
            if (monsterTrasnform == null)
            {
                GameObject root = GameObject.Find("@Monster");
                if (root == null)
                {
                    root = new GameObject { name = "@Monster" };
                    root.transform.position = Vector3.zero;
                }
                monsterTrasnform = root.transform;
            }
            return monsterTrasnform;
        }
    }
    private Transform itemTransform;
    public Transform ItemTransform                                                           // 아이템 생성 위치 선언
    {
        get
        {
            if(itemTransform == null)
            {
                GameObject root = GameObject.Find("@Item");
                if (root == null)
                {
                    root = new GameObject { name = "@Item" };
                    root.transform.position = Vector3.zero;
                }
                itemTransform = root.transform;
            }
            return itemTransform;
        }
    }

    // 몬스터 초기화
    public void ClearMonsters()
    {
        foreach (var item in Monsters)
        {
            item.ChangeState(MonsterState.DIE);
        }
        Monsters.Clear();
    }

    public void Init()
    {
        ClearMonsters();
        Managers.Resource.Destroy(Player.gameObject);
        Managers.Resource.Destroy(MonsterTransform.gameObject);
        Managers.Resource.Destroy(ItemTransform.gameObject);
    }

    // 플레이어 스폰
    public PlayerController SpawnPlayer(Vector3 _position)
    {
        GameObject go = Managers.Resource.Instantiate("Player");
        PlayerController pc = go.GetOrAddComponent<PlayerController>();
        pc.transform.position = _position;
        player = pc;
        player.Init(1, Elemental.Normal.ToString());
        Object.DontDestroyOnLoad(player.gameObject);
        return pc;
    }

    // 몬스터 스폰
    public MonsterController SpawnMonster(Vector3 _position, Define.Monster _monster)
    {
        GameObject go = null;
        switch (_monster)
        {
            case Define.Monster.Ghost_Bat:
                go = Managers.Resource.Instantiate($"{_monster.ToString()}", _pooling: true);
                break;
        }

        MonsterController mc = go.GetOrAddComponent<MonsterController>();
        go.transform.position = _position;
        go.transform.SetParent(MonsterTransform);
        Monsters.Add(mc);
        mc.Init((int)_monster);
        return mc;
    }

    public BossController SpawnBoss(int _bossUID, Vector2 _position)
    {
        GameObject go = null;
        switch (_bossUID)
        {
            case 0:
                go = Managers.Resource.Instantiate($"Ice_Boss");
                break;
        }

        BossController mc = go.GetOrAddComponent<BossController>();
        go.transform.position = _position;
        mc.Init(_bossUID);
        return mc;
    }

    public ItemController SpawnItem(int _itemUID, Vector2 _position, int _count = 1)
    {
        ItemController ic = Managers.Resource.Instantiate("ItemController", _pooling: true).GetComponent<ItemController>();
        ic.transform.position = _position;
        ic.Init(CreateItem<BaseItem>(_itemUID, _count: _count));
        return ic;
    }

    // 몬스터 삭제
    public void MonsterDespawn(MonsterController _object)
    {
        Monsters.Remove(_object);
        Managers.Resource.Destroy(_object.gameObject);
    }
    
    // 아이템 생성
    public T CreateItem<T>(int _itemUID, int _count = 1) where T : BaseItem
    {
        BaseItem baseItem = new BaseItem(_itemUID, _count);
        return baseItem as T;
    }
}
