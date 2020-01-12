using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanMayor : HumanGameEntity
{
    StateMachine<HumanMayor> stateMachine;

    [SerializeField] public Vector3 initPos;
    public float initOri;

    public static float restMinTime = 5.0f;
    public static float restMaxTime = 8.0f;
    public float restTimer;

    public float talkMinTime;
    public float talkMaxTime;
    float talkTimer;
    public RandomWords sRandWords;

    // Unity-chan attributes
    public CapsuleCollider col;
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

    public int staminaRestoreRate;
    public int staminaConsumeRate;
    float staminaConsumeUpdateRate;
    float staminaConsumeUpdate;


    public HumanMayor(int id) : base(id)
    {
    }

    public void Awake()
    {
        ID = nextValidID;

        restTimer = Random.Range(restMinTime, restMaxTime);

        stateMachine = new StateMachine<HumanMayor>(this)
        {
            CurrentState = HumanMayorRest.Instance
        };
    }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        staminaConsumeUpdateRate = 1f;
        staminaConsumeUpdate = 0f;
        fleeStopMaxTime = 3.0f;
        health = 100;
        maxSpeed = move_speed = 2.0f;
        stamina = maxStamina = 100;
        animSpeed = 1.25f;
        turn_speed = 360f * biped.TurnTime;

        initPos = biped.Position; // Supposed to be the "spawn position"
        initOri = biped.transform.eulerAngles.y; // The "spawn orientation"
        cSteerPoint.TargetPoint = initPos;

        col = GetComponent<CapsuleCollider>();
        sRandWords = GetComponent<RandomWords>();
        orgColHight = col.height;
        orgVectColCenter = col.center;

        talkTimer = Random.Range(talkMinTime, talkMaxTime);
        sRandWords.enabled = false;
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
        stateMachine.ChangeState(HumanMayorRest.Instance);
    }

    public override void Update()
    {
        base.Update();

        anim.speed = animSpeed;
        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
        stateMachine.Update();
        staminaConsumeUpdate += Time.deltaTime;

        // If not in idle state and moving, drain stamina, else restore stamina if needed
        if (staminaConsumeUpdate >= staminaConsumeUpdateRate)
        {
            if (stateMachine.CurrentState != HumanMayorRest.Instance && biped.Speed >= 0.1f)
                ModStamina(-staminaConsumeRate);
            else
                ModStamina(staminaRestoreRate);

            staminaConsumeUpdate = 0f;
        }
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return false;
    }

    void resetCollider()
    {
        col.height = orgColHight;
        col.center = orgVectColCenter;
    }

    public StateMachine<HumanMayor> GetFSM()
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
        sRandWords.PlayRandomSalutationAudio();
    }

    public void PlayRandomTalkAudio()
    {
        sRandWords.PlayRandomTalkAudio();
    }

    public void PlayRandomFarewellAudio()
    {
        sRandWords.PlayRandomFarewellAudio();
    }

    public void PlayDeathAudio()
    {
        sRandWords.PlayDeathAudio();
    }

    public void UpdateTalkTime()
    {
        talkTimer -= Time.deltaTime;

        if (talkTimer <= 0f)
        {
            PlayRandomTalkAudio();
            talkTimer = Random.Range(talkMinTime, talkMaxTime);
        }
    }
}
