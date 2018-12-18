using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


public class MqTask
{
    public delegate string MqTaskDelegate(MqTask task);

    public enum MqTaskState
    {
        NotStarted,
        Running,
        Responded,
        Error
    };

    public MqTaskDelegate TaskDelegate;
    public NetMqReqRouter Router;
    public byte[] Address;
    public string Request;
    public MqTaskState State;
    public string Response;
    public Exception Error;

    public MqTask(NetMqReqRouter router, byte[] address, string request, MqTaskDelegate taskDelegate)
    {
        Router = router;
        TaskDelegate = taskDelegate;
        Address = address;
        Request = request;
    }

    public void Respond()
    {
        try
        {
            Response = TaskDelegate(this);
            State = MqTaskState.Responded;
        }
        catch (Exception e)
        {
            Error = e;
            State = MqTaskState.Error;
        }
    }
}

public class NetMqReqRouter
{
    public delegate string MessageDelegate(string message);

    public readonly string _socketAddress;
    public readonly TimeSpan _timeout;

    private readonly Thread _listenerWorker;
    private bool _listenerCancelled;

    private readonly MqTask.MqTaskDelegate _messageDelegate;

    private readonly Stopwatch _contactWatch;
    private const long ContactThreshold = 1000;
    public bool Connected;

    internal Queue<MqTask> tasks = new Queue<MqTask>();
    internal object tasksLock = new object();

    internal Queue<MqTask> responses = new Queue<MqTask>();
    internal object responsesLock = new object();

    internal Queue<string> outputLines = new Queue<string>();
    internal object outputLock = new object();

    private void Output(string line)
    {
        lock (outputLock)
        {
            outputLines.Enqueue(line);
        }
    }

    private void LogOutput()
    {
        while (true)
        {
            lock (outputLock)
            {
                if (outputLines.Count == 0)
                {
                    break;
                }
                Log.Debug(outputLines.Dequeue());
            }
        }
    }
    
    Exception threadException = null;

    public void ListenerWork()
    {
        try
        {
            Output("Starting...");
            
            AsyncIO.ForceDotNet.Force();
            Output("AsyncIO forced");
            using (var server = new RouterSocket())
            {
                Output("Using RouterSocket");
                server.Bind(_socketAddress);

                while (!_listenerCancelled)
                {
                    do
                    {
                        Connected = _contactWatch.ElapsedMilliseconds < ContactThreshold;
                        bool more;
                        byte[] address;
                        if (!server.TryReceiveFrameBytes(_timeout, out address, out more) || !more) continue;
                        string empty;
                        if (!server.TryReceiveFrameString(_timeout, out empty, out more) || !more) continue;
                        string message;
                        if (!server.TryReceiveFrameString(_timeout, out message, out more)) continue;
                        Output("Handling request");
                        _contactWatch.Restart();
                        lock (tasksLock)
                        {
                            tasks.Enqueue(new MqTask(this, address, message, _messageDelegate));
                        }
                    } while (false);

                    while (true)
                    {
                        MqTask response;
                        lock (responsesLock)
                        {
                            if (responses.Count == 0)
                            {
                                break;
                            }
                            response = responses.Dequeue();
                        }
                        Output($"Dequeued response... {response.State}");
                        if (response.State == MqTask.MqTaskState.Responded)
                        {
                            Output("Sending response");
                            server.SendMoreFrame(response.Address);
                            server.SendMoreFrameEmpty();
                            server.SendMoreFrame(response.Response);
                            Output("Sent response");
                        }
                    }
                }
            }
            NetMQConfig.Cleanup();
        }
        catch (Exception e)
        {
            threadException = e;
        }
    }

    public NetMqReqRouter(string socketAddress, double timeoutMs, MqTask.MqTaskDelegate messageDelegate)
    {
        _socketAddress = socketAddress;
        _timeout = new TimeSpan((int)(timeoutMs * 10000));
        _messageDelegate = messageDelegate;
        _contactWatch = new Stopwatch();
        _contactWatch.Start();
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        Log.Debug("Starting thread...");
        _listenerCancelled = false;
        _listenerWorker.Start();
        Log.Debug($"State: {_listenerWorker.ThreadState}");
    }

    public void Stop()
    {
        Log.Debug("Stopping thread...");
        _listenerCancelled = true;
        _listenerWorker.Join();
    }

    public void Respond()
    {
        LogOutput();
        if (threadException != null)
        {
            Log.Error(threadException);
        }
        while (true)
        {
            MqTask task;
            lock (tasksLock) {
                if (tasks.Count == 0)
                {
                    break;
                }
                task = tasks.Dequeue();
            }
            Log.Debug("Responding to task");
            task.Respond();
            if (task.State == MqTask.MqTaskState.Error)
            {
                Log.Error(task.Error);
            }
            else
            {
                lock (responsesLock)
                {
                    responses.Enqueue(task);
                }
            }
        }
    }
}