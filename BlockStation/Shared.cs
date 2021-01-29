using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockStation
{
    public static class Shared
    {
        /// <summary>
        /// アプリケーションパス
        /// </summary>
        public static string ContentRootPath { get; set; }

        /// <summary>
        /// SQLiteデータベースパス
        /// </summary>
        public static string DBPath { get; set; }

        /// <summary>
        /// ログイントークン用メーカー
        /// </summary>
        public static TokenMaker<LoginToken> LoginTokenMaker { get; set; }

        /// <summary>
        /// リフレッシュトークン用メーカー
        /// </summary>
        public static TokenMaker<RefreshToken> RefreshTokenMaker { get; set; }

        /// <summary>
        /// ログイントークン有効期限（秒）
        /// 指定時はtimespan.parseのフォーマット
        /// </summary>
        public static long LoginTokenExp {get; set; }

        /// <summary>
        /// リフレッシュトークン有効期限（秒）
        /// 指定時はtimespan.parseのフォーマット
        /// </summary>
        public static long RefreshTokenExp {get; set; }
    }
}
