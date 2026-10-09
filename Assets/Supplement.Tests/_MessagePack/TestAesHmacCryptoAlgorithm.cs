using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Supplement.Core;
using Supplement.MessagePack;

namespace Supplement.Tests.MessagePack
{
    public class TestAesHmacCryptoAlgorithm
    {
        private const string Password = "test-password";
        // Magic(4) + Version(1) + SaltLength(2)
        private const int HeaderSize = 7;
        private const int MacSize = 32;

        private AesHmacCryptoAlgorithm algorithm;
        private int saltSize;
        private byte[] plainBytes;

        [SetUp]
        public void SetUp()
        {
            var options = AesOptions.CreateDefault();
            algorithm = new AesHmacCryptoAlgorithm(options);
            saltSize = options.SaltSizeInBytes;
            plainBytes = Encoding.UTF8.GetBytes("sample save data");
        }

        [Test]
        [Description("暗号化したバイト列を同じパスワードで復号すると、元のバイト列に戻る")]
        public void DecryptReturnsOriginalBytes()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);

            CollectionAssert.AreEqual(plainBytes, algorithm.Decrypt(cipher, Password));
        }

        [Test]
        [Description("保存ごとにランダムなソルトを使うため、同じ内容を暗号化しても毎回違うバイト列になる")]
        public void EncryptProducesDifferentBytesEachTime()
        {
            CollectionAssert.AreNotEqual(algorithm.Encrypt(plainBytes, Password), algorithm.Encrypt(plainBytes, Password));
        }

        [Test]
        [Description("暗号文を1ビット書き換えると、復号する前に改ざんとして検知されCryptographicExceptionになる")]
        public void DecryptThrowsWhenCipherIsTampered()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            // CBCではMacが無いと、この書き換えが平文の書き換えとして素通りする
            cipher[HeaderSize + saltSize] ^= 0x01;

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(cipher, Password));
        }

        [Test]
        [Description("末尾の改ざん検知の値(Mac)を書き換えると、照合に失敗しCryptographicExceptionになる")]
        public void DecryptThrowsWhenMacIsTampered()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            cipher[cipher.Length - 1] ^= 0x01;

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(cipher, Password));
        }

        [Test]
        [Description("ソルトを書き換えると導かれる鍵が変わり、照合に失敗しCryptographicExceptionになる")]
        public void DecryptThrowsWhenSaltIsTampered()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            cipher[HeaderSize] ^= 0x01;

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(cipher, Password));
        }

        [Test]
        [Description("末尾が欠けた(書き込みが途中で切れた)データは、照合に失敗しCryptographicExceptionになる")]
        public void DecryptThrowsWhenCipherIsTruncated()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            var truncated = new byte[cipher.Length - 16];
            Buffer.BlockCopy(cipher, 0, truncated, 0, truncated.Length);

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(truncated, Password));
        }

        [Test]
        [Description("違うパスワードで復号すると、照合に失敗しCryptographicExceptionになる")]
        public void DecryptThrowsWhenPasswordIsWrong()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(cipher, "wrong-password"));
        }

        [Test]
        [Description("JSON版(AesCryptoAlgorithm)で暗号化したデータは、形式の識別子(Magic)が違うため読み込まずCryptographicExceptionになる")]
        public void DecryptRejectsJsonVersionFormat()
        {
            var jsonFormatCipher = new AesCryptoAlgorithm(AesOptions.CreateDefault()).Encrypt(plainBytes, Password);

            var e = Assert.Throws<CryptographicException>(() => algorithm.Decrypt(jsonFormatCipher, Password));
            StringAssert.Contains("magic mismatch", e.Message);
        }

        [Test]
        [Description("ヘッダより短い(書き込みの直後に切れた)データも、壊れたデータとしてCryptographicExceptionになる")]
        public void DecryptThrowsWhenShorterThanHeader()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            var headerOnly = new byte[HeaderSize];
            Buffer.BlockCopy(cipher, 0, headerOnly, 0, headerOnly.Length);

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(headerOnly, Password));
        }

        [Test]
        [Description("ヘッダとソルトの後に、暗号文とMacが入る長さが無いデータは、壊れたデータとしてCryptographicExceptionになる")]
        public void DecryptRejectsCipherWithoutRoomForMac()
        {
            var cipher = algorithm.Encrypt(plainBytes, Password);
            var tooShort = new byte[HeaderSize + saltSize + MacSize];
            Buffer.BlockCopy(cipher, 0, tooShort, 0, tooShort.Length);

            Assert.Throws<CryptographicException>(() => algorithm.Decrypt(tooShort, Password));
        }

        [TestCase(null)]
        [TestCase("")]
        [Description("パスワードがnullや空文字なら、JSON版と同じく暗号化せずにArgumentNullExceptionを投げる")]
        public void EncryptRejectsEmptyPassword(string password)
        {
            Assert.Throws<ArgumentNullException>(() => algorithm.Encrypt(plainBytes, password));
        }
    }
}
