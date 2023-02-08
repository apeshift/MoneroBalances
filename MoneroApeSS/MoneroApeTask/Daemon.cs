using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MoneroApeTask
{
  public class Daemon
  {
    public async static Task<List<string>> GetPending(List<string> TxnHashes)
    {
      var response = await PostToDaemon(GetTransactionsObject(TxnHashes.ToArray()), MoneroApeSS.ApiConfig.LocalIPAddress + ":" + MoneroApeSS.ApiConfig.MoneroDMNPort + "/get_transactions");

      DaemonTxns daemonTxns = ParseTransaction(response);
      MixOuts mixOuts = new MixOuts();
      mixOuts.get_Outputs_Outs = new List<get_outputs_out>();
      mixOuts.TxnRefs = new List<string>();

      foreach (var usedOutputs in daemonTxns.txs)
      {
        foreach (var Vin in usedOutputs.TxnData.vins)
        {
          SerializeMixins(Vin.key.amount, Vin.key.key_offsets, usedOutputs.tx_hash, mixOuts);
        }
      }

      var mix = await GetMixins(mixOuts);
      List<string> responses = new List<string>();

      foreach (var spent in mix)
      {
        responses.Add(spent.Mixin);
      }

      return responses;
    }
    public async static Task<WalletTransactions> GetOutsForDevice(transfers[] fromWalletRPC, List<string> TxnHashes)
    {

      var response = await PostToDaemon(GetTransactionsObject(TxnHashes.ToArray()), MoneroApeSS.ApiConfig.LocalIPAddress + ":" + MoneroApeSS.ApiConfig.MoneroDMNPort + "/get_transactions");

      WalletTransactions walletTransactions = new WalletTransactions();
      walletTransactions.Outputs = new List<WalletOutput>();

      DaemonTxns daemonTxns = ParseTransaction(response);

      MixOuts mixOuts = new MixOuts();
      mixOuts.get_Outputs_Outs = new List<get_outputs_out>();
      mixOuts.TxnRefs = new List<string>();

      foreach (var usedOutputs in daemonTxns.txs)
      {

        transfers[] incomings = fromWalletRPC.Where(v =>
         v.tx_hash == usedOutputs.tx_hash).ToArray();

        if (incomings == null || incomings.Length == 0) continue;

        foreach (var incoming in incomings)
        {
          WalletOutput _Out = new WalletOutput();

          if (!usedOutputs.output_indices.Contains(incoming.global_index)) continue;


          int Index = 0;
          string rct = string.Empty;

          try
          {
            Index = usedOutputs.output_indices.ToList().IndexOf(incoming.global_index);
          }
          catch (Exception ex)
          {
            throw new Exception("Index | " + ex.Message);
          }

          try
          {
            rct = GetEC_RCT(usedOutputs.TxnData.signatures, Index);
          }
          catch (Exception ex)
          {
            throw new Exception("GetEC_RCT | " + ex.Message);
          }
      
       

          _Out.amount = incoming.amount;
          _Out.global_index = incoming.global_index;
          _Out.index = (uint)Index;

          try
          {
            _Out.public_key = GetEC_PK(usedOutputs.TxnData.vouts, Index);
          }
          catch (Exception ex)
          {
            throw new Exception("GetEC_PK | " + ex.Message);
          }

          if (string.IsNullOrEmpty(_Out.public_key))
          {
            _Out.public_key = GetEC_PKTag(usedOutputs.TxnData.vouts, Index);
          }

          if (string.IsNullOrEmpty(_Out.public_key))
          {
            throw new Exception("null pubkey | " + JsonConvert.SerializeObject(usedOutputs));
          }

          try
          {
            _Out.txn_fee = GetEC_Fee(usedOutputs.TxnData.signatures, Index);
          }
          catch (Exception ex)
          {
            throw new Exception("GetEC_Fee | " + ex.Message);
          }



          _Out.rct = rct;
          _Out.tx_pub_key = usedOutputs.TxPublicKey;
          _Out.tx_hash = usedOutputs.tx_hash;
          _Out.unlocked = incoming.unlocked;
          _Out.timestamp = usedOutputs.block_timestamp;
          _Out.height = usedOutputs.block_height;

          try
          {
            _Out.extra = Convert.ToBase64String(usedOutputs.TxnData.extra);
          }
          catch (Exception ex)
          {
            throw new Exception("ToBase64String | " + ex.Message);
          }




          foreach (var Vin in usedOutputs.TxnData.vins)
          {

            try
            {

              SerializeMixins(Vin.key.amount, Vin.key.key_offsets, _Out.tx_hash, mixOuts);
            }catch(Exception ex)
            {
              throw new Exception("SerializeMixins | " + ex.Message);
            }
          }
        
          walletTransactions.Outputs.Add(_Out);
        }
      }

      try
      {

        walletTransactions.mixRefs = await GetMixins(mixOuts);
      }catch(Exception ex)
      {
        throw new Exception("GetMixins | " + ex.Message);
      }

      return walletTransactions;

    }
    static void SerializeMixins(ulong Amount, uint[] KeyOffsets, string RefTxn, MixOuts mixOuts)
    {
      uint st = 0;
      foreach (uint offset in KeyOffsets)
      {
        st = st + offset;
        mixOuts.get_Outputs_Outs.Add(new get_outputs_out() { amount = Amount, index = st });
        mixOuts.TxnRefs.Add(RefTxn);
      }
    }
    static async Task<List<MixRef>> GetMixins(MixOuts mixOuts)
    {
      var data = new get_outs
      {
        outputs = mixOuts.get_Outputs_Outs.ToArray(),
      };

      var resp = await PostToDaemon(FormatJson(data), MoneroApeSS.ApiConfig.LocalIPAddress + ":" + MoneroApeSS.ApiConfig.MoneroDMNPort + "/get_outs");

      if (string.IsNullOrEmpty(resp))
        throw new Exception("Daemon Error with get_outs");

      var outs = JsonConvert.DeserializeObject<GetOutsResponse>(resp);

      if (outs.outs.Length == 0)
        throw new Exception("Daemon Error with get_outs, lenght 0");

      List<MixRef> refs = new List<MixRef>();

      for (int i = 0; i < outs.outs.Length; i++)
      {
        refs.Add(new MixRef() { Mixin = outs.outs[i].key, TxnRef = mixOuts.TxnRefs[i] });
      }

      return refs;
    }
    static string GetEC_RCT(rct_signatures signatures, int Index)
    {
      if (string.IsNullOrEmpty(signatures.info[Index].mask))
        return signatures.outPk[Index] + signatures.info[Index].amount;

      return signatures.outPk[Index] + signatures.info[Index].mask + signatures.info[Index].amount;
    }
    static string GetEC_Fee(rct_signatures signatures, int Index)
    {
      return signatures.txnFee.ToString();
    }
    static string GetEC_PK(vout[] Vouts, int Index)
    {
      return Vouts[Index].targets.key;
    }

    static string GetEC_PKTag(vout[] Vouts, int Index)
    {
      try
      {
        return Vouts[Index].targets.tagged.key;
      }catch
      { }

      return "";
    }
    static DaemonTxns ParseTransaction(string response)
    {
      DaemonTxns daemonTxn = JsonConvert.DeserializeObject<DaemonTxns>(response);

      foreach (var tx in daemonTxn.txs)
      {
        tx.TxnData = JsonConvert.DeserializeObject<JsonData>(SanitizeReceivedJson(tx.as_json));
        tx.TxPublicKey = GetExtras(tx.TxnData.extra);
      }

      return daemonTxn;
    }
    static string GetExtras(byte[] extra)
    {
      try
      {
        if (extra == null) return null;
        string keyextra = extra.ToHex();
        return keyextra.Substring(keyextra.Length - 64, 64);
      }
      catch { return null; }
    }
    static byte[] ExtraSubarray(byte[] array, int offset, int count)
    {
      var data = new byte[count];

      Buffer.BlockCopy(array, offset, data, 0, count);
      return data;
    }
    static string SanitizeReceivedJson(string uglyJson)
    {
      uglyJson = Regex.Unescape(uglyJson);
      var obj = JObject.Parse(uglyJson);
      return JsonConvert.SerializeObject(obj);
    }
    static string GetTransactionsObject(string[] TxnHashes)
    {
      var data = new
      {
        decode_as_json = true,
        txs_hashes = TxnHashes
      };

      return FormatJson(data);
    }
    public static string GetTransactionsObject(string TxnHash)
    {
      string[] hashes = new string[] { TxnHash };
      var data = new
      {
        decode_as_json = false,
        txs_hashes = hashes
      };

      return FormatJson(data);
    }
    public static string FormatJson(object obj)
    {
      var serializerSettings = new JsonSerializerSettings();
      serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
      return JsonConvert.SerializeObject(obj, serializerSettings);
    }
    public async static Task<string> PostToDaemon(string obj, string url)
    {
      try
      {

        byte[] byteArray = System.Text.Encoding.Default.GetBytes(obj);
        var content = new ByteArrayContent(byteArray);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using (var _HttpClientBrowser = new HttpClient())
        {
          using (var response = await _HttpClientBrowser.PostAsync(url, content))
          {
            response.EnsureSuccessStatusCode();
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(responseStream))
            {
              var res = await streamReader.ReadToEndAsync();

              return res;
            }
          }
        }

      }
      catch (HttpRequestException)
      {
        return null;
      }
    }
    public async static Task<string> GetFromDaemon(string url)
    {
      try
      {
        using (var client = new System.Net.Http.HttpClient())
        {
          var response = await client.GetAsync(url);
          return await response.Content.ReadAsStringAsync();
        }
      }
      catch (HttpRequestException)
      {
        return null;
      }
    }
    public static string GetFeeObject()
    {
      var data = new
      {
        Jsonrpc = "2.0",
        Method = "get_fee_estimate",
        Id = "0",
      };

      return FormatJson(data);
    }
    static string[] GetKeyImages(vin[] Vins)
    {
      List<string> vs = new List<string>();
      foreach (var v in Vins)
      {
        vs.Add(v.key.k_image);
      }

      return vs.ToArray();
    }

  }

}