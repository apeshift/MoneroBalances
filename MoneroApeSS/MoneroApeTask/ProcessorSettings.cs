using RokitFramework;
using System.Diagnostics;
using System.Net;

namespace MoneroApeTask
{
  public interface IProcessorSettings
  {
    string WalletPath { get; }
    string LocalIPAddress { get; }
    string MoneroRPCDirectory { get; }

    int MaxRPCPorts { get; }
    int StartRPCPort { get; }
    int MoneroDMNPort { get; }
    void Log(string message, TraceLevel level);
 
  }

  class ProcessorSettings
  {
    internal static IProcessorSettings Current
    {
      get
      {
        return ((IProcessorSettings)AppSettings.ConfigBag.Current);
      }
    }
  }
}
