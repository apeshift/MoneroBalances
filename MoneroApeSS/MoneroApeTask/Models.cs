using ApeShift;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MoneroApeTask
{

  public class WalletTask
  {
    public MoneroRequest MoneroRequest { get; set; }
    public string Port { get; set; }
    public UserInfo User { get; set; }
  }
  public class StartInfo
  {
    public bool is_import { get; set; }
    public bool new_account { get; set; }
    public DateTime? created_time { get; set; }
    public int start_height { get; set; }
    public string address { get; set; }
  }
  public class UserInfo
  {
    public string MoneroAddress { get; set; }
    public string MoneroViewKey { get; set; }
    public Guid FileName { get; set; }
    public string FilePassword { get; set; }
    public string FullFileName { get; set; }
    public bool WalletCreated { get; set; }
  }
  public enum SyncType
  {
    NA = 0,
    CLI = 1,
    RPC = 2
  }
  public class JError
  {
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }
  }

  public class JResponseWrapper<T>
  {
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("jsonrpc")]
    public string Jsonrpc { get; set; }

    [JsonProperty("error")]
    public JError Error { get; set; }

    [JsonProperty("result")]
    public T Result { get; set; }
  }
  public class TResponse
  {
    public transfers[] transfers { get; set; }
  }
  public class TPoolResponse
  {
    public pool[] pool { get; set; }
  }

  class HeightObjectResp
  {
    public long height { get; set; }
  }

  class FeeObjectResp
  {
    public long fee { get; set; }
  }
  public class WalletInfo
  {
    public long WalletHeight { get; set; }
    public long DaemonHeight { get; set; }
    public long FeesEstimate { get; set; }
    public BigInteger Balance { get; set; }
    public BigInteger TotalSent { get; set; }
    public BigInteger TotalReceived { get; set; }
    public BigInteger LockedBalance { get; set; }
    public List<WalletOutput> SpentOutputs { get; set; }
    public List<WalletOutput> UnspentOutputs { get; set; }
    public string[] TransactionHashes { get; set; }
    public string ProcessID { get; set; }
  }

  public class WalletOutput
  {
    public BigInteger amount { get; set; }
    public long global_index { get; set; }
    public uint index { get; set; }
    public string public_key { get; set; }
    public string tx_pub_key { get; set; }
    public string rct { get; set; }
    public string tx_hash { get; set; }
    public string spent_tx_hash { get; set; }
    public long height { get; set; }
    public long timestamp { get; set; }
    public bool unlocked { get; set; }
    public string txn_fee { get; set; }
    public string extra { get; set; }
  }

  public class WalletTransactions
  {
    public List<WalletOutput> Outputs { get; set; }
    public List<MixRef> mixRefs { get; set; }
  }
  public class MixRef
  {
    public string Mixin { get; set; }
    public string TxnRef { get; set; }
  }
  public class WalletInput
  {
    public string KeyImage { get; set; }
    public List<string> MixIns { get; set; }
  }
}
