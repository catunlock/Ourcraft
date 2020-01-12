using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanVillagerRest : State<HumanVillager>
{
    static readonly HumanVillagerRest instance = new HumanVillagerRest();

    static HumanVillagerRest() { }

    HumanVillagerRest() { }

    public static HumanVillagerRest Instance { get { return instance; } }

    public override void Enter(HumanVillager entity)
    {
        if (Vector3.Distance(entity.initPos, entity.biped.Position) > 0.05f)
        {
            entity.cSteerPoint.enabled = true;
        }
    }

    public override void Execute(HumanVillager entity)
    {
        // if has a pursuer then escape!
        if (entity.health <= 0)
        {
            entity.GetFSM().ChangeState(HumanVillagerDeath.Instance);
            return;
        }

        if (entity.HasPursuer())
        {
            entity.GetFSM().ChangeState(HumanVillagerFleeFromAggressorState.Instance);
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

    public override void Exit(HumanVillager entity)
    {
    }

    public override bool OnMessage(HumanVillager entity, Telegram telegram)
    {
        return false;
    }
}

public class HumanVillagerFleeFromAggressorState : State<HumanVillager>
{
    static readonly HumanVillagerFleeFromAggressorState instance = new HumanVillagerFleeFromAggressorState();

    static HumanVillagerFleeFromAggressorState() { }

    HumanVillagerFleeFromAggressorState() { }

    public static HumanVillagerFleeFromAggressorState Instance { get { return instance; } }

    public override void Enter(HumanVillager entity)
    {
        entity.cSteerPoint.enabled = false;
        entity.cSteerFear.enabled = true;
    }

    public override void Execute(HumanVillager entity)
    {
        if (entity.health <= 0)
        {
            // Turn into zombie
            entity.GetFSM().ChangeState(HumanVillagerDeath.Instance);
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

    public override void Exit(HumanVillager entity)
    {
        entity.cSteerFear.enabled = false;
    }

    public override bool OnMessage(HumanVillager entity, Telegram telegram)
    {
        return false;
    }

}


public class HumanVillagerDeath : State<HumanVillager>
{
    static readonly HumanVillagerDeath instance = new HumanVillagerDeath();

    static HumanVillagerDeath() { }

    HumanVillagerDeath() { }

    public static HumanVillagerDeath Instance { get { return instance; } }

    public override void Enter(HumanVillager entity)
    {
        entity.anim.SetBool("Dead", true);
    }

    public override void Execute(HumanVillager entity)
    {
        if (entity.IsTimeRiseIntoZombie())
            GameObject.Instantiate(entity.zombiePrefab, entity.transform.position, entity.transform.rotation);
    }

    public override void Exit(HumanVillager entity)
    {

    }

    public override bool OnMessage(HumanVillager entity, Telegram telegram)
    {
        return false;
    }
}

