using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
	private static System.Object ThreadLock = new System.Object();
    private static List<Action> MainThreadTasks = new List<Action>();

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //pending tasks
        lock (ThreadLock)
        {
            foreach (Action a in MainThreadTasks)
            {
                a();
            }
            MainThreadTasks.Clear();
        }

        MainThreadTasks.Clear();
    }

    public static void AddTask(Action action)
    {
        lock (ThreadLock)
        {
            MainThreadTasks.Add(action);
        }
    }
}
