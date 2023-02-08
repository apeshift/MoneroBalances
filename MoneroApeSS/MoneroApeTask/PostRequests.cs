using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

namespace MoneroApeTask
{
  public class PostRequests
  {

    public async static Task<WalletInfo> ReturnLoginUser(WalletTask task)
    {
      WalletInfo info = new WalletInfo();
      var open = await PostAsync(task.Port, GetOpenWalletObject(task.User));

      if (!string.IsNullOrEmpty(open) && !open.ToLower().Contains("failed"))
      {

        try
        {
          await PostAsync(task.Port, GetRefreshWalletObject());
          await PostAsync(task.Port, GetSaveWalletObject());
          var WalletHeight = await PostAsync(task.Port, GetHeightObject());
          var IncomingTransfers = await PostAsync(task.Port, GetIncomingTransfersObject());
          var pool = await PostAsync(task.Port, GetPendingTransfersObject());
          var incomingTransfer = JsonConvert.DeserializeObject<JResponseWrapper<TResponse>>(IncomingTransfers);

          if (incomingTransfer.Result.transfers != null && incomingTransfer.Result.transfers.Length > 0)
          {

            info = await CaptiveWallet.GetWalletInfo(incomingTransfer.Result.transfers, pool);

          }
          else
          {
            info.SpentOutputs = new List<WalletOutput>();
            info.UnspentOutputs = new List<WalletOutput>();
            info.Balance = BigInteger.Zero;
            info.LockedBalance = BigInteger.Zero;
            info.TotalReceived = BigInteger.Zero;
            info.TotalSent = BigInteger.Zero;
            info.TransactionHashes = new string[0];
          }

          info.DaemonHeight = DMNProcessor.DaemonHeight;
          info.FeesEstimate = DMNProcessor.DaemonFeeEstimate;
          info.WalletHeight = JsonConvert.DeserializeObject<JResponseWrapper<HeightObjectResp>>(WalletHeight).Result.height;
          info.ProcessID = task.Port;

        }
        catch (Exception ex)
        {
          try
          {
            await PostRequests.PostAsync(task.Port, PostRequests.GetCloseWalletObject());
          }
          catch { }

          throw new Exception(ex.Message);
        }

      }
      else
      {
        if (string.IsNullOrEmpty(open))
          open = "Unable to open wallet file, please try again.";

        throw new Exception(open);
      }

      return info;
    }

    public static string BlockObject(long ht)
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "get_block",
        Id = "0",
        Params = new
        {
          height = ht
        },
      };

      return FormatJson(data);
    }
    public static string GetHeightObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "get_height",
        Id = "0",
      };

      return FormatJson(data);
    }
    static string GetIncomingTransfersObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "incoming_transfers",
        Id = "0",
        Params = new
        {
          transfer_type = "all",
          verbose = true
        },
      };

      return FormatJson(data);
    }
    static string GetPendingTransfersObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "get_transfers",
        Id = "0",
        Params = new
        {
          pool = true
        },
      };

      return FormatJson(data);
    }
    public static string GetRefreshWalletObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "refresh",
        Id = "0",
        Params = new
        {

        },
      };

      return FormatJson(data);
    }

    public static string GetAutoRefreshWalletObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "auto_refresh",
        Id = "0",
        Params = new
        {
          enable = true,
          period = 5
        },
      };

      return FormatJson(data);
    }
    public static string GetOpenWalletObject(UserInfo userInfo)
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "open_wallet",
        Id = "0",
        Params = new
        {
          filename = userInfo.FileName.ToString(),
          password = userInfo.FilePassword,
        },
      };

      return FormatJson(data);
    }
    public static string GetCloseWalletObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "close_wallet",
        Id = "0",
      };

      return FormatJson(data);
    }
    public static string GetSaveWalletObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "store",
        Id = "0",
      };

      return FormatJson(data);
    }
    static string GetQueryKeyObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "query_key",
        Id = "0",
        Params = new
        {
          key_type = "view_key",
        },
      };

      return FormatJson(data);
    }
    public static string GetCreateWalletObject(UserInfo userInfo, int FromHeight)
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "generate_from_keys",
        Id = "0",
        Params = new
        {
          restore_height = FromHeight,
          filename = userInfo.FileName.ToString(),
          address = userInfo.MoneroAddress,
          viewkey = userInfo.MoneroViewKey,
          password = userInfo.FilePassword,
          autosave_current = false
        },
      };

      return FormatJson(data);

    }
    static string FormatJson(object obj)
    {
      var serializerSettings = new JsonSerializerSettings();
      serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
      return JsonConvert.SerializeObject(obj, serializerSettings);
    }
    public async static Task<string> PostAsync(string port, string obj, int TimeoutSeconds = 0)
    {
      try
      {
        string url = MoneroApeSS.ApiConfig.LocalIPAddress + ":" + port + "/json_rpc";
        byte[] byteArray = System.Text.Encoding.Default.GetBytes(obj);
        var content = new ByteArrayContent(byteArray);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using (var _HttpClientBrowser = new HttpClient())
        {
          if (TimeoutSeconds > 0)
            _HttpClientBrowser.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

          using (var response = await _HttpClientBrowser.PostAsync(url, content))
          {

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(responseStream))
            {
              return await streamReader.ReadToEndAsync();
            }
          }
        }

      }
      catch (HttpRequestException ex)
      {
       // ProcessorSettings.Current.Log("PostAsync Error:" + ex.Message, System.Diagnostics.TraceLevel.Verbose);
        return null;
      }
      catch (TaskCanceledException e)
      {
       // ProcessorSettings.Current.Log("PostAsync Error:" + e.Message, System.Diagnostics.TraceLevel.Verbose);
        return null;
      }
    }

  }
}
