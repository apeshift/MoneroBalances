using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RokitFramework
{
  public abstract class ServiceManagerBase
  {

    protected string InstanceName { get; private set; }

    protected abstract List<WorkProcessor> Processors { get; }

    public abstract IWorkQueue<T> GetWorkQueue<T>(string name);

    protected ServiceManagerBase(string instanceName)
    {
      InstanceName = instanceName;
    }

    private void RefreshProcessor(WorkProcessor processor)
    {
      Thread.CurrentThread.Name = "RefreshProcessor: " + processor.ProcessorName;

      try
      {

        processor.Process();
      }
      catch { }

      if (processor.RefreshInterval != 0m)
      {
        decimal interval = (processor.RefreshInterval > 0m) ? processor.RefreshInterval : ((1) * -processor.RefreshInterval);
        processor.RunTimer((int)(interval * 1000m));
      }
    }


    public virtual void Start()
    {

   //   ServiceSettings.Current.Log(this.InstanceName + ": Service Manager: Starting Processors", TraceLevel.Info);

      foreach (WorkProcessor processor in this.Processors)
      {
        processor.Start();
        if (processor.RefreshInterval != 0m)
        {
          processor.RunTimer(new WorkProcessor.WorkTimerCallback(this.RefreshProcessor), 5000);
        }
      }

    //  ServiceSettings.Current.Log(this.InstanceName + ": Service Manager: Start Processors Complete", TraceLevel.Info);
    }

    public virtual void Stop()
    {
    //  ServiceSettings.Current.Log(this.InstanceName + ": Service Manager: Stopping Processors", TraceLevel.Info);

      foreach (WorkProcessor processor in this.Processors)
      {
        processor.StopTimer();
        processor.Stop();
      }

   //   ServiceSettings.Current.Log(this.InstanceName + ": Service Manager: Stop Processors Complete", TraceLevel.Info);
    }

  }

  public abstract class ServiceInstanceBase : ServiceBase
  {
    private static bool IsStopped = true;
    private static bool IsService = true;

    ServiceManagerBase[] _ServiceManagers = null;
    Thread _StopServicesThread = null;


    public abstract TextWriterTraceListener TraceListener { get; set; }
    public abstract ServiceManagerBase[] ServiceStart();
    public abstract void ServiceStop();


    static ServiceInstanceBase()
    {
      ServicePointManager.Expect100Continue = true;
      ServicePointManager.UseNagleAlgorithm = true;
      ServicePointManager.MaxServicePoints = 0;
      ServicePointManager.MaxServicePointIdleTime = 2000;
      ServicePointManager.DefaultConnectionLimit = 100;
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      return true;
    }

    protected ServiceInstanceBase()
    {
      _StopServicesThread = new Thread(new ThreadStart(this.StopServices));
      _StopServicesThread.Name = "StopServices";
    }

    public void DebugStart(string[] args)
    {
      ServiceInstanceBase.IsService = false;

      this.OnStart(args);

      int intTimeout = System.Threading.Timeout.Infinite;

      if (args.Length > 2)
      {
        int.TryParse(args[2], out intTimeout);
      }

      System.Threading.Thread.Sleep(intTimeout);
      this.DebugStop();
    }

    public void DebugStop()
    {
      this.OnStop();
    }

    protected override void OnStart(string[] args)
    {

      ServiceInstanceBase.IsStopped = false;


      _ServiceManagers = this.ServiceStart();


      try
      {

        foreach (ServiceManagerBase manager in _ServiceManagers)
        {
          manager.Start();
        }

      }
      catch (Exception ex)
      {
        try
        {
        //  this.EventLog.WriteEntry("Error in " + ServiceSettings.Current.AppName + ": \n" + ex.Message, System.Diagnostics.EventLogEntryType.Error);
        }
        catch { }

        Environment.Exit(-1);
      }


      base.OnStart(args);
    }


    protected override void OnStop()
    {
     // ServiceSettings.Current.Log(ServiceSettings.Current.AppName + " Service: Stopping Service", TraceLevel.Info);

      _StopServicesThread.Start();

      DateTime stopStart = DateTime.Now;

      while (!ServiceInstanceBase.IsStopped)
      {
        if (DateTime.Now > stopStart.AddSeconds(60))
        {
          Environment.Exit(-1);
          break;
        }

        try
        {
          if (ServiceInstanceBase.IsService)
          {
            this.RequestAdditionalTime(500);
          }
        }
        catch { }
        System.Threading.Thread.Sleep(400);
      }

      try
      {
        if (ServiceInstanceBase.IsService)
        {
          this.RequestAdditionalTime(1000);
        }
      }
      catch { }

      base.OnStop();

    //  ServiceSettings.Current.Log(ServiceSettings.Current.AppName + " Service: Stop Service Complete", TraceLevel.Info);

      this.ServiceStop();
    }

    private void StopServices()
    {


      foreach (ServiceManagerBase manager in _ServiceManagers)
      {
        manager.Stop();
      }


      ServiceInstanceBase.IsStopped = true;
    }

  }
}
