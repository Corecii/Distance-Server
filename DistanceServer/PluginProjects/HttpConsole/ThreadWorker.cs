using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


public class ThreadTask<T>
{
    public delegate T ThreadTaskDelegate(ThreadTask<T> task);

    public enum ThreadTaskState
    {
        NotStarted,
        Running,
        Responded,
        Error
    };
    
    public ManualResetEventSlim ResetEvent = new ManualResetEventSlim();
    public T Request;
    public ThreadTaskState State;
    public T Response;
    public Exception Error;

    public ThreadTask(T request)
    {
        Request = request;
    }

    public void Respond(ThreadTaskDelegate taskDelegate)
    {
        try
        {
            Response = taskDelegate(this);
            State = ThreadTaskState.Responded;
        }
        catch (Exception e)
        {
            Error = e;
            State = ThreadTaskState.Error;
        }
        ResetEvent.Set();
    }

    public void WaitForResponse()
    {
        ResetEvent.Wait();
    }
}

public class ThreadWorker<T>
{
    public Queue<ThreadTask<T>> Tasks = new Queue<ThreadTask<T>>();
    public object TasksLock = new object();

    public Queue<ThreadTask<T>> Responses = new Queue<ThreadTask<T>>();
    public object ResponsesLock = new object();
    public bool QueueResponses = true;

    public Queue<string> OutputLines = new Queue<string>();
    public object OutputLock = new object();

    public void Output(string line)
    {
        lock (OutputLock)
        {
            OutputLines.Enqueue(line);
        }
    }

    private void LogOutput()
    {
        while (true)
        {
            lock (OutputLock)
            {
                if (OutputLines.Count == 0)
                {
                    break;
                }
                Log.Debug(OutputLines.Dequeue());
            }
        }
    }

    Exception threadException = null;

    public ThreadTask<T> AddTask(T info)
    {
        ThreadTask<T> task = new ThreadTask<T>(info);
        lock (TasksLock)
        {
            Tasks.Enqueue(task);
        }
        return task;
    }

    public delegate void SendResponseDelegate(ThreadTask<T> task);
    public void SendResponses(SendResponseDelegate del)
    {
        while (true)
        {
            ThreadTask<T> response;
            lock (ResponsesLock)
            {
                if (Responses.Count == 0)
                {
                    break;
                }
                response = Responses.Dequeue();
            }
            Output($"Dequeued response... {response.State}");
            if (response.State == ThreadTask<T>.ThreadTaskState.Responded)
            {
                Output("Sending response");
                del(response);
                Output("Sent response");
            }
        }
    }

    public ThreadWorker()
    {
    }

    public void Respond(ThreadTask<T>.ThreadTaskDelegate messageDelegate)
    {
        LogOutput();
        if (threadException != null)
        {
            Log.Error(threadException);
        }
        while (true)
        {
            ThreadTask<T> task;
            lock (TasksLock)
            {
                if (Tasks.Count == 0)
                {
                    break;
                }
                task = Tasks.Dequeue();
            }
            Log.Debug("Responding to task");
            task.Respond(messageDelegate);
            if (task.State == ThreadTask<T>.ThreadTaskState.Error)
            {
                Log.Error(task.Error);
            }
            else if (QueueResponses)
            {
                lock (ResponsesLock)
                {
                    Responses.Enqueue(task);
                }
            }
        }
    }
}