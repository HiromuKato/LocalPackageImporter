using UnityEngine;

namespace LocalPackageImporter
{
    /// <summary>
    /// unitypackage情報を保持する構造体
    /// </summary>
    public struct UnityPackageInfo
    {
        /// <summary>
        /// パッケージ名
        /// </summary>
        public string name;

        /// <summary>
        /// サムネイル画像
        /// </summary>
        public Texture thumb;

        /// <summary>
        /// unitypackageファイルサイズ
        /// </summary>
        public string size;

        /// <summary>
        /// content id
        /// </summary>
        public string id;

        /// <summary>
        /// バージョン
        /// </summary>
        public string version;

        /// <summary>
        /// お気に入りかどうか
        /// </summary>
        public bool isFavorite;
    }
}