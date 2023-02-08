using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoneroApeSS
{

  public class ApiConfig
  {

    public const string APEAPI_SECRET = "";
    public const string APEAPI_PUBLIC_ID = "";
    public const string APPMQ_CONNECTION_TOKEN = "";
    public const string BLOCKMQ_CONNECTION_TOKEN = "";

    public const string MY_PRIVATE_KEY = "";
    public const string MY_PUB_KEY = "";
    public const string MY_EXTPUB_KEY = "";


    public const int XMR_WALLET_API_1_START_PORT = 14077;
    public const int XMR_WALLET_API_1_MAX_PORTS = 3;
    public const string XMR_SAVED_WALLETS_DIR = "E:\\UserWallets\\";
    public const string XMR_WALLET_API_DIR = "F:\\Monero\\";
    public const string XMR_WALLET_API_1_PATH = "F:\\Monero\\monero-wallet-rpc1.exe";
    public const string XMR_WALLET_API_2_PATH = "F:\\Monero\\monero-wallet-rpc2.exe";
    public const int XMR_WALLET_API_2_PORT = 14076;
    public const string XMR_WALLET_API_2_PROCESS = "monero-wallet-rpc2";

    public const string LocalIPAddress = "127.0.0.1";
    public const string MoneroDMNPort = "18081";

  }
  class Config
  {
    public static bool StopService(string ServiceName)
    {
      ServiceController objSMC = new ServiceController(ServiceName);
      try
      {
        if (objSMC.Status == ServiceControllerStatus.Stopped) return true;
        objSMC.Stop();
        objSMC.Refresh();

        try
        {
          objSMC.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
        }
        catch (System.ServiceProcess.TimeoutException) { return false; }


        return true;
      }
      catch
      {
        return false;
      }
      finally
      {
        objSMC.Close();
        objSMC.Dispose();
      }
    }
    public static bool StartService(string ServiceName)
    {
      ServiceController objSMC = new ServiceController(ServiceName);
      try
      {
        if (objSMC.Status == ServiceControllerStatus.Running) return true;
        objSMC.Start();
        objSMC.Refresh();

        try
        {
          objSMC.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
        }
        catch (System.ServiceProcess.TimeoutException) { return false; }

        return true;
      }
      catch
      {
        return false;
      }
      finally
      {
        objSMC.Close();
        objSMC.Dispose();
      }
    }
  }

  public static class TaskExtensions
  {
    public static Task<TResult> ToApm<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
    {
      if (task == null) throw new ArgumentNullException(nameof(task));
      if (task.AsyncState == state)
      {
        if (callback != null) task.ContinueWith(t => callback(t), CancellationToken.None,
          TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
        return task;
      }

      var tcs = new TaskCompletionSource<TResult>(state);

      task.ContinueWith(delegate
      {
        tcs.TrySetResult(task.Result);
        callback(tcs.Task);
      }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

      return tcs.Task;
    }

    public async static Task<T> SetWaitCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {

      var tcs = new TaskCompletionSource<bool>();
      using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))

        if (task != await Task.WhenAny(task, tcs.Task))
          throw new OperationCanceledException(cancellationToken);

      return await task;
    }
  }
}
