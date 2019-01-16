using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class CustomThreadPool : MonoBehaviour
{
    public delegate void PoolTask();

    private static List<PoolTask> Tasks = new List<PoolTask>();

    public int ThreadsCount = 4;

    // Use this for initialization
    void Start()
    {
		ThreadPool.SetMinThreads(2, 2);
		ThreadPool.SetMaxThreads(ThreadsCount, ThreadsCount);
    }

    void OnDisable()
    {
    }

    public static void AddTask(PoolTask task)
    {
        lock (Tasks)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state) { task(); } ));
        }
    }

    void OnApplicationQuit()
    {
    }
}
