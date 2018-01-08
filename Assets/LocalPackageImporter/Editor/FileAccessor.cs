using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Tar;

namespace LocalPackageImporter
{
    /// <summary>
    /// ファイルアクセス周りの操作をまとめた静的クラス
    /// </summary>
    public static class FileAccessor
    {
        /// <summary>
        /// unitypackageが格納されているパスを取得する
        /// </summary>
        /// <returns>unitypackageが格納されているパス</returns>
        public static string GetLocalPackagePath()
        {
            string path = "";
            if (SystemInfo.operatingSystem.Contains("Windows"))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x";
            }
            else if (SystemInfo.operatingSystem.Contains("Mac"))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Library/Unity/Asset Store-5.x";
            }
            else
            {
                Debug.LogWarning("Unknown Operating System.");
                path = "";
            }
            return path;
        }

        /// <summary>
        /// パッケージのメタ情報を保存するパスを取得する
        /// </summary>
        /// <returns></returns>
        public static string GetSavePath()
        {
            string path = "";
            if (SystemInfo.operatingSystem.Contains("Windows"))
            {
                // マイドキュメント配下
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/LocalPackageImporter";
            }
            else if (SystemInfo.operatingSystem.Contains("Mac"))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Library/LocalPackageImporter";
            }
            else
            {
                Debug.LogWarning("Unknown Operating System.");
                path = "";
            }
            return path;
        }

        /// <summary>
        /// 指定ディレクトリのunitypackageファイルリストを取得する
        /// </summary>
        /// <param name="path">指定ディレクトリパス</param>
        /// <returns>unitypackageファイルリスト</returns>
        public static List<string> GetPackageList(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                Debug.LogErrorFormat("Not found path:", path);
                return null;
            }
            FileInfo[] files = dir.GetFiles("*.unitypackage", SearchOption.AllDirectories);

            List<string> pathList = new List<string>();
            for (int i = 0; i < files.Length; ++i)
            {
                pathList.Add(files[i].FullName);
            }
            return pathList;
        }

        /// <summary>
        /// 保持しているunitypackage情報を全て読み込む
        /// </summary>
        /// <param name="ownedPackageInfoList">保持しているパッケージ情報リスト</param>
        /// <param name="packagePath">unitypackageのパス</param>
        /// <param name="infoPath">パッケージ情報を格納しているフォルダのパス</param>
        public static void LoadOwnedPackageInfo(ref List<UnityPackageInfo> ownedPackageInfoList, string packagePath, string infoPath)
        {
            ownedPackageInfoList.Clear();
            List<string> allList = FileAccessor.GetPackageList(packagePath);
            foreach (var path in allList)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                UnityPackageInfo info = new UnityPackageInfo();
                info.name = fileNameNoExt;
                string savePath = infoPath + "/" + fileNameNoExt;
                CreateDirectoryIfNotFound(savePath);
                if (!File.Exists(savePath + "/icon.png"))
                {
                    info.thumb = null;
                }
                else
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes(savePath + "/icon.png"));
                    info.thumb = tex;
                }
                JsonData json = GetJsonData(path, infoPath);
                if(json == null)
                {
                    info.id = null;
                    info.version = null;
                }
                else
                {
                    info.id = json.id;
                    info.version = json.version;
                }
                info.size = GetPackageSize(path);
                info.isFavorite = GetFavoriteState(infoPath, fileNameNoExt);
                ownedPackageInfoList.Add(info);
            }
        }

        /// <summary>
        /// unitypackageからサムネイルを取得しinfoPathフォルダ配下に保存する
        /// （注意：unitypackageを解凍してサムネイルを取り出すので時間がかかります）
        /// </summary>
        /// <param name="packagePath">unitypackageのパス</param>
        /// <param name="infoPath">パッケージ情報を格納するフォルダのパス</param>
        /// <param name="tmpPath">一時フォルダのパス</param>
        public static void ExtractThumbnailsFromPackage(string packagePath, string infoPath, string tmpPath)
        {
            List<string> allList = FileAccessor.GetPackageList(packagePath);

            for (int i = 0; i < allList.Count; ++i)
            {
                float progress = (float)(i + 1) / (float)allList.Count;
                EditorUtility.DisplayProgressBar("Getting unitypackage information", (i + 1).ToString() + "/" + allList.Count.ToString(), progress);

                // ファイル名から拡張子をのぞいた文字列を取得
                string fileNameNoExt = Path.GetFileNameWithoutExtension(allList[i]);
                // サムネイル保存先パス
                string thumbDir = infoPath + "/" + fileNameNoExt;

                // 既にアイコンファイルが存在する場合
                if (File.Exists(thumbDir + "/icon.png"))
                {
                    continue;
                }

                try
                {
                    // unitypackage解凍先パス
                    string tmpDir = tmpPath + "/" + fileNameNoExt;
                    CreateDirectoryIfNotFound(tmpDir);

                    //unitypackage(tar.gz)を読み取り専用で開く
                    using (var tgzStream = File.OpenRead(allList[i]))
                    {
                        //GZipStreamオブジェクトを解凍で生成
                        using (var gzStream = new GZipStream(tgzStream, CompressionMode.Decompress))
                        {
                            //指定したディレクトリにtarを展開
                            ExtractTarByEntry(gzStream, tmpDir, false);
                        }
                    }

                    // サムネイルをinfoPath配下にコピー
                    CreateDirectoryIfNotFound(thumbDir);
                    // unitypackage内にアイコンがない場合はスキップ
                    if (!File.Exists(tmpDir + "/.icon.png"))
                    {
                        continue;
                    }
                    File.Copy(tmpDir + "/.icon.png", thumbDir + "/icon.png", true);

                    // 解凍したパッケージフォルダを削除
                    Directory.Delete(tmpDir, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.ToString());
                    EditorUtility.ClearProgressBar();
                }
            }

            // tmpディレクトリを削除
            try
            {
                if (Directory.Exists(tmpPath))
                {
                    Directory.Delete(tmpPath, true);
                }
                EditorUtility.ClearProgressBar();
            }
            catch(Exception e)
            {
                Debug.LogWarning(e.ToString());
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 指定されたtar内のファイルを指定フォルダに展開する
        /// <see cref="https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#extractFull"/>
        /// </summary>
        /// <param name="gzStream">ストリーム</param>
        /// <param name="targetDir">ターゲットフォルダ</param>
        /// <param name="asciiTranslate">アスキー変換をおこなうかどうかのフラグ</param>
        public static void ExtractTarByEntry(Stream inStream, string targetDir, bool asciiTranslate)
        {
            TarInputStream tarIn = new TarInputStream(inStream);
            TarEntry tarEntry;
            while ((tarEntry = tarIn.GetNextEntry()) != null)
            {
                if (tarEntry.IsDirectory)
                {
                    continue;
                }

                // Converts the unix forward slashes in the filenames to windows backslashes
                string name = tarEntry.Name.Replace('/', Path.DirectorySeparatorChar);

                // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                if (Path.IsPathRooted(name))
                {
                    name = name.Substring(Path.GetPathRoot(name).Length);
                }

                // Apply further name transformations here as necessary
                string outName = Path.Combine(targetDir, name);

                // アイコンファイル以外は展開しない
                if(!Path.GetFileName(outName).Equals(".icon.png"))
                {
                    continue;
                }

                string directoryName = Path.GetDirectoryName(outName);
                Directory.CreateDirectory(directoryName); // Does nothing if directory exists

                FileStream outStr = new FileStream(outName, FileMode.Create);

                if (asciiTranslate)
                {
                    CopyWithAsciiTranslate(tarIn, outStr);
                }
                else
                {
                    tarIn.CopyEntryContents(outStr);
                }
                outStr.Close();
                // Set the modification date/time. This approach seems to solve timezone issues.
                DateTime myDt = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                File.SetLastWriteTime(outName, myDt);
            }
            tarIn.Close();
        }

        private static void CopyWithAsciiTranslate(TarInputStream tarIn, Stream outStream)
        {
            byte[] buffer = new byte[4096];
            bool isAscii = true;
            bool cr = false;

            int numRead = tarIn.Read(buffer, 0, buffer.Length);
            int maxCheck = Math.Min(200, numRead);
            for (int i = 0; i < maxCheck; i++)
            {
                byte b = buffer[i];
                if (b < 8 || (b > 13 && b < 32) || b == 255)
                {
                    isAscii = false;
                    break;
                }
            }
            while (numRead > 0)
            {
                if (isAscii)
                {
                    // Convert LF without CR to CRLF. Handle CRLF split over buffers.
                    for (int i = 0; i < numRead; i++)
                    {
                        byte b = buffer[i]; // assuming plain Ascii and not UTF-16
                        if (b == 10 && !cr)     // LF without CR
                            outStream.WriteByte(13);
                        cr = (b == 13);

                        outStream.WriteByte(b);
                    }
                }
                else
                {
                    outStream.Write(buffer, 0, numRead);
                }
                numRead = tarIn.Read(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// unitypackageのバイナリデータからパッケージ情報を取得しjsonファイルに保存する
        /// <param name="packagePath">unitypackageのパス</param>
        /// <param name="infoPath">パッケージ情報を格納しているフォルダのパス</param>
        /// </summary>
        public static void ExtractUnityPackageInfo(string packagePath, string infoPath)
        {
            List<string> allList = FileAccessor.GetPackageList(packagePath);
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
                    if (magicNum[0] != 0x1F || magicNum[1] != 0x8B)
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
                    if (((flag[0] & 0x04) >> 2) != 1)
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
                    CreateDirectoryIfNotFound(dir);

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
        public static string GetPackageSize(string packagePath)
        {
            string size = "";
            using (var fs = File.OpenRead(packagePath))
            {
                size = ((float)fs.Length / 1000000).ToString("0.0") + " MB";
            }
            return size;
        }

        /// <summary>
        /// アセット情報を取得する
        /// </summary>
        /// <param name="packagePath">unitypackageのパス</param>
        /// <param name="infoPath">パッケージ情報を格納しているフォルダのパス</param>
        /// <returns>アセット情報</returns>
        public static JsonData GetJsonData(string packagePath, string infoPath)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(packagePath);
            string jsonPath = infoPath + "/" + fileNameNoExt + "/info.json";
            if (!File.Exists(jsonPath))
            {
                return null;
            }
            string json = File.ReadAllText(jsonPath);

            // JSONからオブジェクトを作成
            JsonData info = new JsonData();
            info = JsonUtility.FromJson<JsonData>(json);
            return info;
        }

        /// <summary>
        /// お気に入りかどうかを取得する
        /// (favファイルが存在すればお気に入りと見なし、なければお気に入りではない)
        /// </summary>
        /// <param name="infoPath">パッケージ情報を格納しているフォルダのパス</param>
        /// <param name="filenameNoExt">パッケージ名</param>
        /// <returns>お気に入りかどうか</returns>
        public static bool GetFavoriteState(string infoPath, string filenameNoExt)
        {
            string favPath = infoPath + "/" + filenameNoExt + "/fav";
            if (File.Exists(favPath))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// お気に入り情報を更新する
        /// </summary>
        /// <param name="infoPath">パッケージ情報を格納しているフォルダのパス</param>
        /// <param name="info">パッケージ情報</param>
        public static void UpdateFavoriteState(string infoPath, UnityPackageInfo info)
        {
            string favPath = infoPath + "/" + info.name + "/fav";
            if(info.isFavorite)
            {
                if (!File.Exists(favPath))
                {
                    using (File.Create(favPath)) { }
                }
            }
            else
            {
                if (File.Exists(favPath))
                {
                    File.Delete(favPath);
                }
            }
        }

        /// <summary>
        /// 存在しなければディレクトリを作成する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryInfo CreateDirectoryIfNotFound(string path)
        {
            DirectoryInfo info = null;
            if (!Directory.Exists(path))
            {
                info = Directory.CreateDirectory(path);
            } else
            {
                FileInfo fileInfo = new FileInfo(path);
                info = fileInfo.Directory;
            }
            return info;
        }

    } // class
} // namespace

