using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Web;
using System.IO;

public class JsonSilializer<T>
{
    public static string ToJson(T obj)
    {
        var sil = new DataContractJsonSerializer(typeof(T));
        using (var stream = new System.IO.MemoryStream())
        {
            sil.WriteObject(stream, obj);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }
    public static T FromJson(string json)
    {
        var sil = new DataContractJsonSerializer(typeof(T));
        using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
        {
            return (T)sil.ReadObject(stream);
        }
    }

    public static void WriteFile(string path, T obj)
    {
        var sil = new DataContractJsonSerializer(typeof(T));
        using (var stream = new BufferedStream(new FileStream(path, FileMode.Create, FileAccess.Write)))
        {
            sil.WriteObject(stream, obj);
        }
    }
    public static T ReadFile(string path)
    {
        var sil = new DataContractJsonSerializer(typeof(T));
        using (var stream = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read)))
        {
            return (T)sil.ReadObject(stream);
        }
    }
}