using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RokitFramework;

namespace MoneroApeTask
{
  public class DMNProcessor : WorkProcessor
  {
    public DMNProcessor(string instanceName) : base(instanceName + " : DMNProcessor")
    {

    }

    public override decimal RefreshInterval
    {
      get
      {
        return 300;
      }
    }

    public static long DaemonHeight = 0;
    public static long DaemonFeeEstimate = 0;
    protected override void Work()
    {
      while (true)
      {
        this.WorkWait();

        try
        {
          this.SignalWorkBegin();

          try
          {
            var height = JsonConvert.DeserializeObject<HeightObjectResp>(Daemon.GetFromDaemon(MoneroApeSS.ApiConfig.LocalIPAddress + ":" + MoneroApeSS.ApiConfig.MoneroDMNPort + "/get_height").Result).height;
            if (height > DaemonHeight) DaemonHeight = height;
          }
          catch { }

          try
          {
            DaemonFeeEstimate = JsonConvert.DeserializeObject<JResponseWrapper<FeeObjectResp>>(Daemon.PostToDaemon(Daemon.GetFeeObject(), MoneroApeSS.ApiConfig.LocalIPAddress + ":" + MoneroApeSS.ApiConfig.MoneroDMNPort + "/json_rpc").Result).Result.fee;
          }
          catch { }

          this.SignalWorkEnd();
        }
        catch (System.Threading.ThreadAbortException)
        {
          return;
        }
        catch (Exception ex)
        {
        //  ProcessorSettings.Current.Log(this.ProcessorName + ": Error: " + ex.Message, System.Diagnostics.TraceLevel.Error);
        }
      }

    }
  }

}

