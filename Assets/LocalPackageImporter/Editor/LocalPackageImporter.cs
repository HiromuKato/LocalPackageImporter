using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;

namespace LocalPackageImporter
{
    /// <summary>
    /// ロカールに保持するunitypackageを一覧表示しインポートを可能とするエディタ拡張
    /// </summary>
    public class LocalPackageImporter : EditorWindow, IHasCustomMenu
    {
        /// <summary>
        /// バージョン
        /// </summary>
        public static readonly string Version = "1.0.3";

        /// <summary>
        /// メニュー名
        /// </summary>
        private const string menuName = "Window/Local Package Importer";

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
        /// 保持しているunitypackage情報のリスト
        /// </summary>
        private List<UnityPackageInfo> ownedPackageInfoList;

        /// <summary>
        /// 表示するunitypackageリスト
        /// </summary>
        private List<UnityPackageInfo> dispList;

        /// <summary>
        /// ローカルに保存されているunitypackageの数
        /// </summary>
        private int allPackageNum = 0;

        /// <summary>
        /// サムネイルが見つからなかった場合の代替画像
        /// </summary>
        private Texture noImage;

        /// <summary>
        /// お気に入りON画像
        /// </summary>
        private Texture heart_on;

        /// <summary>
        /// お気に入りOFF画像
        /// </summary>
        private Texture heart_off;

        /// <summary>
        /// お気に入りのON/OFFトグル
        /// </summary>
        private bool heartToggle = false;

        /// <summary>
        /// ハートトグルのスタイル
        /// </summary>
        private GUIStyle heartToggleStyle;

        /// <summary>
        /// ハートボタンのスタイル
        /// </summary>
        private GUIStyle heartButtonStyle;

        /// <summary>
        /// サムネイルのスタイル
        /// </summary>
        private GUIStyle thumbStyle;

        /// <summary>
        /// スクロールポジション
        /// </summary>
        private Vector2 scrollPos;

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
        /// ボタンの幅
        /// </summary>
        private readonly int buttonWidth = 80;

        /// <summary>
        /// スクロールエリアの高さ
        /// </summary>
        private float rootHeight = 0f;

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
            var window = EditorWindow.GetWindow(typeof(LocalPackageImporter));

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
            localPath = FileAccessor.GetLocalPackagePath();
            infoPath = FileAccessor.GetSavePath();
            if(infoPath.Equals(""))
            {
                Debug.LogError("ERROR");
                return;
            }
            // ※tmpPathのフォルダは削除されるので変更する場合は注意してください
            tmpPath = infoPath + "/tmp";
            noImage = (Texture)AssetDatabase.LoadAssetAtPath("Assets/LocalPackageImporter/Editor/Images/noImage.png", typeof(Texture2D));
            heart_on = (Texture)AssetDatabase.LoadAssetAtPath("Assets/LocalPackageImporter/Editor/Images/heart_on.png", typeof(Texture2D));
            heart_off = (Texture)AssetDatabase.LoadAssetAtPath("Assets/LocalPackageImporter/Editor/Images/heart_off.png", typeof(Texture2D));

            // unitypackageファイルのリストを取得する
            packagePathList = FileAccessor.GetPackageList(localPath);
            if (packagePathList == null)
            {
                // 不正なディレクトリの場合は終了
                DestroyImmediate(this);
            }
            // ローカルに持つ全unitypackage数
            allPackageNum = packagePathList.Count;

