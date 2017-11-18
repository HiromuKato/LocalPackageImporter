using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Tar;
using UnityEditorInternal;

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
        private const string menuName = "Window/UnityPackageImporter";

        /// <summary>
        /// ローカルのunitypackage格納ディレクトリパス
        /// </summary>
        private string localPath;

        /// <summary>
        /// tmpディレクトリパス（サムネイルを取得するためにここに一時的にunitypackageを解凍する）
        /// </summary>
        private string tmpPath;

        /// <summary>
        /// unitypackage情報（サムネイルとjson）を格納するディレクトリパス
        /// </summary>
        private string infoPath;

        /// <summary>
        /// unitypackageファイルのフルパスリスト
        /// </summary>
        private List<string> packagePathList;

        /// <summary>
        /// unitypackage情報を保持する構造体
        /// </summary>
        private struct UnityPackageInfo
        {
            public string name;
            public Texture thumb;
            public string size;
            public string id;
        }
        /// <summary>
        /// 全unitypackage情報のリスト
        /// </summary>
        private List<UnityPackageInfo> allPackageInfo;

        /// <summary>
        /// 表示するunitypackageリスト
        /// </summary>
        private List<UnityPackageInfo> dispList;

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
        /// 検索TextFieldの幅
        /// </summary>
        private readonly int searchWidth = 150;

        /// <summary>
        /// スクロールポジション
        /// </summary>
        private Vector2 scrollPos;

        /// <summary>
        /// Importボタンの幅
        /// </summary>
        private readonly int buttonWidth = 80;

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
            window.titleContent = new GUIContent("Package List");
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
            dispList = new List<UnityPackageInfo>();
            localPath = GetLocalPackagePath();
            tmpPath = Application.dataPath + "/UnityPackageImporter/Editor/tmp";
            infoPath = Application.dataPath + "/UnityPackageImporter/Editor/PackageInfo";
            noImage = (Texture)AssetDatabase.LoadAssetAtPath("Assets/UnityPackageImporter/Editor/noImage.png", typeof(Texture2D));

            // unitypackageファイルのリストを取得する
            packagePathList = GetPackageList(localPath);
            if (packagePathList == null)
            {
                // 不正なディレクトリの場合は終了
                DestroyImmediate(this);
            }

            // PackageInfoフォルダに保持しているunitypackage情報を事前に読み込んでおく
            allPackageInfo = new List<UnityPackageInfo>();
            LoadAllPackageInfo();
        }

        /// <summary>
        /// unitypackageが格納されているパスを取得する
        /// </summary>
        /// <returns>unitypackageが格納されているパス</returns>
        private string GetLocalPackagePath()
        {
            string path = "";
            if (SystemInfo.operatingSystem.Contains ("Windows"))
            {
                path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x";
            }
            else if (SystemInfo.operatingSystem.Contains ("Mac"))
            {
                path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments) + "/Library/Unity/Asset Store-5.x";
            }
            else
            {
                Debug.LogWarning ("Unknown Operating System.");
                path = "";
            }
            return path;
        }

        /// <summary>
        /// GUIを描画する
        /// </summary>
        private void OnGUI()
        {
            if (packagePathList == null)
            {
                packagePathList = GetPackageList(localPath);
                SetPackageInfo();
            }
            EditorGUILayout.Space();

            // GUIになんらかの変更が行われた時、EndChangeCheckがtrueを返す
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    searchWord = GUILayout.TextField(searchWord, GUILayout.Width(searchWidth));
                    GUILayout.Label("Search", EditorStyles.boldLabel);

                    if(GUILayout.Button("Get Package info"))
                    {
                        ExtractUnityPackageInfo();
                        ExtractThumbnailsFromPackage();
                        LoadAllPackageInfo();
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
                SetPackageInfo();
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

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    {
                        EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
                        {
                            if (GUILayout.Button("Import"))
                            {
                                ImportPackage(path);
                            }

                            bool disable = false;
                            if(dispList[i].id == null)
                            {
                                disable = true;
                            }
                            EditorGUI.BeginDisabledGroup(disable);
                            {
                                if (GUILayout.Button("Asset Store"))
                                {
                                    AssetStore.Open("/content/" + dispList[i].id);

                                    // 外部ブラウザで開く場合
                                    //Application.OpenURL("https://www.assetstore.unity3d.com/jp/#!/content/" + dispList[i].id);
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndVertical();

                        if(dispList[i].thumb)
                        {
                            GUILayout.Button(dispList[i].thumb, GUI.skin.box, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                        }
                        else
                        {
                            GUILayout.Button(noImage, GUI.skin.box, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                        }

                        EditorGUILayout.BeginVertical();
                        {
                            GUILayout.Label(fileNameNoExt);
                            GUILayout.Label("サイズ: " + dispList[i].size);
                        }
                        EditorGUILayout.EndVertical();
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
        /// 保持しているunitypackage情報を全て読み込む
        /// </summary>
        private void LoadAllPackageInfo()
        {
            allPackageInfo.Clear();
            List<string> allList = GetPackageList(localPath);
            foreach(var path in allList)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                string dir = infoPath.Replace(Application.dataPath, "Assets");
                UnityPackageInfo info = new UnityPackageInfo();
                info.name = fileNameNoExt;
                info.thumb = (Texture)AssetDatabase.LoadAssetAtPath(dir + "/" + fileNameNoExt + "/icon.png", typeof(Texture2D));
                info.id = GetContentId(path);
                info.size = GetPackageSize(path);
                allPackageInfo.Add(info);
            }
            SetPackageInfo();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 表示するunitypackage情報を設定する(packagePathListが変更されたときに呼ぶ必要がある)
        /// 保持している全unitypackageから名前が一致するものを表示用のリストに追加する
        /// -> サムネイルのLoadAssetやファイルアクセスを都度行うのを避けるためにこのような処理とした
        /// </summary>
        private void SetPackageInfo()
        {
            dispList.Clear();
            for(int i = 0; i < allPackageInfo.Count; ++i)
            {
                for(int j = 0; j < packagePathList.Count; ++j)
                {
                    string filenameNoExt = Path.GetFileNameWithoutExtension(packagePathList[j]);
                    if (allPackageInfo[i].name == filenameNoExt)
                    {
                        dispList.Add(allPackageInfo[i]);
                    }
                }
            }
        }

        /// <summary>
        /// unitypackageからサムネイルを取得しPackageInfoフォルダ配下に保存する
        /// （注意：unitypackageを解凍してサムネイルを取り出すので時間がかかります）
        /// </summary>
        private void ExtractThumbnailsFromPackage()
        {
            List<string> allList = GetPackageList(localPath);

            for (int i = 0; i < allList.Count; ++i)
            {
                float progress = (i+1) / allList.Count;
                EditorUtility.DisplayProgressBar("サムネイル取得中", (i+1).ToString() + "/" + allList.Count.ToString(), progress);

                // ファイル名から拡張子をのぞいた文字列を取得
                string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);
                // サムネイル保存先パス
                string thumbDir = infoPath + "/" + fileNameNoExt;

                // 既にアイコンファイルが存在する場合
                if(File.Exists(thumbDir + "/icon.png" ))
                {
                    continue;
                }

                string tmpDir = tmpPath + "/" + fileNameNoExt;
                // 保存先のディレクトリが存在しない場合は作成する
                if (!Directory.Exists(tmpDir))
                {
                    Directory.CreateDirectory(tmpDir);
                }

                //unitypackage(tar.gz)を読み取り専用で開く
                using (var tgzStream = File.OpenRead(allList[i]))
                {
                    //GZipStreamオブジェクトを解凍で生成
                    using (var gzStream = new GZipStream(tgzStream, CompressionMode.Decompress))
                    {
                        using (var tarArchive = TarArchive.CreateInputTarArchive(gzStream))
                        {
                            //指定したディレクトリにtarを展開
                            tarArchive.ExtractContents(tmpDir);
                        }
                    }
                }

                // 保存先のディレクトリが存在しない場合は作成する
                if (!Directory.Exists(thumbDir))
                {
                    Directory.CreateDirectory(thumbDir);
                }
                // サムネイルをPackageInfo配下にコピー
                File.Copy(tmpDir + "/.icon.png", thumbDir + "/icon.png", true);

                // 解凍したパッケージフォルダを削除
                Directory.Delete(tmpDir, true);
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// unitypackageのバイナリデータからパッケージ情報を取得しjsonファイルに保存する
        /// GZIPのフォーマットについては以下を参照
        /// http://openlab.ring.gr.jp/tsuneo/soft/tar32_2/tar32_2/sdk/TAR_FMT.TXT
        /// </summary>
        private void ExtractUnityPackageInfo()
        {
            List<string> allList = GetPackageList(localPath);
            for (int i = 0; i < allList.Count; ++i)
            {
                // unitypackageファイルを開く
                using (FileStream fs = new FileStream(allList[i], FileMode.Open, FileAccess.Read))
                {
                    // マジックナンバー
                    byte[] magicNum = new byte[2];
                    // フラグ(3bit目が1の場合拡張フィールドが存在する)
                    byte[] flag = new byte[1];
                    // 拡張フィールドのサイズ
                    byte[] extSize = new byte[2];

                    // 現在のFileStreamの位置を保存
                    long fpos = fs.Position;

                    // マジックナンバーの読み込み
                    fs.Read(magicNum, 0, 2);
                    //Debug.Log("magicNum:" + BitConverter.ToString(magicNum).Replace("-", " "));
                    // マジックナンバーの確認
                    if(magicNum[0] != 0x1F || magicNum[1] != 0x8B)
                    {
                        Debug.LogWarning("Invalid unitypackage file.");
                        continue;
                    }

                    // FileStreamが指す位置を4バイト目に移動する
                    fpos = fs.Seek(3, SeekOrigin.Begin);
                    // フラグの読み込み
                    fs.Read(flag, 0, 1);
                    //Debug.Log("flag:" + BitConverter.ToString(flag) + "(16進数), " + Convert.ToString(flag[0], 2) + "(2進数)");
                    // 3bit目が1になっているか（拡張フィールドが存在するか）確認
                    if(((flag[0] & 0x04) >> 2) != 1)
                    {
                        Debug.LogWarning("Extention field not found.");
                        continue;
                    }

                    // FileStreamが指す位置を11バイト目に移動する
                    fpos = fs.Seek(10, SeekOrigin.Begin);
                    // 拡張フィールドのサイズの読み込み
                    fs.Read(extSize, 0, 2);
                    int size = BitConverter.ToInt16(extSize, 0);
                    //Debug.Log("extSize:" + BitConverter.ToString(extSize).Replace("-", " ") + "(16進数), " + size + "(10進数)");
                    byte[] extField = new byte[size];

                    // 拡張フィールドを読み込む
                    fs.Read(extField, 0, size);
                    //Debug.Log("extField:" + BitConverter.ToString(extField).Replace("-", " "));
                    //string str = System.Text.Encoding.UTF8.GetString(extField);
                    //Debug.Log("extField(text):" + str);

                    // 保存先のディレクトリが存在しない場合は作成する
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);
                    string dir = infoPath + "/" + fileNameNoExt;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    // 拡張フィールドの内容をファイルに書き込む
                    using (FileStream outFs = new FileStream(dir + "/info.json", FileMode.Create, FileAccess.Write))
                    {
                        // 4バイト不明なデータが入っているのでoffsetを4としている
                        int offset = 4;
                        outFs.Write(extField, offset, extField.Length - offset);
                    }
                }
            }
        }

        /// <summary>
        /// unitypackageのファイルサイズを取得する
        /// </summary>
        /// <param name="packagePath">ファイルパス</param>
        /// <returns>ファイルサイズ</returns>
        private string GetPackageSize(string packagePath)
        {
            string size = "";
            using (var fs = File.OpenRead(packagePath))
            {
                size = ((float)fs.Length / 1000000).ToString("0.0") + " MB";
            }
            return size;
        }

        /// <summary>
        /// アセットストアのコンテンツIDを取得する
        /// </summary>
        /// <param name="path">unitypackageのパス</param>
        /// <returns></returns>
        private string GetContentId(string path)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
            string jsonPath = infoPath + "/" + fileNameNoExt + "/info.json";
            if(!File.Exists(jsonPath))
            {
                return null;
            }
            string json = File.ReadAllText(jsonPath);

            // JSONからオブジェクトを作成(一通り取得しているが現状はidしか利用していない)
            JsonData info = new JsonData();
            info = JsonUtility.FromJson<JsonData>(json);
            return info.id;
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
