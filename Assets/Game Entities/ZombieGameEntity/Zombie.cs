using System;
using UnityEngine;
using UnitySteer;

using System.Collections.Generic;

// Zombie class
public class Zombie : BaseGameEntity
{
    //the distance at which the zombie detects living beings
    public int DetectionRadius = 20;

    //the distance at which the zombie can attack living beings
    public float AttackRadius = 3;

    //the damage the zombie deals to living beings
    public int AttackDamage = 10;

    //variabes to control the updates of states in time
    public const int FrameUpdateThreshold = 10;
    int frameCount = 0;

 
    public BaseGameEntity target;
    public HashSet<BaseGameEntity> targets;

    //an instance of the state machine class
    StateMachine<Zombie> stateMachine;

    //components to be retrieved from the gameobject
    [HideInInspector] public UnitySteer.Behaviors.SteerForWander cSteeringWander;
    [HideInInspector] public UnitySteer.Behaviors.SteerForPursuit cSteeringPursuit;
    [HideInInspector] public AudioSource audio;
    [HideInInspector] public Animator animator;
    [HideInInspector] public UnitySteer.Behaviors.Biped biped;
    [HideInInspector] public Rigidbody rb;

    //audioclips to play
    public AudioClip[] searchingClips;
    public AudioClip[] followingClips;
    public AudioClip[] attackClips;
    public AudioClip[] damagedClips;
    public AudioClip[] deathClips;

    public void Awake()
    {
        ID = nextValidID;
        //attackCooldown = 10;

        stateMachine = new StateMachine<Zombie>(this);
        stateMachine.CurrentState = LookingForLivingBeing.Instance;
    }

    public Zombie(int id) : base(id)
    {
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return stateMachine.HandleMessage(telegram);
    }

    public StateMachine<Zombie> GetFSM()
    {
        return stateMachine;
    }

    public bool HasTarget()
    {
        return !(target == null);
    }

    public BaseGameEntity GetTarget()
    {
        return target;
    }

    public void ClearTarget()
    {
        target = null;
    }

    public bool CanAttack() 
    {
        return cooldown <= 0;
    }

    public bool isDead()
    {
        return this.dead;
    }

    public void PlaySearchingClip()
    {
        audio.clip = searchingClips[UnityEngine.Random.Range(0, searchingClips.Length)];
        audio.Play();
    }

    public void PlayAttackClip()
    {
        audio.clip = attackClips[UnityEngine.Random.Range(0, attackClips.Length)];
        audio.Play();
    }

    public void PlayFollowingClip()
    {
        audio.clip = followingClips[UnityEngine.Random.Range(0, followingClips.Length)];
        audio.Play();
    }

    public void PlayDamagedClip()
    {
        audio.clip = damagedClips[UnityEngine.Random.Range(0, damagedClips.Length)];
        audio.Play();
    }
    public void PlayDeathClip()
    {
        audio.clip = deathClips[UnityEngine.Random.Range(0, deathClips.Length)];
        audio.Play();
    }

    public override void ReceiveAttack(BaseGameEntity attacker, int damage)
    {
        base.ReceiveAttack(attacker, damage);
        if (!dead) PlayDamagedClip();
    }

    private BaseGameEntity GetNearestTarget()
    {
        float bestDistance = 10000;
        BaseGameEntity bestTarget = null;

        foreach (BaseGameEntity t in targets) {
            if (t == null) continue;

            float d = Vector3.Distance(transform.position, t.transform.position);

            //Check if target can be seen
            bool canBeSeen = true;
            Vector3 eyesPos = transform.position + Vector3.up * 0.8f;
            Vector3 targetPos = t.transform.position;
            Ray ray = new Ray(eyesPos, targetPos - eyesPos);
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray, Vector3.Distance(targetPos, eyesPos));

            for (int i = 0; i < hits.Length; i++)
            {
                GameObject hitObj;
                hitObj = hits[i].collider.gameObject;
                tag = hitObj.tag;
                if (tag == "chunk")
                {
                    canBeSeen = false;
                }
            }            // Check all conditions to update: is active, is closer than the best one yet and can be seen
            if (t.isActiveAndEnabled && d < bestDistance && canBeSeen)
            {
                bestDistance = d;
                bestTarget = t;
            }
        }

        return bestTarget;
    }

    //this must be implemented
    public override void Update()
    {
        frameCount += 1;
        cooldown -= Time.deltaTime;
        
        if (frameCount % FrameUpdateThreshold == 0)
        {
            target = GetNearestTarget();
            if (target != null) cSteeringPursuit.Quarry = target.GetComponent<UnitySteer.Behaviors.Biped>();
            stateMachine.Update();
        }
    }

    public override void Start()
    {
        EntityManager.Instance.RegisterEntity(this);

        cSteeringWander = GetComponent<UnitySteer.Behaviors.SteerForWander>();
        cSteeringPursuit = GetComponent<UnitySteer.Behaviors.SteerForPursuit>();
        animator = GetComponent<Animator>();
        biped = GetComponent<UnitySteer.Behaviors.Biped>();
        rb = GetComponent<Rigidbody>();
        audio = GetComponent<AudioSource>();

        target = null;
        targets = new HashSet<BaseGameEntity>();
        cSteeringWander.enabled = true;
        cSteeringPursuit.enabled = false;
        animator.SetFloat("MoveSpeed", 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Animal" || other.tag == "Player")
        {
            if (other.GetComponent<Zombie>() != null) return;
            targets.Add(other.GetComponent<BaseGameEntity>());
        } 
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Animal" || other.tag == "Player" || other.tag == "Mayor" || other.tag == "Merchant" || other.tag == "Villager")
        {
            if (other.GetComponent<Zombie>() != null) return;
            targets.Add(other.GetComponent<BaseGameEntity>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Animal" || other.tag == "Player" || other.tag == "Mayor" || other.tag == "Merchant" || other.tag == "Villager")
        {
            targets.Remove(other.GetComponent<BaseGameEntity>());
        }
    }
}
