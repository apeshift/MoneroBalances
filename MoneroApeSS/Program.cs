using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;


namespace MoneroApeSS
{
  static class Program
  {
    public const string AppName = "MoneroApeSS";


    static void Main()
    {

      string[] args = System.Environment.GetCommandLineArgs();
      Thread.CurrentThread.Name = Program.AppName + "Service";


      if (args.Length > 1)
      {
        if (args[1] == "/r")
        {
          MainService service = new MainService();
          service.DebugStart(args);
        }

        if (args[1] == "/install")
        {
          Console.WriteLine("Installing Service...");
          InstallService();
          Console.WriteLine("Service installed. Press any key to exit.");
        }

        if (args[1] == "/uninstall")
        {
          Console.WriteLine("Unstalling Service...");
          UninstallService();
          Console.WriteLine("Service uinstalled. Press any key to exit.");
        }
      }
      else
      {
        ServiceBase.Run(new MainService());
      }
    }

    private static AssemblyInstaller GetInstaller()
    {
      AssemblyInstaller installer = new AssemblyInstaller(typeof(MainService).Assembly, null);
      installer.UseNewContext = true;
      return installer;
    }

    private static bool IsInstalled()
    {
      using (ServiceController controller =
        new ServiceController(Program.AppName))
      {
        try
        {
          ServiceControllerStatus status = controller.Status;
        }
        catch
        {
          return false;
        }
        return true;
      }
    }

    private static void InstallService()
    {
      if (IsInstalled()) return;

      try
      {
        using (AssemblyInstaller installer = GetInstaller())
        {
          IDictionary state = new Hashtable();
          try
          {
            installer.Install(state);
            installer.Commit(state);
          }
          catch
          {
            try
            {
              installer.Rollback(state);
            }
            catch { }
            throw;
          }
        }
      }
      catch
      {
        throw;
      }
    }

    private static void UninstallService()
    {

      try
      {
        using (AssemblyInstaller installer = GetInstaller())
        {
          IDictionary state = new Hashtable();
          try
          {
            installer.Uninstall(state);
          }
          catch
          {
            throw;
          }
        }
      }
      catch
      {
        throw;
      }
    }

  }

  public partial class MainService : RokitFramework.ServiceInstanceBase
  {
    public override TextWriterTraceListener TraceListener
    {
      get;
      set;
    }

    public override RokitFramework.ServiceManagerBase[] ServiceStart()
    {

      ServicePointManager.Expect100Continue = false;
      ServicePointManager.UseNagleAlgorithm = false;
      ServicePointManager.MaxServicePointIdleTime = 2000;
      ServicePointManager.DefaultConnectionLimit = 5000;


      return new RokitFramework.ServiceManagerBase[] { new ServiceManager("MoneroApeSS") };

    }

    public override void ServiceStop()
    {
      this.TraceListener.Close();
      this.TraceListener = null;
    }
  }

  [RunInstaller(true)]
  public partial class AppInstaller : System.Configuration.Install.Installer
  {
    private ServiceProcessInstaller _ServiceProcessInstaller;
    private ServiceInstaller _ServiceInstaller;

    public AppInstaller()
    {

      this._ServiceProcessInstaller = new ServiceProcessInstaller();
      this._ServiceInstaller = new ServiceInstaller();

      this._ServiceProcessInstaller.Account = ServiceAccount.LocalSystem;
      this._ServiceProcessInstaller.Password = null;
      this._ServiceProcessInstaller.Username = null;

      this._ServiceInstaller.DisplayName = Program.AppName;
      this._ServiceInstaller.ServiceName = Program.AppName;
      this._ServiceInstaller.StartType = ServiceStartMode.Automatic;

      this._ServiceInstaller.AfterInstall += new InstallEventHandler(_ServiceInstaller_AfterInstall);
      this._ServiceInstaller.BeforeUninstall += new InstallEventHandler(_ServiceInstaller_BeforeUninstall);

      this.Installers.AddRange(new Installer[] { this._ServiceProcessInstaller, this._ServiceInstaller });
    }

    void _ServiceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
    {
      Config.StopService(Program.AppName);
    }

    void _ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
    {
      Config.StartService(Program.AppName);
    }


  }
}
