using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanMerchantRest : State<HumanMerchant>
{
    static readonly HumanMerchantRest instance = new HumanMerchantRest();

    static HumanMerchantRest() { }

    HumanMerchantRest() { }

    public static HumanMerchantRest Instance { get { return instance; } }

    public override void Enter(HumanMerchant entity)
    {
        if (Vector3.Distance(entity.initPos, entity.biped.Position) > 0.05f)
        {
            entity.cSteerPoint.enabled = true;
        }
    }

    public override void Execute(HumanMerchant entity)
    {
        // if has a pursuer then escape!
        if (entity.health <= 0)
        {
            entity.GetFSM().ChangeState(HumanMerchantDeath.Instance);
            return;
        }

        if (entity.HasPursuer())
        {
            entity.GetFSM().ChangeState(HumanMerchantFleeFromAggressorState.Instance);
            return;
        }

        if (entity.GetPlayerInteracted())
        {
            entity.GetFSM().ChangeState(HumanMerchantInteractPlayer.Instance);
            return;
        }

        if (!entity.cSteerPoint.enabled)
        {
            if (entity.GetIdleTimer() <= 0f)
            {
                int posInd;

                posInd = entity.ChangeIdlePos();

                if (posInd == -1)
                    entity.cSteerPoint.TargetPoint = entity.initPos;
                else
                    entity.cSteerPoint.TargetPoint = entity.transform.parent.TransformPoint(entity.movePos[posInd].x, entity.movePos[posInd].y, entity.movePos[posInd].z);
                entity.cSteerPoint.enabled = true;
            }
        }
        else
        {
            float tarOri;
            int posInd = entity.GetCurrentIdlePosIndex();

            if (posInd >= 0)
                tarOri = entity.movePos[posInd].w;
            else
                tarOri = entity.initOri;

            if (entity.cSteerPoint.ReportedArrival)
            {
                entity.anim.SetFloat("Speed", 0.0f);

                if (entity.gameObject.transform.eulerAngles.y == tarOri)
                    entity.cSteerPoint.enabled = false;
                else
                {
                    float rotation = tarOri - entity.transform.eulerAngles.y;

                    if (Mathf.Abs(rotation) <= entity.turn_speed * Time.deltaTime)
                    {
                        Vector3 ori = new Vector3(0.0f, tarOri, 0.0f);

                        entity.gameObject.transform.eulerAngles = ori;
                        entity.biped.transform.eulerAngles = ori;

                    }
                    else
                    {
                        rotation = Mathf.Sign(rotation) * entity.turn_speed * Time.deltaTime;

                        entity.gameObject.transform.Rotate(0.0f, rotation, 0.0f);
                        entity.biped.transform.Rotate(0.0f, rotation, 0.0f);
                    }
                }
            }
            else if (entity.cSteerPoint.ReportedMove)
            {
                entity.anim.SetFloat("Speed", entity.biped.Speed);
            }
        }
    }

    public override void Exit(HumanMerchant entity)
    {
    }

    public override bool OnMessage(HumanMerchant entity, Telegram telegram)
    {
        return false;
    }
}

public class HumanMerchantFleeFromAggressorState : State<HumanMerchant>
{
    static readonly HumanMerchantFleeFromAggressorState instance = new HumanMerchantFleeFromAggressorState();

    static HumanMerchantFleeFromAggressorState() { }

    HumanMerchantFleeFromAggressorState() { }

    public static HumanMerchantFleeFromAggressorState Instance { get { return instance; } }

    public override void Enter(HumanMerchant entity)
    {
        entity.cSteerPoint.enabled = false;
        entity.cSteerFear.enabled = true;
    }

    public override void Execute(HumanMerchant entity)
    {
        if (entity.health <= 0)
        {
            // Turn into zombie
            entity.GetFSM().ChangeState(HumanMerchantDeath.Instance);
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

            entity.cSteerFear.SetMaxEvents(p.Count);

            for (int i = 0; i < p.Count; ++i)
                entity.cSteerFear.AddEvent(p[i].transform.position);
        }

        entity.anim.SetFloat("Speed", entity.biped.Speed);
    }

    public override void Exit(HumanMerchant entity)
    {
        entity.cSteerFear.enabled = false;
    }

    public override bool OnMessage(HumanMerchant entity, Telegram telegram)
    {
        return false;
    }

}


public class HumanMerchantDeath : State<HumanMerchant>
{
    static readonly HumanMerchantDeath instance = new HumanMerchantDeath();

    static HumanMerchantDeath() { }

    HumanMerchantDeath() { }

    public static HumanMerchantDeath Instance { get { return instance; } }

    public override void Enter(HumanMerchant entity)
    {
        entity.anim.SetBool("Dead", true);
    }

    public override void Execute(HumanMerchant entity)
    {
        if (entity.IsTimeRiseIntoZombie())
            GameObject.Instantiate(entity.zombiePrefab, entity.transform.position, entity.transform.rotation);
    }

    public override void Exit(HumanMerchant entity)
    {

    }

    public override bool OnMessage(HumanMerchant entity, Telegram telegram)
    {
        return false;
    }
}

public class HumanMerchantInteractPlayer : State<HumanMerchant>
{
    static readonly HumanMerchantInteractPlayer instance = new HumanMerchantInteractPlayer();

    static HumanMerchantInteractPlayer() { }

    HumanMerchantInteractPlayer() { }

    public static HumanMerchantInteractPlayer Instance { get { return instance; } }

    public override void Enter(HumanMerchant entity)
    {
        // Disable rest and enable other thing
        entity.cSteerPoint.enabled = false;
        //entity.sRandWords.enabled = true;
        entity.anim.Play("Interact");
        //entity.ResetTalkTimer();
        //entity.PlayRandomSalutationAudio();
    }

    public override void Execute(HumanMerchant entity)
    {
        // Nothing at first, keep dialogue or other thing, as well as control animations for talking, etc
        if (!entity.GetPlayerInteracted())
            entity.GetFSM().RevertToPreviousState();

        //entity.UpdateTalkTime();
    }

    public override void Exit(HumanMerchant entity)
    {
        //entity.PlayRandomFarewellAudio();
        //entity.sRandWords.RestoreDefault();
        //entity.sRandWords.enabled = false;
    }

    public override bool OnMessage(HumanMerchant entity, Telegram telegram)
    {
        return false;
    }
}

