using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class MessageDispatcher : MonoBehaviour
{
    List<Telegram> queue;

    void Discharge(BaseGameEntity receiver, Telegram telegram)
    {
        if (!receiver.HandleMessage(telegram))
        {
            UnityEngine.Debug.Log("Message not handled");
        }
        else
        {
            UnityEngine.Debug.Log("Message handled");
        }
    }

    //	static readonly MessageDispatcher instance = new MessageDispatcher ();
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    void MessageDispacher()
    {
    }


    //this is a singleton
    //	public static MessageDispatcher Instance { get { return instance; } }

    public void DispatchMessage(long time, int senderID, int receiverID, int messageID)
    {
        BaseGameEntity receiver = EntityManager.Instance.GetEntityFromID(receiverID);
        Telegram telegram = new Telegram(time, senderID, receiverID, messageID);
        if (time <= 0.0)
        {
            Discharge(receiver, telegram);
        }
        else
        {
            long currentTime = Stopwatch.GetTimestamp();
            telegram.Time = currentTime + time;
            queue.Add(telegram);
            queue.Sort(CompareByTime);
        }
    }

    static int CompareByTime(Telegram a, Telegram b)
    {
        return a.Time.CompareTo(b.Time);
    }

    public void DispatchDelayedMessages()
    {
        long currentTime = Stopwatch.GetTimestamp();
        while (queue.Count > 0 && queue[0].Time < currentTime && queue[0].Time > 0)
        {
            Telegram telegram = queue[0];
            BaseGameEntity receiver = EntityManager.Instance.GetEntityFromID(telegram.ReceiverID);
            Discharge(receiver, telegram);
            queue.Remove(queue[0]);
        }
    }

    public void Awake()
    {
        queue = new List<Telegram>();
        UnityEngine.Debug.Log("Empieza el dispatcher" + queue);
    }
    public void Update()
    {
        System.Threading.Thread.Sleep(50);
        DispatchDelayedMessages();
    }
}

