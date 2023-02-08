using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace ApeShift.ECDH
{
  public class SharedSession
  {

    private readonly ECPrivateKeyParameters _Private;

    public SharedSession(byte[] UserPrivate)
    {
      _Private = ECKEY.GetECPrivate(UserPrivate);
    }

    public byte[] MyPublicKey()
    {
      return ECKEY.GetPubKey(_Private, true);
    }

    public byte[] SharedSecret(byte[] PeerPublicKey)
    {
      using (SHA256Managed sha = new SHA256Managed())
      {
        return sha.ComputeHash(ECKEY.GetSharedPubkey(_Private, 
          ECKEY.GetECPublic(PeerPublicKey)));
      }
    }

    public byte[] CreateMessage(byte[] message, byte[] PeerPublicKey)
    {
      return AesEncrypt(message, SharedSecret(PeerPublicKey));
    }

    public byte[] ReceiveMessage(byte[] message, byte[] PeerPublicKey)
    {
      return AesDecrypt(message, SharedSecret(PeerPublicKey));
    }

    static byte[] AesEncrypt(byte[] inputByteArray, byte[] _SharedSecret)
    {
      var iv = _SharedSecret.SafeSubarray(0, 16);
      var encryptionKey = _SharedSecret.SafeSubarray(16, 16);
      var aes = new AesBuilder().SetKey(encryptionKey).SetIv(iv).
        IsUsedForEncryption(true).Build();
      return aes.Process(inputByteArray, 0, inputByteArray.Length);
    }

    static byte[] AesDecrypt(byte[] encrypted, byte[] _SharedSecret)
    {
      var iv = _SharedSecret.SafeSubarray(0, 16);
      var encryptionKey = _SharedSecret.SafeSubarray(16, 16);
      var aes = new AesBuilder().SetKey(encryptionKey).SetIv(iv).
        IsUsedForEncryption(false).Build();
      return aes.Process(encrypted, 0, encrypted.Length);
    }
    
  }

}