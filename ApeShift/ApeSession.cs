using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApeShift.ECDH;
using ApeShiftWeb;
using Newtonsoft.Json;

namespace ApeShift
{

  public enum connection_type { client = 0, server = 1, network = 2 }
  public class ApeSession
  {

    public event MoneroRequestCompletedEventHandler _OnRequest;

    public event MoneroResponseCompletedEventHandler _OnResponse;

    public string _APEAPI_SECRET;
    public string _APEAPI_PUBLIC_ID;
    public string _APEMQ_CONNECTION_TOKEN;

    connection_type _UserType;

    SharedSession _sharedSession;

    public ApeMQ apeMQ = new ApeMQ();

    ApeAPI _ApeAPI;
    string _URL;

    public ApeSession(string APEAPI_SECRET, string APEAPI_PUBLIC_ID, string APEMQ_CONNECTION_TOKEN, string MY_PRIVATE_KEY, 
      connection_type connection_Type)
    {

      _sharedSession = new SharedSession(MY_PRIVATE_KEY.HexToByteArray());

      _APEAPI_PUBLIC_ID = APEAPI_PUBLIC_ID;
      _APEAPI_SECRET = APEAPI_SECRET;
      _APEMQ_CONNECTION_TOKEN = APEMQ_CONNECTION_TOKEN;
      _ApeAPI = new ApeAPI(_APEAPI_SECRET, _APEAPI_PUBLIC_ID, _sharedSession);

      _UserType = connection_Type;

      apeMQ._OnMsg += Processor_MSGEvent;

      _URL = "wss://app.async360.com/";

      if (connection_Type == connection_type.network)
        _URL = "wss://bcq.async360.com/";

      apeMQ.StartWS(_APEMQ_CONNECTION_TOKEN, _URL);

    }

    public async Task<string> APIRequest(api_methods method, object obj)
    {  
      return await _ApeAPI.Request(method, obj);
    }

    public async Task<MoneroResponse<object>> RequestAsync(MoneroRequest Request, string SendToPubKey)
    {
      try
      {
        AsyncDelegate asyncDelegate = new AsyncDelegate(this, Request, SendToPubKey);
        return await asyncDelegate.RequestAsync(60);
      }
      catch (TaskCanceledException) { }
      catch (OperationCanceledException) { }

      return new MoneroResponse<object>()
      {
        ErrorMessage = "Timeout, task cancelled.",
      };
    }

    void Processor_MSGEvent(DataMSGCompletedEventArgs processor)
    {
      if (processor.Message.IndexOf("error") == 0)
      {
        apeMQ.StartWS(_APEMQ_CONNECTION_TOKEN, _URL);
        return;
      }

      new Thread(() =>
      {
        try
        {

          if (_UserType == connection_type.network)
            return;

          var result = JsonConvert.DeserializeObject<MQResponse<MQDiffieResult>>(processor.Message).Result;

          if (result == null)
            return;

          var request = _sharedSession.ReceiveMessage(Convert.FromBase64String(result.ProtectedMessage),
            result.FromPublicKey.HexToByteArray());

          if (_UserType == connection_type.server)
          {
            MoneroRequest moneroRequest = JsonConvert.DeserializeObject<MoneroRequest>(Encoding.UTF8.GetString(request));
            moneroRequest.Request = result;
            RequestArrived(moneroRequest);
          }

          if (_UserType == connection_type.client)
          {
            MoneroResponse<object> moneroResp = JsonConvert.DeserializeObject<MoneroResponse<object>>(Encoding.UTF8.GetString(request));
            moneroResp.Context = result.MQContext;
            ResponseArrived(moneroResp);
            
          }

        }
        catch(Exception ex) {

          Console.WriteLine(ex.Message);
        }

      }).Start();

    }

    public void RequestArrived(MoneroRequest request)
    {
      try
      {
        if (request == null)
          return;

        if (_OnRequest == null)
          return;

        Interlocked.CompareExchange(ref _OnRequest, null, null)?.Invoke(new MoneroRequestCompletedEventArgs(request));
      }
      catch { }
    }

    public void ResponseArrived(MoneroResponse<object> response)
    {
      try
      {
        if (response == null)
          return;

        if (_OnResponse == null)
          return;

        Interlocked.CompareExchange(ref _OnResponse, null, null)?.Invoke(new MoneroResponseCompletedEventArgs(response));
      }
      catch { }
    }

    public async Task<bool> SendMessage(MoneroResponse<BalanceResponse> Message, MoneroRequest request)
    {
      Message.Context = request.Request.MQContext;
      Message.WalletMethod = request.WalletMethod;
      await _ApeAPI.SendMessage(JsonConvert.SerializeObject(Message), request.Request.FromPublicKey, request.Request.MQContext);

      return true;
    }

    public async Task<bool> SendMessage(MoneroResponse<ImportResponse> Message, MoneroRequest request)
    {
      Message.Context = request.Request.MQContext;
      Message.WalletMethod = request.WalletMethod;
      await _ApeAPI.SendMessage(JsonConvert.SerializeObject(Message), request.Request.FromPublicKey, request.Request.MQContext);

      return true;
    }
    public async Task<bool> SendMessage(string Message, MoneroRequest request)
    {
      await _ApeAPI.SendMessage(Message, request.Request.FromPublicKey, request.Request.MQContext);

      return true;
    }

    public async Task<bool> SendMessage(MoneroRequest Message, string PublicKey, string Context)
    {
      await _ApeAPI.SendMessage(JsonConvert.SerializeObject(Message), PublicKey, Context);

      return true;
    }

  }


  public delegate void DataMSGCompletedEventHandler(DataMSGCompletedEventArgs e);
  public partial class DataMSGCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
  {
    public DataMSGCompletedEventArgs(object result) : base(null, false, null) { Message = (string)result; }
    public string Message { get; }
  }

  public delegate void MoneroRequestCompletedEventHandler(MoneroRequestCompletedEventArgs e);
  public partial class MoneroRequestCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
  {
    public MoneroRequestCompletedEventArgs(MoneroRequest result) : base(null, false, null)
    { Request = result; }
    public MoneroRequest Request { get; }
  }

  public delegate void MoneroResponseCompletedEventHandler(MoneroResponseCompletedEventArgs e);
  public partial class MoneroResponseCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
  {
    public MoneroResponseCompletedEventArgs(MoneroResponse<object> result) : base(null, false, null)
    { Response = result; }
    public MoneroResponse<object> Response { get; }
  }

  public enum monero_request_type { wallet_balance = 0, import_wallet = 1 }
  public class MoneroRequest
  {
    public monero_request_type WalletMethod { get; set; }
    public string MoneroAddress { get; set; }
    public string MoneroViewKey { get; set; }
    public MQDiffieResult Request { get; set; }

  }

  public class MoneroResponse<T>
  {
    public monero_request_type WalletMethod { get; set; }
    public string ErrorMessage { get; set; }
    public string Context { get; set; }
    public T Response { get; set; }
  }

  public class ImportResponse
  {
    public string ClientToken { get; set; }
  }

  public class BalanceResponse
  {
    public BigInteger Balance { get; set; }
    public BigInteger TotalSent { get; set; }
    public BigInteger TotalReceived { get; set; }
    public BigInteger LockedBalance { get; set; }
  }
}
