using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApeShift.ECDH
{
  public class Utils
  {

    public static bool IsPublicKeyValid(string PublicKey)
    {
      try
      {
        var param = ECKEY.GetECPublic(PublicKey.HexToByteArray());
        var test = ECKEY.GetPubKey(param, true);
        if (test.ToHex().ToLower() == PublicKey.ToLower())
          return true;

        return false;
      }
      catch
      {
        return false;
      }
    }

  }
}
