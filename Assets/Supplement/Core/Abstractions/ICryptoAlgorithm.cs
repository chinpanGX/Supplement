namespace Supplement.Core.Abstractions
{
    public interface ICryptoAlgorithm
    {
        byte[] Encrypt(byte[] plainBytes, string password);
        byte[] Decrypt(byte[] cipherBytes, string password);
    }
}