using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Tar;

namespace UnityPackageImporter
{
    /// <summary>
    /// ロカールにあるunitypackageを一覧表示しインポートを可能とするエディタ拡張
    /// </summary>
    public class UnityPackageImporter : EditorWindow
    {
        /// <summary>
        /// メニュー名
        /// </summary>
        private const string menuName = "Tool/UnityPackageImporter";

        /// <summary>
        /// ローカルのunitypackage格納ディレクトリパス
        /// </summary>
        private string localPath;

        /// <summary>
        /// tmpディレクトリパス（サムネイルを取得するためにここに一時的にunitypackageを解凍する）
        /// </summary>
        private string tmpPath;

        /// <summary>
        /// サムネイル画像を格納するディレクトリパス
        /// </summary>
        private string thumbPath;

        /// <summary>
        /// unitypackageファイルのフルパスリスト
        /// </summary>
        private List<string> packagePathList;

        /// <summary>
        /// サムネイル情報を保持する構造体
        /// </summary>
        private struct ThumbInfo
        {
            public string name;
            public Texture thumb;
        }
        /// <summary>
        /// 全サムネイル情報のリスト
        /// </summary>
        private List<ThumbInfo> allThumbInfo;

        /// <summary>
        /// 表示に利用するサムネイルリスト
        /// </summary>
        private List<Texture> thumbList;

        /// <summary>
        /// サムネイルが見つからなかった場合の代替画像
        /// </summary>
        private Texture noImage;

        /// <summary>
        /// サムネイル表示幅
        /// </summary>
        private readonly int thumbWitdh = 64;

        /// <summary>
        /// サムネイル表示高さ
        /// </summary>
        private readonly int thumbHeight = 64;

        /// <summary>
        /// 検索ワード
        /// </summary>
        private string searchWord = "";

        /// <summary>
        /// スクロールポジション
        /// </summary>
        private Vector2 scrollPos;

        /// <summary>
        /// Importボタンの幅
        /// </summary>
        private readonly int buttonWidth = 60;

        /// <summary>
        /// Importボタンの高さ
        /// </summary>
        private readonly int buttonHeight = 20;

        /// <summary>
        /// package名の幅
        /// </summary>
        private readonly int nameWidth = 250;

        /// <summary>
        /// １項目分の共通の高さ
        /// </summary>
        private readonly int height = 64;

        /// <summary>
        /// Windowを表示する
        /// </summary>
        [MenuItem(menuName)]
        public static void ShowWindow()
        {
            // 実行可能でない場合は終了する
            if (!IsExecutable())
            {
                return;
            }

            // すでにWindowSampleが存在すればそのインスタンスを取得し、なければ生成する
            var window = EditorWindow.GetWindow(typeof(UnityPackageImporter));

            // Windowのタイトルを設定する
            window.titleContent = new GUIContent("ImportPackage");
        }

        /// <summary>
        /// メニューが実行可能かどうかを返す
        /// （実行可能な場合はメニューを有効、実行不可能な場合にはメニューを無効にする）
        /// </summary>
        /// <returns>メニューの実行有無</returns>
        [MenuItem(menuName, true)]
        private static bool IsExecutable()
        {
            return !EditorApplication.isPlaying && !Application.isPlaying && !EditorApplication.isCompiling;
        }

        /// <summary>
        /// 初期化処理を行う
        /// </summary>
        private void OnEnable()
        {
            thumbList = new List<Texture>();
            localPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x";
            tmpPath = Application.dataPath + "/UnityPackageImporter/Editor/tmp";
            thumbPath = Application.dataPath + "/UnityPackageImporter/Editor/Thumbs";
            noImage = (Texture)AssetDatabase.LoadAssetAtPath("Assets/UnityPackageImporter/Editor/noImage.png", typeof(Texture2D));

            // unitypackageファイルのリストを取得する
            packagePathList = GetPackageList(localPath);
            if (packagePathList == null)
            {
                // 不正なディレクトリの場合は終了
                DestroyImmediate(this);
            }

            // 保持している全サムネイルを事前に読み込んでおく
            allThumbInfo = new List<ThumbInfo>();
            LoadAllThumbnails();
        }