            // infoPathフォルダに保持しているunitypackage情報を事前に読み込んでおく
            ownedPackageInfoList = new List<UnityPackageInfo>();
            FileAccessor.LoadOwnedPackageInfo(ref ownedPackageInfoList, localPath, infoPath);
            SetDisplayPackageInfo();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// GUIを描画する
        /// </summary>
        private void OnGUI()
        {
            if (packagePathList == null)
            {
                packagePathList = FileAccessor.GetPackageList(localPath);
                SetDisplayPackageInfo();
            }
            EditorGUILayout.Space();

            // GUIになんらかの変更が行われた時、EndChangeCheckがtrueを返す
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    // ハートトグルボタンのスタイル設定
                    if (heartToggleStyle == null)
                    {
                        heartToggleStyle = new GUIStyle(GUI.skin.button);
                        heartToggleStyle.margin = new RectOffset(6, 0, 0, 0);
                        heartToggleStyle.padding = new RectOffset(0, 0, 0, 0);
                    }
                    heartToggle = GUILayout.Toggle(heartToggle, heartToggle ? heart_on : heart_off, heartToggleStyle, GUILayout.Width(20), GUILayout.Height(20));

                    // 検索
                    searchWord = GUILayout.TextField(searchWord, GUILayout.Width(searchWidth));
                    string count = "(" + packagePathList.Count + "/" + allPackageNum + ")";
                    GUILayout.Label("Search" + count, EditorStyles.boldLabel);

                    if(GUILayout.Button("Update metadata"))
                    {
                        UpdateMetadata();
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
                    packagePathList = FileAccessor.GetPackageList(localPath);
                }

                if(heartToggle)
                {
                    // ハートがついたもののみ表示する
                    packagePathList = GetFavoritePackage();
                }

                SetDisplayPackageInfo();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.Space();

            var scrollArea = EditorGUILayout.BeginHorizontal();
            {
                // スクロールビュー
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUI.skin.box);
                {
                    // 一項目分の高さ
                    int lineHeight = 74;

                    // 上部の描画が不要なエリアをスペースで埋める
                    var startIndex = (int)(scrollPos.y / lineHeight);
                    GUILayout.Space(startIndex * lineHeight);

                    var listCount = packagePathList.Count;
                    var endIndex = listCount;
                    if (rootHeight > 0f)
                    {
                        // 空白が下部に表示されないようにするためにこのカウント分追加で描画する
                        int marginCount = 3;
                        endIndex = startIndex + (int)(rootHeight / lineHeight) + marginCount;
                        if (endIndex > listCount)
                        {
                            endIndex = listCount;
                        }
                    }

                    for (int i = startIndex; i < endIndex; ++i)
                    {
                        string path = packagePathList[i];
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
                            {
                                if (GUILayout.Button("Import"))
                                {
                                    AssetDatabase.ImportPackage(path, true);
                                }

                                bool disable = false;
                                if (dispList[i].id == null)
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

                                // ハートボタンのスタイル設定
                                if (heartButtonStyle == null)
                                {
                                    heartButtonStyle = new GUIStyle(GUI.skin.label);
                                    heartButtonStyle.margin = new RectOffset(32, 0, 0, 0);
                                }
                                if (GUILayout.Button(dispList[i].isFavorite ? heart_on : heart_off, heartButtonStyle))
                                {
                                    PressedFavorite(i);
                                }
                            }
                            EditorGUILayout.EndVertical();

                            // サムネイルのスタイル設定
                            if (thumbStyle == null)
                            {
                                thumbStyle = new GUIStyle(GUI.skin.box);
                                thumbStyle.margin = new RectOffset(0, 0, 0, 0);
                                thumbStyle.padding = new RectOffset(0, 0, 0, 0);
                            }
                            if (dispList[i].thumb)
                            {
                                GUILayout.Button(dispList[i].thumb, thumbStyle, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                            }
                            else
                            {
                                GUILayout.Button(noImage, thumbStyle, GUILayout.Width(thumbWitdh), GUILayout.Height(thumbHeight));
                            }

                            EditorGUILayout.BeginVertical();
                            {
                                GUILayout.Label(fileNameNoExt);
                                GUILayout.Label("Size: " + dispList[i].size);
                                GUILayout.Label("Version: " + dispList[i].version);
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    // 下部の描画が不要なエリアをスペースで埋める
                    GUILayout.Space((listCount - endIndex) * lineHeight);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndHorizontal();

            // スクロールエリアの描画が完了したタイミングで更新
            if (scrollArea.height > 0f)
            {
                rootHeight = scrollArea.height;
            }

            /*
            // for DEBUG
            GUILayout.Label("ScrollArea:" + rootHeight);
            GUILayout.Label("Window Size : (" + Screen.width.ToString() + ", " + Screen.height.ToString() + ")", EditorStyles.boldLabel);
            Handles.color = Color.red;
            Handles.DrawLine(new Vector2(0, 0), new Vector2(100, 100));
            */
        }

        /// <summary>
        /// unitypackageのメタデータを取得・アップデートします
        /// </summary>
        private void UpdateMetadata()
        {
            // 実行時間計測
            // System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            FileAccessor.ExtractUnityPackageInfo(localPath, infoPath);
            FileAccessor.ExtractThumbnailsFromPackage(localPath, infoPath, tmpPath);
            FileAccessor.LoadOwnedPackageInfo(ref ownedPackageInfoList, localPath, infoPath);
            SetDisplayPackageInfo();
            AssetDatabase.Refresh();

            // watch.Stop();
            // float ms = watch.ElapsedMilliseconds;
            // Debug.LogFormat("{0} ms", ms);
        }

        /// <summary>
        /// パッケージを検索する
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <returns>検索キーワードを含むunitypackageのファイルリスト</returns>
        private List<string> SearchPackage(string keyword)
        {
            List<string> allList = FileAccessor.GetPackageList(localPath);
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
        /// 表示するunitypackage情報を設定する(packagePathListが変更されたときに呼ぶ必要がある)
        /// 保持しているunitypackageから名前が一致するものを表示用のリストに追加する
        /// -> サムネイルのLoadAssetやファイルアクセスを都度行うのを避けるためにこのような処理とした
        /// </summary>
        private void SetDisplayPackageInfo()
        {
            dispList.Clear();
            for(int i = 0; i < ownedPackageInfoList.Count; ++i)
            {
                for(int j = 0; j < packagePathList.Count; ++j)
                {
                    string filenameNoExt = Path.GetFileNameWithoutExtension(packagePathList[j]);
                    if (ownedPackageInfoList[i].name == filenameNoExt)
                    {
                        dispList.Add(ownedPackageInfoList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// お気に入りのパッケージを表示する（検索キーワードが入力されている場合も考慮）
        /// </summary>
        /// <returns>表示するパッケージのフルパスリスト</returns>
        private List<string> GetFavoritePackage()
        {
            List<string> allList = FileAccessor.GetPackageList(localPath);
            List<string> pathList = new List<string>();
            for (int i = 0; i < allList.Count; ++i)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);
                for(int j = 0; j < packagePathList.Count; ++j)
                {
                    // 検索後のパッケージパスリスト
                    string afterFileNameNoExt = Path.GetFileNameWithoutExtension(packagePathList[j]);

                    // 所有しているパッケージの名称と一致し、かつお気に入り、かつ検索後のパッケージパスと一致する場合
                    if (ownedPackageInfoList[i].name == fileNameNoExt &&
                        ownedPackageInfoList[i].isFavorite &&
                        ownedPackageInfoList[i].name == afterFileNameNoExt)
                    {
                        pathList.Add(allList[i]);
                    }
                }
            }
            return pathList;
        }

        /// <summary>
        /// （各パッケージの）お気に入りボタンが押された場合
        /// </summary>
        /// <param name="index">押されたボタンのインデックス</param>
        private void PressedFavorite(int index)
        {
            UnityPackageInfo info = dispList[index];
            info.isFavorite = !info.isFavorite;
            dispList[index] = info;
            FileAccessor.UpdateFavoriteState(infoPath, dispList[index]);

            // ownedPackageInfoList更新(本来は該当する項目だけの更新としたほうがよい)
            FileAccessor.LoadOwnedPackageInfo(ref ownedPackageInfoList, localPath, infoPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// メニューに項目を追加する
        /// </summary>
        /// <param name="menu">メニュー</param>
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("About"), false, () => {
                EditorUtility.DisplayDialog(
                    "About",
                    "Local Package Importer Version " + Version + "\n\n" +
                    "Copyright 2017 Hi-Rom",
                    "OK");
            });

            menu.AddItem(new GUIContent("Open unitypackage Folder"), false, () => {
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    System.Diagnostics.Process.Start(localPath);
                }
                else if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    EditorUtility.RevealInFinder(localPath);
                }
                else
                {
                    Debug.LogWarning("This operating system is not supported.");
                }
            });

            menu.AddItem(new GUIContent("Open MetaData Folder"), false, () => {
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    System.Diagnostics.Process.Start(infoPath);
                }
                else if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    EditorUtility.RevealInFinder(infoPath);
                }
                else
                {
                    Debug.LogWarning("This operating system is not supported.");
                }
            });
        }

    } // class
} // namespace
