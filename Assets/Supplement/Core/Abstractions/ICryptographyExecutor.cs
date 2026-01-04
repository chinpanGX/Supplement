namespace Supplement.Core
{
    public interface ICryptographyExecutor
    {
        byte[] Encrypt(string plainText, string password);
        string Decrypt(byte[] cipherTextBytes, string password);
    }
}