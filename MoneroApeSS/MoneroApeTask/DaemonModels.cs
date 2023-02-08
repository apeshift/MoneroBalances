using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneroApeTask
{
  class GetOutsResponse
  {
    public outs[] outs { get; set; }
  }
  class outs
  {
    public string key { get; set; }
  }
  class get_outs
  {
    public get_outputs_out[] outputs { get; set; }
    public bool get_txid { get; set; }
  }

  class get_outputs_out
  {
    public ulong amount { get; set; }
    public uint index { get; set; }
  }
  class MixOuts
  {
    public List<get_outputs_out> get_Outputs_Outs { get; set; }
    public List<string> TxnRefs { get; set; }
  }
  class DaemonTxns
  {
    public txs[] txs { get; set; }
  }
  class txs
  {
    public string as_json { get; set; }
    public string tx_hash { get; set; }
    public long block_height { get; set; }
    public long block_timestamp { get; set; }
    public int[] output_indices { get; set; }
    public JsonData TxnData { get; set; }
    public string TxPublicKey { get; set; }
    public string PaymentID { get; set; }

  }
  class JsonData
  {
    [JsonProperty("vout")]
    public vout[] vouts { get; set; }


    [JsonProperty("vin")]
    public vin[] vins { get; set; }


    [JsonProperty("rct_signatures")]
    public rct_signatures signatures { get; set; }

    public byte[] extra { get; set; }
  }
  class vout
  {

    [JsonProperty("target")]
    public target targets { get; set; }


  }

  class vin
  {
    public key key { get; set; }
  }

  class key
  {
    public string k_image { get; set; }
    public ulong amount { get; set; }
    public uint[] key_offsets { get; set; }

  }

  class tagged_key
  {
    public string key { get; set; }
  }
  class target
  {
    public string key { get; set; }


    [JsonProperty("tagged_key")]
    public tagged_key tagged { get; set; }
  }

  class rct_signatures
  {
    public long txnFee { get; set; }
    public string[] outPk { get; set; }

    [JsonProperty("ecdhInfo")]
    public ecdhInfo[] info { get; set; }

  }
  class ecdhInfo
  {
    public string amount { get; set; }
    public string mask { get; set; }
  }
}
