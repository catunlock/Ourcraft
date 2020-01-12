﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanVillager : HumanGameEntity
{
    StateMachine<HumanVillager> stateMachine;

    [SerializeField] public Vector3 initPos;
    public float initOri;

    [SerializeField] public Vector4[] movePos;
    int currentMovePos;

    public static float restMinTime = 5.0f;
    public static float restMaxTime = 8.0f;
    public float restTimer;

    public float talkMinTime;
    public float talkMaxTime;
    float talkTimer;
    public RandomWords sRandWords;

    public int staminaRestoreRate;
    public int staminaConsumeRate;
    float staminaConsumeUpdateRate;
    float staminaConsumeUpdate;

    public float idleMaxTime;
    public float idleMinTime;
    float idleTimer;

    // Unity-chan attributes
    /*public CapsuleCollider col;
    public AnimatorStateInfo currentBaseState;

    private float orgColHight;
    private Vector3 orgVectColCenter;

    public static int idleState = Animator.StringToHash("Base Layer.Idle");
    public static int locoState = Animator.StringToHash("Base Layer.Locomotion");
    public static int jumpState = Animator.StringToHash("Base Layer.Jump");
    public static int restState1 = Animator.StringToHash("Base Layer.Rest 1");
    public static int restState2 = Animator.StringToHash("Base Layer.Rest 2");
    public static int restState3 = Animator.StringToHash("Base Layer.Rest 3");

    public float animSpeed;
    */

    public HumanVillager(int id) : base(id)
    {
    }

    void FixedUpdate()
    {
        gameObject.transform.eulerAngles = new Vector3(0f, gameObject.transform.eulerAngles.y, 0f);
    }

    public void Awake()
    {
        ID = nextValidID;

        restTimer = Random.Range(restMinTime, restMaxTime);

        stateMachine = new StateMachine<HumanVillager>(this)
        {
            CurrentState = HumanVillagerRest.Instance
        };
    }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        idleTimer = Random.Range(idleMinTime, idleMaxTime);
        currentMovePos = -1;
        staminaConsumeUpdateRate = 1f;
        staminaConsumeUpdate = 0f;
        fleeStopMaxTime = 3.0f;
        health = 100;
        move_speed = 2.0f;
        stamina = 100;
        //animSpeed = 1.25f;
        turn_speed = 360f * biped.TurnTime;

        initPos = biped.Position; // Supposed to be the "spawn position"
        initOri = biped.transform.eulerAngles.y; // The "spawn orientation"
        cSteerPoint.TargetPoint = initPos;

        //col = GetComponent<CapsuleCollider>();
        //sRandWords = GetComponent<RandomWords>();
        //orgColHight = col.height;
        //orgVectColCenter = col.center;

        talkTimer = Random.Range(talkMinTime, talkMaxTime);
        //sRandWords.enabled = false;
    }

    public void ResetTalkTimer()
    {
        talkTimer = Random.Range(talkMinTime, talkMaxTime);
    }

    public override void Respawn()
    {
        base.Respawn();

        health = 100;
        stamina = 100;
        stateMachine.ChangeState(HumanVillagerRest.Instance);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState == HumanVillagerRest.Instance && !cSteerPoint.enabled)
            idleTimer -= Time.deltaTime;

        //anim.speed = animSpeed;
        //currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
        stateMachine.Update();

        staminaConsumeUpdate += Time.deltaTime;

        // If not in idle state and moving, drain stamina, else restore stamina if needed
        if (staminaConsumeUpdate >= staminaConsumeUpdateRate)
        {
            if (stateMachine.CurrentState != HumanVillagerRest.Instance && biped.Speed >= 0.1f)
                ModStamina(-staminaConsumeRate);
            else
                ModStamina(staminaRestoreRate);

            staminaConsumeUpdate = 0f;
        }
        
        if (idleTimer <= 0f)
            idleTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return false;
    }

    public StateMachine<HumanVillager> GetFSM()
    {
        return stateMachine;
    }

    public void OnTriggerStay(Collider collider)
    {
        string tag = collider.gameObject.tag;

        if (!tag.Equals("Zombie")) // Other things to check, like a type
            return;

        GameObject target = collider.gameObject;
        Vector3 agentPos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 direction = targetPos - agentPos;
        float length = direction.magnitude;
        Ray ray = new Ray(agentPos, direction);
        RaycastHit[] hits;

        hits = Physics.RaycastAll(ray, length);
        direction.Normalize();

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObj;
            hitObj = hits[i].collider.gameObject;
            tag = hitObj.tag;

            if (!tag.Equals("Zombie") && hitObj != gameObject)
                return;
        }

        if (pursuers.IndexOf(collider.gameObject) < 0)
            pursuers.Add(collider.gameObject);
    }

    public void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag.Equals("Zombie"))
            pursuers.Remove(collider.gameObject);
    }

    public void PlayRandomSalutationAudio()
    {
        //sRandWords.PlayRandomSalutationAudio();
    }

    public void PlayRandomTalkAudio()
    {
        //sRandWords.PlayRandomTalkAudio();
    }

    public void PlayRandomFarewellAudio()
    {
        //sRandWords.PlayRandomFarewellAudio();
    }

    public void UpdateTalkTime()
    {
        talkTimer -= Time.deltaTime;

        if (talkTimer <= 0f)
        {
            //PlayRandomTalkAudio();
            talkTimer = Random.Range(talkMinTime, talkMaxTime);
        }
    }

    public float GetIdleTimer()
    {
        return idleTimer;
    }

    public int GetCurrentIdlePosIndex()
    {
        return currentMovePos;
    }

    public int ChangeIdlePos()
    {
        int nextPos = Random.Range(-1, movePos.Length);

        if (nextPos == currentMovePos && movePos.Length > 0)
        {
            if (nextPos == movePos.Length - 1)
                nextPos = -1;
            else
                nextPos = (nextPos + 1) % movePos.Length;
        }

        currentMovePos = nextPos;

        return nextPos;
    }
}
