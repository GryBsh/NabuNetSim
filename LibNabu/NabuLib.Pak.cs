using System.Security.Cryptography;
using System.Text;

namespace Nabu;

public static partial class NabuLib
{
    /// <summary>
    ///     Gets the name of a PAK file, which is the MD5 hash
    ///     of the `000000nabu` where `000000` is the PAK ID 
    ///     in 3 octets. 
    /// </summary>
    /// <param name="pakId">the ID of the desired PAK</param>
    /// <returns></returns>
    public static string PakName(int pakId)
    {
        var pakName = FormatTriple(pakId);
        var md5 = MD5.Create();
        var hashed = Encoding.UTF8.GetBytes($"{pakName}nabu");
        var hash = md5.ComputeHash(hashed);
        return string.Join('-', hash.Select(h => Format(h).ToUpper()));
    }

    /// <summary>
    ///     Converts PAK file data into the raw segment spool
    /// </summary>
    /// <param name="pakData">The raw PAK data in bytes</param>
    /// <returns>A Segment spool</returns>
    public static byte[] Unpak(byte[] pakData)
    {
        var cipher = DES.Create();
        cipher.Key = Constants.PakKey; //new byte[] { 0x6e, 0x58, 0x61, 0x32, 0x62, 0x79, 0x75, 0x7a };
        cipher.IV = Constants.PakIV;   //new byte[] { 0x0c, 0x15, 0x2b, 0x11, 0x39, 0x23, 0x43, 0x1b };
        cipher.Mode = CipherMode.CBC;
        var data = cipher.CreateDecryptor().TransformFinalBlock(pakData, 0, pakData.Length);
        return data;
    }
}