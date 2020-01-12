using UnityEngine;
using System.Collections;
using System.Diagnostics;

abstract public class BaseGameEntity : MonoBehaviour {
	//every entity must have a unique identifying number
	protected int id;
	//this is the next valid ID. Each time a BaseGameEntity is instantiated
	//this value is updated
	static protected int nextValidID = 0;
    //health of the unit
    protected int maxHealth;
    public int health;
    protected int MaxHealth {
        get { return maxHealth; }
        set {
            maxHealth = value;
            health = value;
        }
    }
    protected uint maxStamina;
    public uint stamina;
    protected uint MaxStamina
    {
        get { return maxStamina; }
        set
        {
            maxStamina = value;
            stamina = value;
        }
    }

    public float maxSpeed;
    public float move_speed;
    public float turn_speed;


    public bool dead;
    public float attackCooldown;
    protected float cooldown = 0;
	
	public BaseGameEntity (int id)
	{
		this.ID = id;
	}

	// Use this for initialization
	public abstract void Start (); 
	
	
	
	// Update is called once per frame
	public abstract void Update ();
	
	
	public int ID {
		get { return id; }
		//this must be called within the constructor to make sure the ID is set
		//correctly. It verifies that the value passed to the method is greater
		//or equal to the next valid ID, before setting the ID and incrementing
		//the next valid ID
		set {
			// Debug.Assert (value >= nextValidID, "<BaseGameEntity::SetID>: invalid ID");
			id = value;
			nextValidID = id + 1;
		}
	}

    public virtual void Respawn() { }

    public virtual void ReceiveAttack(BaseGameEntity attacker, int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            dead = true;
        }
    }

    public virtual void Heal(int amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }

    public virtual void DoAttack(BaseGameEntity receiver, int damage)
    {
        receiver.ReceiveAttack(this, damage);
        cooldown = attackCooldown;
    }

    public virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "Collider")
        {
            this.ReceiveAttack(GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<BaseGameEntity>(), 10);
        }
    }

    public abstract  bool HandleMessage (Telegram telegram);
}

