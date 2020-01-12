using System;
using UnityEngine;

//All the states that can be assigned to the Zombie class
public class LookingForLivingBeing : State<Zombie>
{
    static readonly LookingForLivingBeing instance = new LookingForLivingBeing();
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static LookingForLivingBeing()
    {
    }

    LookingForLivingBeing()
    {
    }
    //this is a singleton	
    public static LookingForLivingBeing Instance { get { return instance; } }

    public override void Enter(Zombie zombie)
    {
        zombie.cSteeringWander.enabled = true;
        zombie.cSteeringPursuit.enabled = false;
        zombie.PlaySearchingClip();
    }

    public override void Execute(Zombie zombie)
    {
        if (zombie.isDead())
        {
            zombie.GetFSM().ChangeState(Die.Instance);
            return;
        }

        zombie.animator.SetFloat("MoveSpeed", zombie.biped.Speed + 0.9f);

        if (!zombie.audio.isPlaying && UnityEngine.Random.value > 0.95) zombie.PlaySearchingClip();
        
        // check if a target has been found by senses
        if (zombie.HasTarget())
        {
            zombie.GetFSM().ChangeState(FollowLivingBeing.Instance);
            // Insert change of animation here
        }
    }

    public override void Exit(Zombie zombie)
    {

    }

    public override bool OnMessage(Zombie zombie, Telegram telegram)
    {
        return false;
    }
}


public class FollowLivingBeing : State<Zombie>
{
    static readonly FollowLivingBeing instance = new FollowLivingBeing();
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static FollowLivingBeing()
    {
    }

    FollowLivingBeing()
    {
    }
    //this is a singleton	
    public static FollowLivingBeing Instance { get { return instance; } }

    public override void Enter(Zombie zombie)
    {
        zombie.cSteeringWander.enabled = false;
        zombie.cSteeringPursuit.enabled = true;
        zombie.PlayFollowingClip();
    }

    public override void Execute(Zombie zombie)
    {
        if (zombie.isDead())
        {
            zombie.GetFSM().ChangeState(Die.Instance);
            return;
        }
        
        if (!zombie.audio.isPlaying && UnityEngine.Random.value > 0.90) zombie.PlayFollowingClip();

        if (zombie.HasTarget())
        {
            zombie.animator.SetFloat("MoveSpeed", zombie.biped.Speed + 0.9f);

            Vector3 zombieCurrPos = zombie.biped.Position;
            Vector3 targetCurrPos = zombie.GetTarget().transform.position;

            float distance = Vector3.Distance(zombieCurrPos, targetCurrPos);
            
            if (distance <= zombie.AttackRadius * 0.7 && zombie.CanAttack())
            {
                zombie.animator.SetBool("Attack", true);
                zombie.GetFSM().ChangeState(AttackLivingBeing.Instance);
            }
            else
            {
                zombie.animator.SetBool("Attack", false);
                if (distance > zombie.DetectionRadius)
                {
                    zombie.ClearTarget();
                }
            }
        }
        else
        {
            zombie.GetFSM().ChangeState(LookingForLivingBeing.Instance);
        }
    }

    public override void Exit(Zombie zombie)
    {

    }

    public override bool OnMessage(Zombie zombie, Telegram telegram)
    {
        return false;
    }
}


public class AttackLivingBeing : State<Zombie>
{
    static readonly AttackLivingBeing instance = new AttackLivingBeing();
    bool hasAttacked = false;
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static AttackLivingBeing()
    {
    }

    AttackLivingBeing()
    {
    }
    //this is a singleton	
    public static AttackLivingBeing Instance { get { return instance; } }

    public override void Enter(Zombie zombie)
    {
        zombie.cSteeringWander.enabled = false;
        zombie.cSteeringPursuit.enabled = false;
        zombie.PlayAttackClip();
    }

    public override void Execute(Zombie zombie)
    {
        if (zombie.isDead())
        {
            zombie.GetFSM().ChangeState(Die.Instance);
            return;
        }

        AnimatorStateInfo currentStateInfo = zombie.animator.GetCurrentAnimatorStateInfo(0);
           
        // Check if the animation being played is the attack (sometimes it takes time to transition)
        // Then check if enough percentage of the animation has been played to hurt the target
        if (!hasAttacked && currentStateInfo.IsName("zombie_attack") && currentStateInfo.normalizedTime > 0.25)
        {
            Vector3 zombieCurrPos = zombie.biped.Position;
            Vector3 targetCurrPos = zombie.GetTarget().transform.position;

            float distance = Vector3.Distance(zombieCurrPos, targetCurrPos);
            
            // Check if the target is still at enough distance after launching the attack
            if (distance <= zombie.AttackRadius)
            {
                zombie.DoAttack(zombie.GetTarget(), zombie.AttackDamage);
            }

            hasAttacked = true; // mark that the attack has already been sent
        }

        // if animation is about to finish, swap the state to "following" again
        if (hasAttacked && currentStateInfo.normalizedTime > 0.9)
        {
            hasAttacked = false;
            zombie.GetFSM().ChangeState(FollowLivingBeing.Instance);
        }
    }

    public override void Exit(Zombie zombie)
    {
        hasAttacked = false;
    }

    public override bool OnMessage(Zombie zombie, Telegram telegram)
    {
        return false;
    }
}


public class Die : State<Zombie>
{
    static readonly Die instance = new Die();

    static Die()
    {
    }

    Die()
    {
    }

    public static Die Instance { get { return instance; } }

    public override void Enter(Zombie zombie)
    {
        zombie.cSteeringWander.enabled = false;
        zombie.cSteeringPursuit.enabled = false;
        zombie.animator.SetBool("Dead", true);
        zombie.PlayDeathClip();
    }

    public override void Execute(Zombie zombie)
    {
        
    }

    public override void Exit(Zombie zombie)
    {

    }

    public override bool OnMessage(Zombie zombie, Telegram telegram)
    {
        return false;
    }
}
