using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;

namespace MoneroApeTask
{
  public class CaptiveWallet
  {
    public async static Task<WalletInfo> GetWalletInfo(transfers[] _incomingTransfers, string poolResp)
    {
      Dictionary<string, WalletOutput> _RPC_Outputs = new Dictionary<string, WalletOutput>();
      Dictionary<string, WalletOutput> _SpentOutputs = new Dictionary<string, WalletOutput>();

      List<string> pool_mixRefs = new List<string>();

      try
      {
        var incomingPool = JsonConvert.DeserializeObject<JResponseWrapper<TPoolResponse>>(poolResp);

        if (incomingPool.Result != null && incomingPool.Result.pool.Length > 0)
        {
          List<string> PoolTxnHashes = new List<string>();
          foreach (var poolreq in incomingPool.Result.pool)
          {
            if (poolreq.confirmations < 1)
            {
              PoolTxnHashes.Add(poolreq.txid);
            }
          }

          if (PoolTxnHashes.Count > 0)
          {
            try
            {
              pool_mixRefs = await Daemon.GetPending(PoolTxnHashes);
            }catch(Exception ex)
            {
              throw new Exception("GetPending | " + ex.Message);
            }
          }
        }
      }
      catch { }

      List<WalletOutput> _UnSpentOutputs = new List<WalletOutput>();

      BigInteger Received = BigInteger.Zero;
      BigInteger Balance = BigInteger.Zero;
      BigInteger Locked = BigInteger.Zero;

      List<string> TxnHashes = new List<string>();


      try
      {
        foreach (var req in _incomingTransfers) TxnHashes.Add(req.tx_hash);
      }
      catch (Exception ex)
      {
        throw new Exception("0 | " + ex.Message);
      }

   

      WalletTransactions txns = null;

      try
      {
        txns = await Daemon.GetOutsForDevice(_incomingTransfers, TxnHashes);
      }
      catch (Exception ex)
      {
        throw new Exception("1 | " + ex.Message);
      }

      if (txns == null)
        throw new Exception("txns null");

      if (txns.Outputs == null)
        throw new Exception("txns outputs null");

      try
      {
        foreach (var txn in txns.Outputs)
        {

          if (!string.IsNullOrEmpty(txn.public_key))
          {
            _RPC_Outputs[txn.public_key] = txn;
          }

        }
     
      }
      catch (Exception ex)
      {
        throw new Exception("a | " + ex.Message);
      }

      try
      {
        foreach (var spent in txns.mixRefs)
        {
          if (_RPC_Outputs.ContainsKey(spent.Mixin))
          {
            _SpentOutputs[spent.Mixin] = _RPC_Outputs[spent.Mixin];
            _SpentOutputs[spent.Mixin].spent_tx_hash = spent.TxnRef;
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception("b | " + ex.Message);
      }

      try
      {
        foreach (var output in _RPC_Outputs.Values)
        {
          Received = BigInteger.Add(Received, output.amount);

          if (!_SpentOutputs.ContainsKey(output.public_key))
          {
            _UnSpentOutputs.Add(output);
            Balance = BigInteger.Add(Balance, output.amount);

            if (!output.unlocked)
            {
              Locked = BigInteger.Add(Locked, output.amount);
            }
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception("c | " + ex.Message);
      }

      try
      {
        if (pool_mixRefs != null && pool_mixRefs.Count > 0)
        {
          foreach (var po in _UnSpentOutputs)
          {
            if (po.unlocked && pool_mixRefs.Contains(po.public_key))
            {
              po.unlocked = false;
              Locked = BigInteger.Add(Locked, po.amount);
            }
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception("d | " + ex.Message);
      }


      try
      {
        return new WalletInfo()
        {
          LockedBalance = Locked,
          Balance = Balance,
          TotalReceived = Received,
          TotalSent = BigInteger.Subtract(Received, Balance),
          SpentOutputs = _SpentOutputs.Values.ToList(),
          UnspentOutputs = _UnSpentOutputs,
          TransactionHashes = TxnHashes.ToArray()
        };
      }
      catch (Exception ex)
      {
        throw new Exception("e | " + ex.Message);
      }




    }
  }

}
