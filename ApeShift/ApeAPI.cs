﻿using ApeShift.ECDH;
using ApeShiftWeb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static ApeShift.ECDH.HexByteExtensions;

namespace ApeShift
{
  public class ApeAPI
  {
    string _APEAPI_SECRET;
    string _APEAPI_PUBLIC_ID;

    SharedSession _sharedSession;

    public ApeAPI(string APEAPI_SECRET, string APEAPI_PUBLIC_ID, SharedSession sharedSession)
    {
      _APEAPI_PUBLIC_ID = APEAPI_PUBLIC_ID;
      _APEAPI_SECRET = APEAPI_SECRET;

      _sharedSession = sharedSession;
    }

    public async Task<string> Request(api_methods Method, object Params)
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = Method.ToString(),
        Id = "2",
        Params = Params
      };

      var resp = await SendRequest(data);
      var jwr = JsonConvert.DeserializeObject<JResponseWrapper<object>>(resp);

      if (jwr.Error != null)
      {
        throw new Exception(jwr.Error.Message);
      }

      return resp;

    }
    public async Task<bool> SendMessage(string Message, string QPublicKey, string Context)
    {
      try
      {
        var protectedMessage = _sharedSession.CreateMessage(Encoding.UTF8.GetBytes(Message),
          QPublicKey.HexToByteArray()).ToB64();

        var o = new InvokeApplicatonRequest
        {
          protected_message = protectedMessage,
          from_public_key = _sharedSession.MyPublicKey().ToHex(),
          to_public_key = QPublicKey,
          request_context = Context
        };

        var data = new
        {
          Jsonrpc = "2.0",
          Method = api_methods.invoke_application.ToString(),
          Id = "2",
          Params = o
        };

        var resp = await SendRequest(data);

        var jwr = JsonConvert.DeserializeObject<JResponseWrapper<object>>(resp);

        if (jwr.Error != null)
          return false;

        return true;

      }
      catch { }

      return false;

    }

    public string GetHeaderSign(string obj)
    {
      HMACSHA512 hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_APEAPI_SECRET));
      byte[] hashmessage = hmac.ComputeHash(Encoding.UTF8.GetBytes(obj));
      return hashmessage.ToHex();
    }

    async Task<string> SendRequest(object Request, string url = "https://api.async360.com/api")
    {

      try
      {
        JObject obj = (JObject)JToken.FromObject(Request);
        byte[] byteArray = System.Text.Encoding.Default.GetBytes(obj.ToString());

        using (var client = new HttpClient())
        {

          client.DefaultRequestHeaders.Add("public-id", _APEAPI_PUBLIC_ID);
          client.DefaultRequestHeaders.Add("sign", GetHeaderSign(obj.ToString()));

          var content = new ByteArrayContent(byteArray);
          var response = await client.PostAsync(url, content);
          var responseString = await response.Content.ReadAsStringAsync();

          return responseString;
        }
      }
      catch (HttpRequestException ex)
      {
        Console.WriteLine(ex.Message);

      }

      return null;
    }


    class JError
    {
      [JsonProperty("message")]
      public string Message { get; set; }

      [JsonProperty("code")]
      public int Code { get; set; }
    }

    class JResponseWrapper<T>
    {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("jsonrpc")]
      public string Jsonrpc { get; set; }

      [JsonProperty("error")]
      public JError Error { get; set; }

      [JsonProperty("result")]
      public T Result { get; set; }
    }

  }
}