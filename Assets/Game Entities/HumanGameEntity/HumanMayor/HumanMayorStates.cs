using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanMayorRest : State<HumanMayor>
{
    static readonly HumanMayorRest instance = new HumanMayorRest();

    static HumanMayorRest() { }

    HumanMayorRest() { }

    public static HumanMayorRest Instance { get { return instance; } }

    public override void Enter(HumanMayor entity)
    {
        if (Vector3.Distance(entity.initPos, entity.biped.Position) > 0.05f)
        {
            entity.cSteerPoint.enabled = true;
        }
    }

    public override void Execute(HumanMayor entity)
    {
        if (entity.health <= 0)
        {
            entity.GetFSM().ChangeState(HumanMayorDeath.Instance);
            return;
        }

        // if has a pursuer then escape!
        if (entity.HasPursuer())
        {
            entity.GetFSM().ChangeState(HumanMayorFleeFromAggressorState.Instance);
            return;
        }

        if (entity.GetPlayerInteracted())
        {
            entity.GetFSM().ChangeState(HumanMayorInteractPlayer.Instance);
            return;
        }

        if (!entity.cSteerPoint.enabled)
        {
            entity.restTimer -= Time.deltaTime;

            if (entity.restTimer <= 0.0f)
            {
                string restString;

                if (entity.currentBaseState.fullPathHash != HumanMayor.idleState)
                {
                    if (entity.anim.IsInTransition(0))
                    {
                        entity.anim.SetBool("Rest 1", false);
                        entity.anim.SetBool("Rest 2", false);
                        entity.anim.SetBool("Rest 3", false);
                        entity.restTimer = Random.Range(HumanMayor.restMinTime, HumanMayor.restMaxTime);
                    }
                }
                else if (!entity.anim.GetBool("Rest 1") && !entity.anim.GetBool("Rest 2")
                    && !entity.anim.GetBool("Rest 3"))
                {
                    int rest = Random.Range(1, 4);
                    restString = "Rest " + rest.ToString();
                    entity.anim.SetBool(restString, true);
                }
            }
        }
        else
        {
            if (entity.cSteerPoint.ReportedArrival)
            {
                entity.anim.SetFloat("Speed", 0.0f);

                if (entity.gameObject.transform.eulerAngles.y == entity.initOri)
                    entity.cSteerPoint.enabled = false;
                else
                {
                    float rotation = entity.initOri - entity.transform.eulerAngles.y;

                    if (Mathf.Abs(rotation) <= entity.turn_speed * Time.deltaTime)
                    {
                        Vector3 ori = new Vector3(0.0f, entity.initOri, 0.0f);

                        entity.gameObject.transform.eulerAngles = ori;
                        entity.biped.transform.eulerAngles = ori;
                        entity.anim.SetFloat("Direction", 0.0f);
                    }
                    else
                    {
                        entity.anim.SetFloat("Direction", rotation);
                        rotation = Mathf.Sign(rotation) * entity.turn_speed * Time.deltaTime;

                        entity.gameObject.transform.Rotate(0.0f, rotation, 0.0f);
                        entity.biped.transform.Rotate(0.0f, rotation, 0.0f);
                    }
                }
            }
            else if (entity.cSteerPoint.ReportedMove)
            {
                entity.anim.SetFloat("Speed", entity.biped.Speed);
                entity.anim.SetFloat("Direction", entity.biped.OrientationVelocity.y);
            }
        }
    }

    public override void Exit(HumanMayor entity)
    {
        entity.anim.SetBool("Rest 1", false);
        entity.anim.SetBool("Rest 2", false);
        entity.anim.SetBool("Rest 3", false);
    }

    public override bool OnMessage(HumanMayor entity, Telegram telegram)
    {
        return false;
    }
}

public class HumanMayorFleeFromAggressorState : State<HumanMayor>
{
    static readonly HumanMayorFleeFromAggressorState instance = new HumanMayorFleeFromAggressorState();

