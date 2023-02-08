using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MoneroApeTask;
using ApeShift;

namespace MoneroApeSS
{

  public class CBAsyncManager
  {

    static ApeSession APISession = new ApeSession(ApiConfig.APEAPI_SECRET, ApiConfig.APEAPI_PUBLIC_ID,
      ApiConfig.APPMQ_CONNECTION_TOKEN, ApiConfig.MY_PRIVATE_KEY, connection_type.server);

    static ConcurrentQueue<WalletTask> WalletTasks = new ConcurrentQueue<WalletTask>();

    static ConcurrentDictionary<string, string> BusyTasks = new ConcurrentDictionary<string, string>();

    static ConcurrentQueue<string> AvailablePorts = new ConcurrentQueue<string>();

    public static void Start()
    {

      var ports = PortManager.StartRPCPorts();

      foreach (var port in ports)
      {
        AvailablePorts.Enqueue(port);
      }

      APISession._OnRequest += Processor_API;

      Task t = Task.Run(async () =>
       {
         await SyncWallets();

       });

    }

    static void Processor_API(MoneroRequestCompletedEventArgs Request)
    {
      Task.Run(async () =>
      {

        var request = Request.Request;

        var uinfo = GetUserInfo(request.MoneroAddress, request.MoneroViewKey);

        if (uinfo == null)
        {
          MoneroResponse<BalanceResponse> response = new MoneroResponse<BalanceResponse>();
          response.ErrorMessage = "Invalid Monero address or view key";

          await APISession.SendMessage(response, request);
          return;
        }

        if (request.WalletMethod == monero_request_type.import_wallet)
        {
          await ImportManager.ImportWallet(request);
          return;
        }

        if (!uinfo.WalletCreated)
        {
          MoneroResponse<BalanceResponse> response = new MoneroResponse<BalanceResponse>();
          response.ErrorMessage = "Wallet not created, use import method";

          await APISession.SendMessage(response, request);
          return;
        }

        if (ImportManager.IsWalleInQ(uinfo))
        {
          MoneroResponse<BalanceResponse> response = new MoneroResponse<BalanceResponse>();
          response.ErrorMessage = "Let's wait for wallet import";

          await APISession.SendMessage(response, request);
          return;
        }

        AddWalletTask(uinfo, request);

      });

    }

    async static Task SyncWallets()
    {

      while (true)
      {
        var task = GetWalletTask();

        if (task == null)
        {
          await Task.Delay(1000);
          continue;
        }

        Task t = Task.Run(async () =>
        {

          var request = task.MoneroRequest;

          MoneroResponse<BalanceResponse> response = new MoneroResponse<BalanceResponse>();

          try
          {
            WalletInfo user = await PostRequests.ReturnLoginUser(task);
            response.Response = new BalanceResponse()
            {
              Balance = user.Balance,
              LockedBalance = user.LockedBalance,
              TotalReceived = user.TotalReceived,
              TotalSent = user.TotalSent
            };
          }
          catch(Exception ex)
          {
            response.ErrorMessage = ex.Message;
          }

          RemoveTask(task);

          try
          {
            await APISession.SendMessage(response, request);
          }
          catch { }

        });

      }

    }

    static WalletTask GetWalletTask()
    {

      AvailablePorts.TryDequeue(out string Port);

      if (!string.IsNullOrEmpty(Port))
      {

        WalletTasks.TryDequeue(out WalletTask task);

        if (task != null)
        {
          task.Port = Port;
          return task;
        }
        else
        {
          AvailablePorts.Enqueue(Port);
        }
      }

      return null;
    }

    static void RemoveTask(WalletTask task)
    {
      try
      {
        AvailablePorts.Enqueue(task.Port);
        BusyTasks.TryRemove(task.User.FileName.ToString(), out string fn);
      }
      catch { }
    }

    static bool IsWalleInQ(UserInfo info)
    {

      if (BusyTasks.ContainsKey(info.FileName.ToString()))
        return true;

      return false;
    }
    static void AddWalletTask(UserInfo info, MoneroRequest request)
    {
      if (IsWalleInQ(info))
        return;

      WalletTask task = new WalletTask();
      task.User = info;
      task.MoneroRequest = request;
      WalletTasks.Enqueue(task);
      BusyTasks.TryAdd(task.User.FileName.ToString(), "busy");

    }
    public static UserInfo GetUserInfo(string MoneroAddress, string MoneroViewKey)
    {

      if (string.IsNullOrEmpty(MoneroAddress)) return null;
      if (string.IsNullOrEmpty(MoneroViewKey)) return null;
      MoneroViewKey = MoneroViewKey.ToLower().Trim();
      MoneroAddress = MoneroAddress.Trim();
      byte[] ViewKeyBytes = GetViewKeyBytes(MoneroViewKey);
      if (ViewKeyBytes == null) return null;

      var _FileName = GetFileName(MoneroAddress, ViewKeyBytes);
      var _WalletFile = ApiConfig.XMR_SAVED_WALLETS_DIR + _FileName.ToString();

      return new UserInfo()
      {
        FileName = _FileName,
        FilePassword = MoneroViewKey,
        MoneroAddress = MoneroAddress,
        MoneroViewKey = MoneroViewKey,
        FullFileName = _WalletFile,
        WalletCreated = FileCreated(_WalletFile),
      };
    }
    static Guid GetFileName(string MoneroAddress, byte[] MoneroViewKey)
    {
      HMACMD5 hMACMD5 = new HMACMD5(Encoding.UTF8.GetBytes(MoneroAddress));
      var hash = hMACMD5.ComputeHash(MoneroViewKey);
      return new Guid(hash);
    }
    static byte[] GetViewKeyBytes(string MoneroViewKey)
    {
      try
      {
        byte[] bts = MoneroViewKey.HexToByteArray();
        if (bts.Length != 32) return null;
        return bts;
      }
      catch { return null; }
    }

    static bool FileCreated(string _FileName)
    {
      try
      {
        return new System.IO.FileInfo(_FileName).Exists;
      }
      catch { return false; }
    }

  }


}