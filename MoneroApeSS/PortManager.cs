using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;


namespace MoneroApeSS
{

  public class PortManager
  {

    public static bool PortManagerStarted;
    
    public static List<string> StartRPCPorts(int StartAt = ApiConfig.XMR_WALLET_API_1_START_PORT)
    {

      List<string> Ports = new List<string>();

      for (int i = 0; i < ApiConfig.XMR_WALLET_API_1_MAX_PORTS; i++)
      {
        string port = (StartAt + i).ToString();
        var p = StartPort(port);

        if (p > 0)
        { 
            Ports.Add(port);
        }
      }

      PortManagerStarted = true;
      return Ports;

    }
    public async static Task<bool> PortOpen(string port)
    {
      string IP = "http://127.0.0.1:";
      string url = IP + port + "/json_rpc";
      var check = await GetAsync(url);
      if (string.IsNullOrEmpty(check)) return false;
      return true;
    }

    public async static Task<string> GetAsync(string url, int timeoutMsec = 3000)
    {
      try
      {
        using (var client = new System.Net.Http.HttpClient())
        {
          client.Timeout = TimeSpan.FromMilliseconds(timeoutMsec);
          var response = await client.GetAsync(url);
          var responseString = await response.Content.ReadAsStringAsync();
          return responseString;
        }
      }
      catch (TaskCanceledException)
      {
        return null;
      }
      catch (HttpRequestException)
      {
        return null;
      }
      catch (Exception)
      {
        return null;
      }
    }
    public static int StartPort(string port)
    {
      string cmdArguments = "--rpc-bind-port " + port + " --rpc-bind-ip=127.0.0.1 --wallet-dir=" + ApiConfig.XMR_SAVED_WALLETS_DIR + "  --disable-rpc-login --disable-rpc-ban --max-concurrency=6";

      using (System.Diagnostics.Process p = new System.Diagnostics.Process())
      {
        try
        {
          ProcessStartInfo psi = new ProcessStartInfo(ApiConfig.XMR_WALLET_API_1_PATH, cmdArguments);
          psi.Verb = "runas";
          psi.CreateNoWindow = true;
          psi.WorkingDirectory = ApiConfig.XMR_WALLET_API_DIR;
          p.StartInfo = psi;
          var st = p.Start();

          if (st)
            return p.Id;
        }
        catch { }
      }

      return 0;
    }

    public static int StartPort2(string port)
    {
      string cmdArguments = "--rpc-bind-port " + port + " --rpc-bind-ip=127.0.0.1 --wallet-dir=" + ApiConfig.XMR_SAVED_WALLETS_DIR + " --disable-rpc-login --disable-rpc-ban --max-concurrency=6";

      using (System.Diagnostics.Process p = new System.Diagnostics.Process())
      {
        try
        {
          ProcessStartInfo psi = new ProcessStartInfo(ApiConfig.XMR_WALLET_API_2_PATH, cmdArguments);
          psi.Verb = "runas";
          psi.CreateNoWindow = true;
          psi.WorkingDirectory = ApiConfig.XMR_WALLET_API_DIR;
          p.StartInfo = psi;
          var st = p.Start();

          if (st)
            return p.Id;
        }
        catch { }
      }

      return 0;
    }
  }
}
