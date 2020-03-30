using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization;
using System.Data.SQLite;

public class RDBUtility
{
    static public string ToSqlString(object value) {
        if(value == null) return "NULL";
        if(value is string) return "'" + value + "'";
        if(value is DateTime) return "'" + ((DateTime)value).ToString("yyyy/MM/dd HH:mm:ss") + "'";
        return value.ToString();
    }

    static public bool Insert(SQLiteConnection con, string table, object rec) {
        var columns = new StringBuilder();
        var values = new StringBuilder();
        var props = rec.GetType().GetProperties().Where(x=>x.GetCustomAttributes(false).Any(y=>y is DataMemberAttribute));

        foreach(var pro in props) {
            if(columns.Length > 0) columns.Append(",");
            columns.Append(pro.Name);
                
            if(values.Length > 0) values.Append(",");
            values.Append(ToSqlString(pro.GetValue(rec)));
        };

        string sql = "INSERT INTO " + table + " (" + columns + ") VALUES(" + values + ")" ;

        using (var cmd = con.CreateCommand()) {
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery() == 1;
        }
    }

    static public T Select<T>(SQLiteConnection con, string table, string whare=null) where T:class, new() {
        var r = SelectAll<T>(con, table, whare);
        return r.Count > 0 ? r.First() : null;
    }

    static public List<T> SelectAll<T>(SQLiteConnection con, string table, string whare=null) where T:class, new() {
        var sql = "SELECT * FROM " + table;
        if(whare != null)  sql += " WHERE " + whare;

        var list = new List<T>();
        var props = typeof(T).GetProperties().Where(x=>x.GetCustomAttributes(false).Any(y=>y is DataMemberAttribute));

        using(var cmd = con.CreateCommand()) {
            cmd.CommandText = sql;
            using(var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    var rec = new T();
                    for(int i=0; i<reader.FieldCount; i++) {
                        var prop = props.FirstOrDefault(x=>x.Name.Equals(reader.GetName(i), StringComparison.OrdinalIgnoreCase));
                        
                        if(prop != null) {
                            if(prop.PropertyType == typeof(int)) {
                                prop.SetValue(rec, (int)(long)reader[i]);
                            } else if(prop.PropertyType == typeof(DateTime)) {
                                prop.SetValue(rec, DateTime.Parse((string)reader[i]));
                            } else {
                                prop.SetValue(rec, reader[i]);
                            }
                        }
                    }
                    list.Add(rec);
                }
                return list;
            }
        }
    }
}