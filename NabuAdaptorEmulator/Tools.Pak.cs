using System.Security.Cryptography;
using System.Text;

namespace Nabu;

public static partial class Tools
{
    public static string PakName(int pak)
    {
        var pakName = FormatTriple(pak);
        var md5 = MD5.Create();
        var hashed = Encoding.UTF8.GetBytes($"{pakName}nabu");
        var hash = md5.ComputeHash(hashed);
        return string.Join('-',hash.Select(h => Format(h).ToUpper()));
    }

    public static byte[] Unpack(byte[] pak)
    {
        var cipher  = DES.Create();
        cipher.Key  = Constants.PakKey; //new byte[] { 0x6e, 0x58, 0x61, 0x32, 0x62, 0x79, 0x75, 0x7a };
        cipher.IV   = Constants.PakIV;   //new byte[] { 0x0c, 0x15, 0x2b, 0x11, 0x39, 0x23, 0x43, 0x1b };
        cipher.Mode = CipherMode.CBC;
        var data    = cipher.CreateDecryptor().TransformFinalBlock(pak, 0, pak.Length);
        return data;
    }
}
