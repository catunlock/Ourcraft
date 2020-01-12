using System;
using UnityEngine;

//All the states that can be assigned to the Animal class
public class LookingForResources : State<Animal>
{
    static readonly LookingForResources instance = new LookingForResources();
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static LookingForResources()
    {
    }

    LookingForResources()
    {
    }
    //this is a singleton
    public static LookingForResources Instance { get { return instance; } }

    public override void Enter(Animal animal)
    {
        animal.cSteeringWander.enabled = true;
        animal.cSteeringEvasion.enabled = false;
        animal.cSteeringCohesion.enabled = true;
        animal.PlayWanderingClip();
    }

    public override void Execute(Animal animal)
    {
        if (animal.isDead())
        {
            animal.PlayDeathClip();
            animal.gameObject.SetActive(false);
            if (animal.KilledByZombie) GameObject.Instantiate(animal.zombiePrefab, animal.transform.position, animal.transform.rotation);
            else GameObject.FindGameObjectWithTag("Player").GetComponent<BaseGameEntity>().Heal(100);
        }

        animal.animator.SetFloat("MoveSpeed", animal.biped.Speed - 0.1f);

        if (!animal.audio.isPlaying && UnityEngine.Random.value > 0.95) animal.PlayWanderingClip();

        // check if the animal has been attacked in the last iteration
        if (animal.HasBeenAttacked)
        {
            animal.animator.SetBool("jump", true);
            Vector3 dir = animal.transform.position - animal.cSteeringEvasion.Menace.transform.position;
            animal.rb.AddForce(Vector3.up * 50 + new Vector3(dir.x, 0, dir.z) * 50);
            animal.GetFSM().ChangeState(EvadeAttacker.Instance);
        }
        // if everything is fine, try to find a partner to reproduce
        else
        {
            if (animal.HasPartner())
            {
                animal.GetFSM().ChangeState(Reproduce.Instance);
            }
            else if (animal.CanReproduce())
            {
                Animal a = animal.SearchPartner();
                if (a != null)
                {
                    a.SetPartner(animal);
                    animal.SetPartner(a);
                    animal.transform.LookAt(a.transform);
                    a.transform.LookAt(animal.transform);
                }
            }
        }
    }

    public override void Exit(Animal animal)
    {

    }

    public override bool OnMessage(Animal animal, Telegram telegram)
    {
        return false;
    }
}


public class EvadeAttacker : State<Animal>
{
    static readonly EvadeAttacker instance = new EvadeAttacker();
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static EvadeAttacker()
    {
    }

    EvadeAttacker()
    {
    }
    //this is a singleton	
    public static EvadeAttacker Instance { get { return instance; } }

    public override void Enter(Animal animal)
    {
        animal.cSteeringWander.enabled = false;
        animal.cSteeringEvasion.enabled = true;
        animal.cSteeringCohesion.enabled = true;
        animal.StartRunning();
        animal.PlayRunningClip();
    }

    public override void Execute(Animal animal)
    {
        if (animal.isDead())
        {
            animal.PlayDeathClip();
            animal.gameObject.SetActive(false);
            if (animal.KilledByZombie) GameObject.Instantiate(animal.zombiePrefab, animal.transform.position, animal.transform.rotation);
            else GameObject.FindGameObjectWithTag("Player").GetComponent<BaseGameEntity>().Heal(100);
        }

        // check if the animal has been attacked in the last iteration
        if (animal.HasBeenAttacked)
        {
            animal.animator.SetBool("jump", true);
            Vector3 dir = animal.transform.position - animal.cSteeringEvasion.Menace.transform.position;
            animal.rb.AddForce(Vector3.up * 50 + new Vector3(dir.x, 0, dir.z) * 50);
        }

        animal.animator.SetFloat("MoveSpeed", animal.biped.Speed - 0.1f);

        if (!animal.audio.isPlaying && UnityEngine.Random.value > 0.90) animal.PlayRunningClip();

        Vector3 animalCurrPos = animal.biped.Position;
        Vector3 targetCurrPos = animal.cSteeringEvasion.Menace.transform.position;

        float distance = Vector3.Distance(animalCurrPos, targetCurrPos);

        if (distance > Animal.SafeDistance || animal.cSteeringEvasion.Menace.GetComponent<BaseGameEntity>().dead)
        {
            animal.GetFSM().ChangeState(LookingForResources.Instance);
        }
     
    }

    public override void Exit(Animal animal)
    {
        animal.StopRunning();
    }

    public override bool OnMessage(Animal animal, Telegram telegram)
    {
        return false;
    }
}

//State in which two animals are reproducing with eachother
public class Reproduce : State<Animal>
{
    static readonly Reproduce instance = new Reproduce();
    float itime;
    static Reproduce()
    {
    }

    Reproduce()
    {
    }
    //this is a singleton
    public static Reproduce Instance { get { return instance; } }

    public override void Enter(Animal animal)
    {
        animal.cSteeringWander.enabled = false;
        animal.cSteeringEvasion.enabled = false;
        animal.cSteeringCohesion.enabled = false;
        itime = Time.time;
    }

    public override void Execute(Animal animal)
    {
        if (animal.isDead())
        {
            animal.PlayDeathClip();
            animal.gameObject.SetActive(false);
            if (animal.KilledByZombie) GameObject.Instantiate(animal.zombiePrefab, animal.transform.position, animal.transform.rotation);
            else GameObject.FindGameObjectWithTag("Player").GetComponent<BaseGameEntity>().Heal(100);
        }

        if (animal.GetPartner() == null || animal.GetPartner().dead)
        {
            animal.RemovePartner();
            animal.GetFSM().ChangeState(LookingForResources.Instance);
        }

        if (animal.HasBeenAttacked)
        {
            animal.animator.SetBool("jump", true);
            Vector3 dir = animal.transform.position - animal.cSteeringEvasion.Menace.transform.position;
            animal.rb.AddForce(Vector3.up * 50 + new Vector3(dir.x, 0, dir.z) * 50);
            animal.GetPartner().RemovePartner();
            animal.GetFSM().ChangeState(EvadeAttacker.Instance);
        }

        if (Time.time - itime > Animal.reproductionTime)
        {
            Vector3 pos = Vector3.Lerp(animal.transform.position, animal.GetPartner().transform.position, 0.4f);
            if (UnityEngine.Random.value > 0.5f)
            {
                GameObject son = GameObject.Instantiate(animal.gameObject, pos, Quaternion.identity);
                son.transform.localScale *= 0.25f;
            }

            animal.RemovePartner();
            animal.GetFSM().ChangeState(LookingForResources.Instance);
        }
    }

    public override void Exit(Animal animal)
    {

    }

    public override bool OnMessage(Animal animal, Telegram telegram)
    {
        return false;
    }
}