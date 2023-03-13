using System.Security.Cryptography;
using System.Text;

namespace AES_CBC;

public class AesCrypto
{
   /// <summary>
   ///  Takes a string that has to be encrypted
   /// </summary>
   /// <param name="plainText"></param>
   /// <returns>a unrecognizable string (Encrypted)</returns>
    public  string Encrypter(string plainText)
    {
        byte[] cipherData;
        Aes aes = Aes.Create();
        //Get the Key from SHA256 Hashing Algorithm  
        aes.Key = GetPassphraseKeyBytes();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        //passes both hashed Key and random Initialization vector for encryption algorithm
        ICryptoTransform cipher = aes.CreateEncryptor(aes.Key, aes.IV);

        //Creates virtual memory for storage of unsigned byte array
        using (MemoryStream ms = new())
        {
            //Crypto stream allows for transitions between streams 
            using (CryptoStream cs = new(ms, cipher, CryptoStreamMode.Write))
            {
                //StreamWrite is used for writing characters to a stream
                using (StreamWriter sw = new(cs))
                {
                    sw.Write(plainText);
                }
            }

            cipherData = ms.ToArray();
        }
        //here is the sensitive data concatinated into a longer string which can be deconcatinated in Decrypter for decryption
        byte[] combinedData = new byte[aes.IV.Length + cipherData.Length];
        Array.Copy(aes.IV,0,combinedData,0,aes.IV.Length);
        Array.Copy(cipherData,0,combinedData,aes.IV.Length,cipherData.Length);
        return Convert.ToBase64String(combinedData);
    }

   /// <summary>
   /// Decryptes encrypted string 
   /// </summary>
   /// <param name="combinedString"></param>
   /// <returns>Decrypted string</returns>
    public  string Decrypter(string combinedString)
    {
        string plainText;
        byte[] combinedData = Convert.FromBase64String(combinedString);
        Aes aes = Aes.Create();
        aes.Key = GetPassphraseKeyBytes();
        //here we get the IV via deconcatination
        byte[] iv = new byte[aes.BlockSize / 8];
        byte[] cipherText = new byte[combinedData.Length - iv.Length];
        Array.Copy(combinedData,iv,iv.Length);
        Array.Copy(combinedData,iv.Length,cipherText,0,cipherText.Length);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;

        ICryptoTransform decipher = aes.CreateDecryptor(aes.Key, aes.IV);

        using (MemoryStream ms = new(cipherText))
        {
            using (CryptoStream cs = new(ms,decipher,CryptoStreamMode.Read))
            {
                using (StreamReader sr = new (cs))
                {
                    plainText = sr.ReadToEnd();
                }
            }

            return plainText;
        }
    }
    
   /// <summary>
   /// Here we get our secretkey HASHED via SHA256 Hashing algorithm
   /// </summary>
   /// <returns>byte[] of hashed string </returns>
    private  byte[] GetPassphraseKeyBytes()
    {
        
        byte[] passphrase = Encoding.UTF8.GetBytes("secretKey");

        using SHA256 sha256 = SHA256.Create();
        byte[] inputBytes = passphrase;

        return sha256.ComputeHash(inputBytes);
    }
}
