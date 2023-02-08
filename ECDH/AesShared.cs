using System;
using System.Security.Cryptography;

namespace ApeShift.ECDH
{

  public class AesWrapper
  {

    private Aes _inner;
    private ICryptoTransform _transformer;
    private AesWrapper(Aes aes)
    {
      _inner = aes;
    }

    internal static AesWrapper Create()
    {
      var aes = Aes.Create();
      return new AesWrapper(aes);
    }

    public byte[] Process(byte[] inputBuffer, int inputOffset, int inputCount)
    {
      return _transformer.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
    }

    internal void Initialize(byte[] key, byte[] iv, bool forEncryption)
    {
      if (_transformer != null)
        return;
      _inner.IV = iv;
      _inner.KeySize = key.Length * 8;
      _inner.Key = key;
      _transformer = forEncryption ? _inner.CreateEncryptor() : _inner.CreateDecryptor();
    }
  }

  public class AesBuilder
  {
    private byte[] _key;
    private bool? _forEncryption;

    private byte[] _iv = new byte[16];


    public AesBuilder SetKey(byte[] key)
    {
      _key = key;
      return this;
    }

    public AesBuilder IsUsedForEncryption(bool forEncryption)
    {
      _forEncryption = forEncryption;
      return this;
    }

    public AesBuilder SetIv(byte[] iv)
    {
      _iv = iv;
      return this;
    }

    public AesWrapper Build()
    {
      var aes = AesWrapper.Create();
      var encrypt = !_forEncryption.HasValue || _forEncryption.Value;
      aes.Initialize(_key, _iv, encrypt);
      return aes;
    }
  }

  public class AesUtil
  {
    public static byte[] AesProtect(byte[] bytes, byte[] _Secret)
    {

      var IV = _Secret.SafeSubarray(0, 16);
      var KEY = _Secret.SafeSubarray(16, 16);

      var encryptorForGenerateKey = System.Security.Cryptography.Aes.Create();
      encryptorForGenerateKey.BlockSize = 128;
      encryptorForGenerateKey.KeySize = 128;
      encryptorForGenerateKey.Padding = PaddingMode.Zeros;

      var encryptorTransformer = encryptorForGenerateKey.CreateEncryptor(KEY, IV);
      return encryptorTransformer.TransformFinalBlock(bytes, 0, bytes.Length);

    }
    public static byte[] AesUnprotect(byte[] encrypted2, byte[] _Secret)
    {
      try
      {
        var IV = _Secret.SafeSubarray(0, 16);
        var KEY = _Secret.SafeSubarray(16, 16);

        var decryptor = System.Security.Cryptography.Aes.Create();
        decryptor.BlockSize = 128;
        decryptor.KeySize = 128;
        decryptor.Padding = PaddingMode.Zeros;

        var decryptorTransformer = decryptor.CreateDecryptor(KEY, IV);
        var bts = decryptorTransformer.TransformFinalBlock(encrypted2, 0, encrypted2.Length);

        return bts;
      }
      catch
      {
        return null;
      }

    }
  }
}
