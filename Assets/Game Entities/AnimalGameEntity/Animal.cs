using System;
using UnityEngine;
using UnitySteer;

using System.Collections.Generic;

// Animal class
public class Animal : BaseGameEntity
{
    //time in seconds needed for two animals to spend together to reproduce
    public static float reproductionTime = 5;
    //time in seconds that needs to happen for an animal to be able to reproduce again
    public static float reproductionCooldown = 120;
    public float timeForNextReproduction;
    //distance at which the animals can reproduce
    public static float reproductionRange = 2;
    //partner
    Animal partner = null;

    //growing and adult variables
    public float growRate = 1;
    public bool isAdult = true;
    static float maxScale = 0.25f;

    //distance at which the animal considers it's safe against menaces
    public static int SafeDistance = 15;

    //whether or not the unit has been attacked recently
    public bool HasBeenAttacked = false;
    public bool KilledByZombie = false;

    // speed maximums for stamina control
    public const float normalSpeed = 1f;
    public const float runningSpeed = 3f;
    public const float tiredSpeed = 0.5f;
    bool tired = false;
    bool running = false;

    //variabes to control the updates of states in time
    public const int FrameUpdateThreshold = 10;
    public const int StaminaFrameThreshold = 100;
    int frameCount = 0;

    //an instance of the state machine class
    StateMachine<Animal> stateMachine;

    [HideInInspector] public UnitySteer.Behaviors.SteerForWander cSteeringWander;
    [HideInInspector] public UnitySteer.Behaviors.SteerForEvasion cSteeringEvasion;
    [HideInInspector] public UnitySteer.Behaviors.SteerForNeighborGroup cSteeringCohesion;
    [HideInInspector] public Animator animator;
    [HideInInspector] public AudioSource audio;
    [HideInInspector] public UnitySteer.Behaviors.Biped biped;
    [HideInInspector] public Rigidbody rb;

    public GameObject zombiePrefab;

    public AudioClip[] wanderingClips;
    public AudioClip[] runningClips;
    public AudioClip[] damagedClips;
    public AudioClip[] deathClips;

    public MessageDispatcher messageDispatcher;


    public void Awake()
    {
        ID = nextValidID;
        MaxHealth = 50;
        MaxStamina = 100;

        stateMachine = new StateMachine<Animal>(this);
        stateMachine.CurrentState = LookingForResources.Instance;
    }

    public void PlayWanderingClip()
    {
        audio.clip = wanderingClips[UnityEngine.Random.Range(0, wanderingClips.Length)];
        audio.Play();
    }

    public void PlayRunningClip()
    {
        audio.clip = runningClips[UnityEngine.Random.Range(0, runningClips.Length)];
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

    public Animal(int id) : base(id)
    {
        Debug.Log("Animal created...");
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return stateMachine.HandleMessage(telegram);
    }

    public StateMachine<Animal> GetFSM()
    {
        return stateMachine;
    }

    public bool isDead()
    {
        return this.dead;
    }

    override public void ReceiveAttack(BaseGameEntity attacker, int damage)
    {
        base.ReceiveAttack(attacker, damage);
        HasBeenAttacked = true;
        cSteeringEvasion.Menace = attacker.GetComponent<UnitySteer.Behaviors.Vehicle>();

        if (dead && attacker is Zombie) KilledByZombie = true;
        if (!dead) PlayDamagedClip();
    }

    public void ReduceStamina(uint amount)
    {
        stamina = (uint)Mathf.Max(0, stamina - amount);
        if (stamina == 0)
        {
            biped.MaxSpeed = tiredSpeed;
            tired = true;
        }
    }

    public void RecoverStamina(uint amount)
    {
        stamina = (uint)Mathf.Min(maxStamina, stamina + amount);
        if (tired && stamina > maxStamina / 2)
        {
            if (running) biped.MaxSpeed = runningSpeed;
            else biped.MaxSpeed = normalSpeed;
            tired = false;
        }
    }

    public void StartRunning()
    {
        if (!tired) biped.MaxSpeed = runningSpeed;
        else biped.MaxSpeed = tiredSpeed;
        running = true;
    }

    public void StopRunning()
    {
        if (tired) biped.MaxSpeed = tiredSpeed;
        else biped.MaxSpeed = normalSpeed;
        running = false;
    }

    public bool CanReproduce()
    {
        return health == maxHealth && stamina == maxStamina && partner == null && timeForNextReproduction == 0 && isAdult;
    }

    public Animal SearchPartner()
    {
        List<UnitySteer.Behaviors.Vehicle> neighbors = GetComponent<UnitySteer.Behaviors.SteerForNeighborGroup>().Neighbors;
        
        foreach (UnitySteer.Behaviors.Vehicle v in neighbors)
        {
            if (v != null && v.isActiveAndEnabled && Vector3.Distance(transform.position, v.transform.position) <= reproductionRange)
            {
                Animal a = v.GameObject.GetComponent<Animal>();
                if (a.CanReproduce()) return a;
            }
        }

        return null;
    }

    public void SetPartner(Animal animal)
    {
        partner = animal;
    }

    public bool HasPartner()
    {
        return partner != null;
    }

    public Animal GetPartner()
    {
        return partner;
    }

    public void RemovePartner()
    {
        partner = null;
        timeForNextReproduction = reproductionCooldown;
    }

    public override void Update()
    {
        frameCount += 1;
        timeForNextReproduction = Mathf.Max(0, timeForNextReproduction - Time.deltaTime);

        if (transform.localScale.x < maxScale)
        {
            isAdult = false;
            transform.localScale *= growRate;
        }
        else isAdult = true;

        if (frameCount % FrameUpdateThreshold == 0)
        {
            stateMachine.Update();
            HasBeenAttacked = false;
        }

        if (frameCount % StaminaFrameThreshold == 0)
        {
            if (running && !tired) ReduceStamina(6);
            else RecoverStamina(1);
        }

        // Avoid sinking
        if (transform.position.y < -1.7f) transform.position.Set(transform.position.x, -1.7f, transform.position.z);
    }

    public override void Start()
    {

        EntityManager.Instance.RegisterEntity(this);

        cSteeringWander = GetComponent<UnitySteer.Behaviors.SteerForWander>();
        cSteeringEvasion = GetComponent<UnitySteer.Behaviors.SteerForEvasion>();
        cSteeringCohesion = GetComponent<UnitySteer.Behaviors.SteerForNeighborGroup>();
        animator = GetComponent<Animator>();
        biped = GetComponent<UnitySteer.Behaviors.Biped>();
        biped.MaxSpeed = normalSpeed;
        rb = GetComponent<Rigidbody>();
        audio = GetComponent<AudioSource>();

        cSteeringWander.enabled = true;
        cSteeringEvasion.enabled = false;
        cSteeringCohesion.enabled = true;
        animator.SetFloat("MoveSpeed", 0);

        //timeForNextReproduction = reproductionCooldown;
        timeForNextReproduction = 0;
    }
}
