using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// URLセーフで署名付きのトークンを生成します。
/// 署名にはHMAC-SHA256を利用し、文字列はBase64URLエンコードされます。
/// </summary>
public class AuthTokenMaker
{
    private byte[] key;
    private HMACSHA256 hmac;

    public AuthTokenMaker(string key)
    {
        this.key = Encoding.UTF8.GetBytes(key);
        hmac = new HMACSHA256(this.key);
    }

    /// <summary>
    /// トークンを生成します。
    /// </summary>
    /// <param name="data">データ部</param>
    public string MakeToken(string data)
    {
        string b64 = toBase64url(data);
        var hash = GetHash(b64);
        return b64 + "." + hash;
    }

    /// <summary>
    /// トークンの正否をチェックします。
    /// </summary>
    /// <param name="code">トークンコード</param>
    /// <returns>true:正</returns>
    public bool CheckToken(string code)
    {
        if (code == null) return false;
        var spl = code.Split('.');
        if (spl.Length != 2) return false;

        var hash = GetHash(spl[0]);
        return hash == spl[1];
    }

    private string GetHash(string base64str)
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(base64str));
        StringBuilder buil = new StringBuilder(hash.Length * 2);
        for (int i=0; i < hash.Length; i++) {
            buil.Append(hash[i].ToString("X2"));
        }
        return buil.ToString();
    }



    public static string toBase64url(string data)
    {
        string code = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        code = code.TrimEnd( '=' );
        code = code.Replace( '+', '-' );
        code = code.Replace( '/', '_' );
        return code;
    }
    public static string fromBase64url(string code)
    {
        int pads = (4 - code.Length % 4) % 4;
        code += new string('=', pads);
        code = code.Replace('-', '+');
        code = code.Replace('_', '/');

        var bytes = Convert.FromBase64String(code);
        return Encoding.UTF8.GetString(bytes);    
    }
}
