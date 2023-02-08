using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RokitFramework
{
  public enum ProcessorMode
  {
    Single,
    Continuous
  }

  public abstract class WorkProcessor : IWorkProcessor
  {
    public delegate void WorkBegin(WorkProcessor processor);
    public delegate void WorkEnd(WorkProcessor processor);
    public delegate void WorkTimerCallback(WorkProcessor processor);

    private Thread _WorkThread = null;
    private EventWaitHandle _WorkSignal = null;
    private object _SignalLock = new object();

    protected abstract void Work();

    public event WorkBegin WorkBeginEvent;
    public event WorkEnd WorkEndEvent;

    public delegate void TaskEnd(WorkProcessor processor);
    public event TaskEnd TaskkEndEvent;

    private Timer _RefreshTimer;
    private object _TimerLock = new object();
    private object _ProcessData;

    private ProcessorMode _ProcessorMode;

    public string ProcessorName { get; private set; }

    public ServiceManagerBase ServiceManager { get; set; }

    public virtual decimal RefreshInterval
    {
      get
      {
        return -1m;
      }
    }

    protected WorkProcessor(string name, ApartmentState threadState, ProcessorMode mode)
    {
      _WorkThread = new Thread(new ThreadStart(Work));
      _WorkThread.SetApartmentState(threadState);
      _WorkThread.Name = name;
      this.ProcessorName = name;
      _ProcessorMode = mode;
    }

    protected WorkProcessor(string name) : this(name, ApartmentState.MTA, ProcessorMode.Continuous)
    {

    }

    protected WorkProcessor(string name, ApartmentState threadState) : this(name, threadState, ProcessorMode.Continuous)
    {

    }

    protected WorkProcessor(string name, ProcessorMode mode) : this(name, ApartmentState.MTA, mode)
    {

    }


    private void RefreshTimer_Callback(object state)
    {
      WorkTimerCallback callback = (WorkTimerCallback)state;
      callback(this);
    }

    public virtual void RunTimer(int delay)
    {
      lock (_TimerLock)
      {
        if (_RefreshTimer != null)
        {
          _RefreshTimer.Change(delay, Timeout.Infinite);
        }
      }
    }

    public virtual void RunTimer(WorkTimerCallback callback, int delay)
    {
      lock (_TimerLock)
      {
        if (_RefreshTimer == null)
        {
          _RefreshTimer = new Timer(new TimerCallback(RefreshTimer_Callback), callback, delay, Timeout.Infinite);
        }
        else
        {
          _RefreshTimer.Change(delay, Timeout.Infinite);
        }
      }
    }

    public virtual void StopTimer()
    {
      lock (_TimerLock)
      {
        if (_RefreshTimer != null)
        {
          _RefreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
          _RefreshTimer.Dispose();
          _RefreshTimer = null;
        }
      }
    }

    public virtual void Start()
    {
      _WorkSignal = new EventWaitHandle(false, EventResetMode.ManualReset);

      _WorkThread.Start();
    }

    public virtual void Stop()
    {
      this.Stop(Timeout.Infinite);
    }

    public virtual void Stop(int timeOut)
    {

      if (_WorkThread.IsAlive)
      {
        if (TaskkEndEvent != null)
        {
          TaskkEndEvent(this);
        }


        _WorkThread.Abort();
        lock (_SignalLock)
        {
          _WorkSignal.Set();
        }
        if (!_WorkThread.Join(timeOut))
        {
          return;
        }
      }
      _WorkThread = null;
      lock (_SignalLock)
      {
        _WorkSignal.Close();
        _WorkSignal = null;
      }
    }

    public void Process()
    {
      this.SignalWork();
    }


    public void Process<T>(T processData)
    {
      _ProcessData = processData;
      this.SignalWork();
    }

    protected void SignalWork()
    {
      lock (_SignalLock)
      {
        if (_WorkSignal == null)
        {
          return;
        }
        _WorkSignal.Set();
        if (_ProcessorMode == ProcessorMode.Continuous)
        {
          _WorkSignal.Reset();
        }

      }
    }

    protected void SignalWorkBegin()
    {
      if (WorkBeginEvent != null)
      {
        WorkBeginEvent(this);
      }
    }

    protected void SignalWorkEnd()
    {
      if (WorkEndEvent != null)
      {
        WorkEndEvent(this);
      }
    }

    protected void WorkWait()
    {
      int ret = WaitForSingleObject(_WorkSignal.SafeWaitHandle, Timeout.Infinite);
      lock (_SignalLock)
      {
        Thread.Sleep(100);
      }
    }

    protected void WorkWait<T>(out T processData)
    {
      this.WorkWait();

      processData = (T)_ProcessData;
      _ProcessData = null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int WaitForSingleObject(SafeWaitHandle hHandle, int dwMilliseconds);

  }


  interface IWorkProcessor
  {
    decimal RefreshInterval { get; }
    void Process();
    void Start();
    void Stop();

  }
}
