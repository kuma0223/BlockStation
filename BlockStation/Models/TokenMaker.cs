using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// URLセーフで署名付きのJWTトークンを生成します。
/// 署名にはHMAC-SHA256を利用し、文字列はBase64URLエンコードされます。
/// トークンの生成およびチェックを行う場合はキーが必要です。
/// 復元にはキーは使用されません。
/// </summary>
public class TokenMaker<T>
{
    private byte[] key;
    private HMACSHA256 hmac;

    /// <summary>
    /// ハッシュキーを指定してトークン生成を開始します。
    /// </summary>
    /// <param name="key">ハッシュキー</param>
    public TokenMaker(string key)
    {
        this.key = Encoding.UTF8.GetBytes(key);
        hmac = new HMACSHA256(this.key);
    }

    /// <summary>
    /// トークンを生成します。
    /// </summary>
    /// <param name="obj">データオブジェクト</param>
    public string MakeToken(T obj) {
        var json = JsonSilializer<T>.ToJson(obj);
        return MakeTokenJson(json);
    }

    /// <summary>
    /// トークンを生成します。
    /// </summary>
    /// <param name="json">JSONデータ文字列</param>
    private string MakeTokenJson(string json) {
        string header = "{\"type\":\"JWT\",\"alg\":\"HS256\"}";
        string b64 = $"{ToBase64url(header)}.{ToBase64url(json)}";
        var hash = GetHash(b64);
        return $"{b64}.{hash}";
    }

    /// <summary>
    /// トークンを復元します。
    /// </summary>
    /// <param name="token">トークン文字列</param>
    /// <returns>データオブジェクト</returns>
    public T AnalyseToken(string token) {
        var json = AnalyseTokenJson(token);
        return JsonSilializer<T>.FromJson(json);
    }

    /// <summary>
    /// トークンを復元します。
    /// </summary>
    /// <param name="token">トークン文字列</param>
    /// <returns>JSONデータ文字列</returns>
    private string AnalyseTokenJson(string token) {
        var spl = token.Split('.');
        var json = FromBase64url(spl[1]);
        return json;
    }

    /// <summary>
    /// トークンの正否をチェックします。
    /// </summary>
    /// <param name="code">トークンコード</param>
    /// <returns>true:正</returns>
    public bool CheckToken(string token)
    {
        if (token == null) return false;
        var spl = token.Split('.');
        if (spl.Length != 3) return false;

        var hash = GetHash($"{spl[0]}.{spl[1]}");
        return hash == spl[2];
    }

    /// <summary>
    /// ハッシュ値を取得します。
    /// </summary>
    /// <param name="str">入力文字列</param>
    /// <returns>ハッシュ値</returns>
    private string GetHash(string str)
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(str));
        StringBuilder buil = new StringBuilder(hash.Length * 2);
        for (int i=0; i < hash.Length; i++) {
            buil.Append(hash[i].ToString("X2"));
        }
        return buil.ToString();
    }

    /// <summary>
    /// Base64URLにエンコードします。
    /// </summary>
    /// <param name="data">入力文字列</param>
    /// <returns>エンコード結果</returns>
    private static string ToBase64url(string data)
    {
        string code = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        code = code.TrimEnd( '=' );
        code = code.Replace( '+', '-' );
        code = code.Replace( '/', '_' );
        return code;
    }

    /// <summary>
    /// Base64URLをデコードします。
    /// </summary>
    /// <param name="code">Base64URL文字列</param>
    /// <returns>デコード結果</returns>
    private static string FromBase64url(string code)
    {
        int pads = (4 - code.Length % 4) % 4;
        code += new string('=', pads);
        code = code.Replace('-', '+');
        code = code.Replace('_', '/');

        var bytes = Convert.FromBase64String(code);
        return Encoding.UTF8.GetString(bytes);    
    }
}