        /// <summary>
        /// GUIを描画する
        /// </summary>
        private void OnGUI()
        {
            if (packagePathList == null)
            {
                packagePathList = GetPackageList(localPath);
                SetThumbnails();
            }
            EditorGUILayout.Space();

            // GUIになんらかの変更が行われた時、EndChangeCheckがtrueを返す
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    searchWord = GUILayout.TextField(searchWord);
                    GUILayout.Label("Search", EditorStyles.boldLabel);

                    if(GUILayout.Button("Get Thumbnails."))
                    {
                        GetThumbnailsFromPackage();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                if(searchWord != "")
                {
                    // 検索
                    packagePathList = SearchPackage(searchWord);
                }
                else
                {
                    // 空白のときは全てのパッケージを表示する
                    packagePathList = GetPackageList(localPath);
                }
                SetThumbnails();
            }
            EditorGUILayout.Space();

            // スクロールビュー
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUI.skin.box);
            {
                // unitypackageリスト
                for( int i = 0; i < packagePathList.Count; ++i)
                {
                    string path = packagePathList[i];
                    // 拡張子を除いたファイル名
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                    // ディレクトリパス（ファイル名を除いたもの）
                    //string directryPath = Path.GetDirectoryName(path);

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    {
                        if (GUILayout.Button("Import", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                        {
                            ImportPackage(path);
                        }

                        if(thumbList[i])
                        {
                            GUILayout.Button(thumbList[i], GUI.skin.box, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                        }
                        else
                        {
                            GUILayout.Button(noImage, GUI.skin.box, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                        }

                        GUILayout.Label(fileNameNoExt, GUILayout.Width(nameWidth), GUILayout.Height(height));
                        //GUILayout.Label(directryPath, GUILayout.Height(height));
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 指定ディレクトリのunitypackageファイルリストを取得する
        /// </summary>
        /// <param name="path">指定ディレクトリパス</param>
        /// <returns>unitypackageファイルリスト</returns>
        private List<string> GetPackageList(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                Debug.LogErrorFormat("Not found path:", path);
                return null;
            }
            FileInfo[] files = dir.GetFiles("*.unitypackage", SearchOption.AllDirectories);

            List<string> pathList = new List<string>();
            for(int i = 0; i< files.Length; ++i)
            {
                pathList.Add(files[i].FullName);
            }
            return pathList;
        }

        /// <summary>
        /// パッケージを検索する
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <returns>検索キーワードを含むunitypackageのファイルリスト</returns>
        private List<string> SearchPackage(string keyword)
        {
            List<string> allList = GetPackageList(localPath);
            List<string> pathList = new List<string>();
            for (int i = 0; i< allList.Count; ++i)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);
                if (fileNameNoExt.ToLower().Contains(keyword.ToLower()))
                {
                    pathList.Add(allList[i]);
                }
            }
            return pathList;
        }

        /// <summary>
        /// 保持しているサムネイル画像を全て読み込む
        /// </summary>
        private void LoadAllThumbnails()
        {
            allThumbInfo.Clear();
            List<string> allList = GetPackageList(localPath);
            foreach(var path in allList)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                string dir = thumbPath.Replace(Application.dataPath, "Assets");
                ThumbInfo info = new ThumbInfo();
                info.name = fileNameNoExt;
                info.thumb = (Texture)AssetDatabase.LoadAssetAtPath(dir + "/" + fileNameNoExt + "/icon.png", typeof(Texture2D));
                allThumbInfo.Add(info);
            }
            SetThumbnails();
        }

        /// <summary>
        /// 表示用のサムネイル画像を設定する(packagePathListが変更されたときに呼ぶ必要がある)
        /// 保持している全サムネイルリストから名前が一致するものを表示用のリストに追加する
        /// -> 都度LoadAssetするのを避けるためにこのような処理とした
        /// </summary>
        private void SetThumbnails()
        {
            thumbList.Clear();
            for(int i = 0; i < allThumbInfo.Count; ++i)
            {
                for(int j = 0; j < packagePathList.Count; ++j)
                {
                    string filenameNoExt = Path.GetFileNameWithoutExtension(packagePathList[j]);
                    if (allThumbInfo[i].name == filenameNoExt)
                    {
                        thumbList.Add(allThumbInfo[i].thumb);
                    }
                }
            }
        }

        /// <summary>
        /// unitypackageからサムネイルを取得しThumbsフォルダ配下に保存する
        /// （注意：unitypackageを解凍してサムネイルを取り出すので時間がかかります）
        /// </summary>
        private void GetThumbnailsFromPackage()
        {
            List<string> allList = GetPackageList(localPath);

            for (int i = 0; i < allList.Count; ++i)
            {
                float progress = (i+1) / allList.Count;
                EditorUtility.DisplayProgressBar("サムネイル取得中", (i+1).ToString() + "/" + allList.Count.ToString(), progress);

                // ファイル名から拡張子をのぞいた文字列を取得
                string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);

                // 既に同ディレクトリがThumbsフォルダ内に存在する場合
                if(Directory.Exists(thumbPath + "/" + fileNameNoExt))
                {
                    continue;
                }

                // ディレクトリ作成
                DirectoryInfo tmpDir = Directory.CreateDirectory(tmpPath + "/" + fileNameNoExt);

                //unitypackage(tar.gz)を読み取り専用で開く
                using (var tgzStream = File.OpenRead(allList[i]))
                //GZipStreamオブジェクトを解凍で生成
                using (var gzStream = new GZipStream(tgzStream, CompressionMode.Decompress))
                using (var tarArchive = TarArchive.CreateInputTarArchive(gzStream))
                {
                    //指定したディレクトリにtarを展開
                    tarArchive.ExtractContents(tmpDir.FullName);
                }

                // サムネイルをThumbs配下にコピー
                DirectoryInfo thumbDir = Directory.CreateDirectory(thumbPath + "/" + fileNameNoExt);
                File.Copy(tmpDir.FullName + "/.icon.png", thumbDir.FullName + "/icon.png", true);

                // 解凍したパッケージフォルダを削除
                Directory.Delete(tmpDir.FullName, true);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            LoadAllThumbnails();
        }

        /// <summary>
        /// パッケージをインポートする
        /// </summary>
        /// <param name="path">インポートするunitypackageのパス</param>
        private void ImportPackage(string path)
        {
            AssetDatabase.ImportPackage(path, true);
        }

    } // class
} // namespace
