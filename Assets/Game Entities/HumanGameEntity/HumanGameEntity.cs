using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySteer;

public class HumanGameEntity : BaseGameEntity
{
    public MessageDispatcher messageDispatcher;

    public UnitySteer.Behaviors.SteerForFear cSteerFear;
    public UnitySteer.Behaviors.SteerForPoint cSteerPoint;
    public UnitySteer.Behaviors.Biped biped;
    public Rigidbody rb;
    public Animator anim;

    public List<GameObject> pursuers;

    public float fleeStopMaxTime = 5.0f;
    public float fleeStopTimer;
    public float riseInZombieTime;
    public float respawnTime = 10f;
    float deadTime;

    public bool playerInteracted;
    public bool KilledByZombie = false;

    protected Vector3 spawnPos;
    protected float spawnOri;

    public GameObject zombiePrefab;

    public HumanGameEntity(int id) : base(id)
    {
    }

    // Start is called before the first frame update
    public override void Start()
    {
        spawnPos = gameObject.transform.position;
        spawnOri = gameObject.transform.eulerAngles.y;

        EntityManager.Instance.RegisterEntity(this);

        cSteerFear = GetComponent<UnitySteer.Behaviors.SteerForFear>();
        cSteerPoint = GetComponent<UnitySteer.Behaviors.SteerForPoint>();
        anim = GetComponent<Animator>();
        biped = GetComponent< UnitySteer.Behaviors.Biped>();
        rb = GetComponent<Rigidbody>();

        cSteerFear.enabled = false;
        cSteerPoint.enabled = false;

        respawnTime = 4.5f; // Same as turn in zombie, good case would be for this to be greater and the object to not be drawn until respawn
        deadTime = 0f;

        maxSpeed = 2f;
        move_speed = maxSpeed;
        stamina = maxStamina = 100;
    }

    // Update is called once per frame
    public override void Update()
    {

        if (health <= 0)
        {
            deadTime += Time.deltaTime;

            if (deadTime >= respawnTime)
                Respawn();
        }
    }

    // Modifies the stamina by adding amount to current and changes speed depending on result
    public void ModStamina(int amount)
    {
        if (stamina >= maxStamina - amount)
            stamina = maxStamina;
        else if (stamina < -amount)
            stamina = 0;
        else
            stamina = (uint)(stamina + amount);

        if (stamina >= maxStamina * 0.9f)
        {
            biped.MaxSpeed = maxSpeed;
            move_speed = maxSpeed;
        }
        else if (stamina <= maxStamina * 0.05f)
        {
            biped.MaxSpeed = maxSpeed * 0.1f;
            move_speed = maxSpeed * 0.1f;
        }
        else if (stamina <= maxStamina >> 1)
        {
            biped.MaxSpeed = maxSpeed / 2f;
            move_speed = maxSpeed / 2f;
        }
    }

    public bool IsTimeRiseIntoZombie()
    {
        if (KilledByZombie && deadTime >= riseInZombieTime)
        {
            KilledByZombie = false;
            return true;
        }

        return false;
    }

    public override void Respawn()
    {
        cSteerFear.enabled = false;
        cSteerPoint.enabled = false;

        gameObject.transform.position = spawnPos;
        gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, spawnOri, gameObject.transform.eulerAngles.z);
        gameObject.SetActive(true);
        deadTime = 0f;

        anim.Rebind();
        dead = KilledByZombie = false;
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return false;
    }

    public bool HasPursuer()
    {
        return pursuers.Count > 0;
    }

    public GameObject GetPursuer(GameObject pursuer)
    {
        if (pursuers.IndexOf(pursuer) >= 0)
            return pursuers[pursuers.IndexOf(pursuer)];

        return null;
    }

    public List<GameObject> GetPursuers()
    {
        // Remove possible null gameobjects (destroyed ones) from list
        // if the condition changes, then this has to be modified
        pursuers.RemoveAll(gameobj => gameobj == null);

        return pursuers;
    }

    public void SetPlayerInteracted(bool active)
    {
        playerInteracted = active;
    }

    public bool GetPlayerInteracted()
    {
        return playerInteracted;
    }

    override public void ReceiveAttack(BaseGameEntity attacker, int damage)
    {
        base.ReceiveAttack(attacker, damage);

        if (dead && attacker is Zombie)
            KilledByZombie = true;
    }
}