    static HumanMayorFleeFromAggressorState() { }

    HumanMayorFleeFromAggressorState() { }

    public static HumanMayorFleeFromAggressorState Instance { get { return instance; } }

    public override void Enter(HumanMayor entity)
    {
        entity.cSteerPoint.enabled = false;
        entity.cSteerFear.enabled = true;
    }

    public override void Execute(HumanMayor entity)
    {
        if (entity.health <= 0)
        {
            // Turn into zombie
            entity.GetFSM().ChangeState(HumanMayorDeath.Instance);
            return;
        }
        else if (!entity.HasPursuer())
        {
            entity.fleeStopTimer -= Time.deltaTime;

            if (entity.fleeStopTimer <= 0)
            {
                entity.GetFSM().RevertToPreviousState();
                return;
            }
        }
        else
        {
            List<GameObject> p = entity.GetPursuers();

            entity.fleeStopTimer = entity.fleeStopMaxTime;

            /*p.Sort(delegate(GameObject a, GameObject b)
            {
                return (entity.gameObject.transform.position - a.transform.position).sqrMagnitude.CompareTo(
                    (entity.gameObject.transform.position - b.transform.position).sqrMagnitude);
            });

            if (p.Count > 5)
                p.RemoveRange(5, p.Count - 5);
            */

            entity.cSteerFear.SetMaxEvents(p.Count);

            for (int i = 0; i < p.Count; ++i)
                entity.cSteerFear.AddEvent(p[i].transform.position);
        }

        entity.anim.SetFloat("Speed", entity.biped.Speed);
        entity.anim.SetFloat("Direction", entity.biped.OrientationVelocity.y);
    }

    public override void Exit(HumanMayor entity)
    {
        entity.cSteerFear.enabled = false;
    }

    public override bool OnMessage(HumanMayor entity, Telegram telegram)
    {
        return false;
    }

}


public class HumanMayorDeath : State<HumanMayor>
{
    static readonly HumanMayorDeath instance = new HumanMayorDeath();

    static HumanMayorDeath() { }

    HumanMayorDeath() { }

    public static HumanMayorDeath Instance { get { return instance; } }

    public override void Enter(HumanMayor entity)
    {
        entity.PlayDeathAudio();
        entity.anim.SetBool("Dead", true);
    }

    public override void Execute(HumanMayor entity)
    {
        if (entity.IsTimeRiseIntoZombie())
            GameObject.Instantiate(entity.zombiePrefab, entity.transform.position, entity.transform.rotation);
    }

    public override void Exit(HumanMayor entity)
    {

    }

    public override bool OnMessage(HumanMayor entity, Telegram telegram)
    {
        return false;
    }
}

public class HumanMayorInteractPlayer : State<HumanMayor>
{
    static readonly HumanMayorInteractPlayer instance = new HumanMayorInteractPlayer();

    static HumanMayorInteractPlayer() { }

    HumanMayorInteractPlayer() { }

    public static HumanMayorInteractPlayer Instance { get { return instance; } }

    public override void Enter(HumanMayor entity)
    {
        // Disable rest and enable other thing
        entity.cSteerPoint.enabled = false;
        entity.sRandWords.enabled = true;
        entity.anim.SetBool("Interacting", true);
        entity.ResetTalkTimer();
        entity.PlayRandomSalutationAudio();
    }

    public override void Execute(HumanMayor entity)
    {
        // Nothing at first, keep dialogue or other thing, as well as control animations for talking, etc
        if (!entity.GetPlayerInteracted())
            entity.GetFSM().RevertToPreviousState();

        entity.UpdateTalkTime();
    }

    public override void Exit(HumanMayor entity)
    {
        entity.PlayRandomFarewellAudio();
        entity.anim.SetBool("Interacting", false);
        entity.sRandWords.RestoreDefault();
        entity.sRandWords.enabled = false;
    }

    public override bool OnMessage(HumanMayor entity, Telegram telegram)
    {
        return false;
    }
}

