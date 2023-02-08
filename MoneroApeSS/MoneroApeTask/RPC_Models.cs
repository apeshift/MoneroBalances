using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Web;

namespace MoneroApeTask
{

  public class pool
  {
    public BigInteger amount { get; set; }
    public int confirmations { get; set; }
    public string type { get; set; }
    public string txid { get; set; }
  }
  public class transfers
  {
    public BigInteger amount { get; set; }
    public int global_index { get; set; }
    public bool unlocked { get; set; }
    public string tx_hash { get; set; }
  }

  public class xxTransfers
  {

    [JsonProperty("in")]
    public Incoming[] incoming { get; set; }

    [JsonProperty("out")]
    public Outgoing[] outgoing { get; set; }

    [JsonProperty("pending")]
    public Pending[] pending { get; set; }

  }
  public class Incoming
  {
    public BigInteger amount { get; set; }
    public int confirmations { get; set; }
    public bool double_spend_seen { get; set; }
    public long fee { get; set; }
    public long height { get; set; }
    public string payment_id { get; set; }
    public long timestamp { get; set; }
    public string txid { get; set; }
    public string type { get; set; }
    public long unlock_time { get; set; }

  }

  public class Outgoing
  {
    public BigInteger amount { get; set; }
    public int confirmations { get; set; }
    public bool double_spend_seen { get; set; }
    public long fee { get; set; }
    public long height { get; set; }
    public string payment_id { get; set; }
    public long timestamp { get; set; }
    public string txid { get; set; }
    public string type { get; set; }
    public long unlock_time { get; set; }
  }

  public class Pending
  {
    public BigInteger amount { get; set; }
    public int confirmations { get; set; }
    public bool double_spend_seen { get; set; }
    public long fee { get; set; }
    public ulong height { get; set; }
    public string payment_id { get; set; }
    public long timestamp { get; set; }
    public string txid { get; set; }
    public string type { get; set; }
    public long unlock_time { get; set; }
  }

}