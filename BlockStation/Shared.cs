using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockStation
{
    public static class Shared
    {
        /// <summary>
        /// SQLiteデータベースパス
        /// </summary>
        public static string DBPath { get; set; }

        /// <summary>
        /// ログイントークン用メーカー
        /// </summary>
        public static TokenMaker<LoginToken> LoginTokenMaker { get; set; }

    }
}
