using ApeShift;
using ApeShiftWeb;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MoneroApeSS
{
  public class ImportManager
  {
    static ApeSession APISession;

    static ConcurrentDictionary<string, ImportTask> BusyTasks = new ConcurrentDictionary<string, ImportTask>();

    static ConcurrentQueue<ImportTask> ImportTasks = new ConcurrentQueue<ImportTask>();

    private static bool RpcPortBusy = false;

    public static void Start()
    {

      PortManager.StartPort2(ApiConfig.XMR_WALLET_API_2_PORT.ToString());

      APISession = new ApeSession(ApiConfig.APEAPI_SECRET, ApiConfig.APEAPI_PUBLIC_ID, ApiConfig.BLOCKMQ_CONNECTION_TOKEN, ApiConfig.MY_PRIVATE_KEY, connection_type.network);

      APISession.apeMQ._OnMsg += OnMQEvent;

      Task t = Task.Run(async () =>
      {
        await SyncWallets();

      });

    }

    public async static Task ImportWallet(MoneroRequest request)
    {
      var uinfo = CBAsyncManager.GetUserInfo(request.MoneroAddress, request.MoneroViewKey);

      MoneroResponse<ImportResponse> response = new MoneroResponse<ImportResponse>();

      if (IsWalleInQ(uinfo))
      {
        ImportTask task = BusyTasks[uinfo.FileName.ToString()];
        response.Response = new ImportResponse()
        {
          ClientToken = task.User.client_token
        };

        await APISession.SendMessage(response, request);
        return;
      }

      if (uinfo.WalletCreated)
      {
        response.ErrorMessage = "Wallet already created";
        await APISession.SendMessage(response, request);
        return;
      }

      try
      {
        response.Response = await GetTokenForClient(uinfo);
        await APISession.SendMessage(response, request);

      }
      catch (Exception ex)
      {
        response.ErrorMessage = ex.Message;
        await APISession.SendMessage(response, request);
        return;
      }
    }
    static async Task<ImportResponse> GetTokenForClient(MoneroApeTask.UserInfo user)
    {

      var count = await APISession.APIRequest(api_methods.get_address_count, new AddressCountRequest()
      {
        network = "grs_testnet",
        queue_name = "wallet"
      });

      var address = GetPayementAddress(JsonConvert.DeserializeObject
        <JResponseWrapper<AddressCountResponse>>(count).Result.address_count);


      var envoytask = new EnvoyTask()
      {
        task_context = user.FileName.ToString() + "import",
        send_offline_message = true,
        single_instance = false,
        timeout = 60 * 48,
        persist_timeout = false
      };

      var client = new GetEnvoyTokenRequest()
      {
        reusable_minutes = 120,
        envoy_task = envoytask
      };

      var aperesp = await APISession.APIRequest(api_methods.get_envoy_token, client);

      var tokenResp = JsonConvert.DeserializeObject<JResponseWrapper<GetEnvoyTokenResponse>>(aperesp).Result;


      Task t = Task.Run(async () =>
      {
        try
        {
          var subscribe = await APISession.APIRequest(api_methods.subscribe, new SubscibeRequest()
          {
            address = address,
            include_data = user.FileName.ToString(),
            network = "grs_testnet",
            queue_name = "wallet"
          });

          await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest()
          {
            new_timeout = 60 * 48,
            message = "To start import please send any GRS-TESTNET amount to address " + address,
            task_id = tokenResp.task_id
          });

          AddWalletTask(new ApeUserData()
          {
            MoneroAddress = user.MoneroAddress,
            MoneroViewKey = user.MoneroViewKey,
            task_id = tokenResp.task_id,
            client_token = tokenResp.envoy_token
          });
        }
        catch (Exception ex)
        {
          await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest()
          {
            new_timeout = 0,
            message = "[Internal API Error] " + ex.Message,
            task_id = tokenResp.task_id
          });
        }

      });

      return new ImportResponse()
      {
        ClientToken = tokenResp.envoy_token
      };

    }
    async static Task SyncWallets()
    {

      while (true)
      {

        var task = GetWalletTask();

        if (task == null)
        {
          await Task.Delay(3000);
          continue;
        }

        RpcPortBusy = true;
        CancellationTokenSource cancellationToken = new CancellationTokenSource();

        var a = Task.Run(async () =>
        {
          try
          {
            var d = await SendCounters(task.User.task_id,
              cancellationToken.Token).SetWaitCancellation(cancellationToken.Token);
          }
          catch (OperationCanceledException)
          {
            RpcPortBusy = false;
            RemoveTask(task);
          }
        });

        Task t = Task.Run(async () =>
        {
          try
          {
            await ReturnNewUser(task);

          }
          catch { }

          cancellationToken.Cancel();

        });

      }
    }
    async static Task<bool> SendCounters(string task_id, CancellationToken token)
    {

      var counter = new System.Diagnostics.PerformanceCounter("Process",
        "% Processor Time", ApiConfig.XMR_WALLET_API_2_PROCESS);

      counter.NextValue();

      while (!token.IsCancellationRequested)
      {
        var st = counter.NextValue();

        if (st > 0)
          st = (st / 8);

        var ty = await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest
        {
          new_timeout = 60 * 48,
          message = "[Import Perfomance] Your wallet process is currently using CPU% " + st.ToString(),
          task_id = task_id
        });

        await Task.Delay(3000);

      }

      return true;
    }

    static ImportTask GetWalletTask()
    {

      if (!RpcPortBusy)
      {
        ImportTasks.TryDequeue(out ImportTask task);
        return task;
      }
      else
      {
        foreach (var t in ImportTasks)
        {
          if (t.ImportAdded)
          {
            Task.Run(async () =>
            {
              try
              {
                await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest()
                {
                  new_timeout = 60 * 48,
                  message = "Import queue busy, please wait. Time elapsed " +
                    (DateTime.Now - t.QTime).ToString(),
                  task_id = t.User.task_id
                });
              }
              catch { }
            });
          }
        }
      }

      return null;
    }
    static void RemoveTask(ImportTask task)
    {
      try
      {
        var uinfo = CBAsyncManager.GetUserInfo(task.User.MoneroAddress, task.User.MoneroViewKey);
        BusyTasks.TryRemove(uinfo.FileName.ToString(), out ImportTask fn);
      }
      catch { }
    }
    public static bool IsWalleInQ(MoneroApeTask.UserInfo info)
    {
      if (BusyTasks.ContainsKey(info.FileName.ToString()))
        return true;

      return false;
    }
    static void AddWalletTask(ApeUserData user)
    {

      var uinfo = CBAsyncManager.GetUserInfo(user.MoneroAddress, user.MoneroViewKey);

      if (uinfo == null)
        return;

      if (IsWalleInQ(uinfo))
        return;

      ImportTask task = new ImportTask();
      task.User = user;

      BusyTasks.TryAdd(uinfo.FileName.ToString(), task);

    }

    static void AddImportTask(string FileName)
    {

      if (!BusyTasks.ContainsKey(FileName))
        return;

      ImportTask task = BusyTasks[FileName];
      if (task.ImportAdded)
        return;

      task.ImportAdded = true;
      task.QTime = DateTime.Now;

      ImportTasks.Enqueue(task);

      Task.Run(async () =>
      {
        string msg = "Payment received! Import starting soon.";
        int count = ImportTasks.Count - 1;

        if (RpcPortBusy)
          count++;

        if (count > 0)
          msg = "Payment received! (There are " + count.ToString() + " imports ahead of you in queue)";

        await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest
        {
          new_timeout = 60 * 48,
          message = msg,
          task_id = task.User.task_id
        });

      });

    }
    static void OnMQEvent(DataMSGCompletedEventArgs processor)
    {
      if (processor.Message.IndexOf("error") == 0)
        return;

      var resp = JsonConvert.DeserializeObject<MQResponse<MQNetworkResult>>(processor.Message).Result;

      AddImportTask(resp.UserData);
    }

    public async static Task ReturnNewUser(ImportTask task)
    {
      var uinfo = CBAsyncManager.GetUserInfo(task.User.MoneroAddress, task.User.MoneroViewKey);

      var open = await MoneroApeTask.PostRequests.PostAsync(ApiConfig.XMR_WALLET_API_2_PORT.ToString(),
        MoneroApeTask.PostRequests.GetCreateWalletObject(uinfo, 0));

      if (!string.IsNullOrEmpty(open) && open.Contains(uinfo.MoneroAddress))
      {
        new Thread(() =>
        {
          Task.Run(async () =>
          {
            try
            {
              await MoneroApeTask.PostRequests.PostAsync(ApiConfig.XMR_WALLET_API_2_PORT.ToString(),
                MoneroApeTask.PostRequests.GetRefreshWalletObject());
            }
            catch { }

          });

        }).Start();


        while (true)
        {

          await Task.Delay(10000);

          var testPort = await PortManager.PortOpen(ApiConfig.XMR_WALLET_API_2_PORT.ToString());

          if (testPort)
          {

            var hreq = await MoneroApeTask.PostRequests.PostAsync(ApiConfig.XMR_WALLET_API_2_PORT.ToString(),
              MoneroApeTask.PostRequests.GetHeightObject());

            if (!string.IsNullOrEmpty(hreq))
            {

              var WalletHeight = JsonConvert.DeserializeObject<MoneroApeTask.JResponseWrapper
                <MoneroApeTask.HeightObjectResp>>(hreq).Result.height;

              if (WalletHeight > 0)
              {
                try
                {

                  await MoneroApeTask.PostRequests.PostAsync(ApiConfig.XMR_WALLET_API_2_PORT.ToString(),
                    MoneroApeTask.PostRequests.GetSaveWalletObject());

                  await MoneroApeTask.PostRequests.PostAsync(ApiConfig.XMR_WALLET_API_2_PORT.ToString(),
                    MoneroApeTask.PostRequests.GetCloseWalletObject());
                }
                catch { }

                await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest
                {
                  new_timeout = 0,
                  message = "[Import Success] Full scan complete.",
                  task_id = task.User.task_id
                });

                break;

              }
            }

          }

        }

      }
      else
      {
        await APISession.APIRequest(api_methods.invoke_envoy, new InvokeEnvoyRequest
        {
          new_timeout = 0,
          message = "[Error] Unable to create wallet, please check address and view key.",
          task_id = task.User.task_id
        });
      }

    }

    static string GetPayementAddress(int index)
    {
      ExtPubKey ext = new ExtPubKey(ApiConfig.MY_EXTPUB_KEY);
      Network network = NBitcoin.Altcoins.Groestlcoin.Instance.Testnet;
      var xkey = ext.Derive(index, false);
      return xkey.PubKey.Compress(false).GetAddress(network).ToString();
    }


  }

  public class ApeUserData
  {
    public string MoneroAddress { get; set; }
    public string MoneroViewKey { get; set; }
    public string task_id { get; set; }
    public string client_token { get; set; }
  }

  public class ImportTask
  {
    public DateTime QTime { get; set; }
    public bool ImportAdded { get; set; }
    public ApeUserData User { get; set; }
  }

}
