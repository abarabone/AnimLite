using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Data.Common;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif

namespace AnimLite.Utility
{
    public static class LocalEncoding
    {
        public static Encoding sjis = Encoding.GetEncoding("shift_jis");
    }

    public static class EncodeUtility
    {

        public static string ToUtf8(this string src)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sjis = LocalEncoding.sjis;
            var utf8 = Encoding.UTF8;

            var srcbytes = sjis.GetBytes(src);
            var dstbytes = Encoding.Convert(sjis, utf8, srcbytes);

            var res = utf8.GetString(dstbytes);
            return res;
        }

    }




    public enum FullPathMode
    {
        ForceDirectPath,
        DataPath,
        PersistentDataPath,
    }

    [Serializable]
    public struct PathUnit : IEquatable<PathUnit>
    {
        /// <summary>
        /// true にすると、ParentPath 以下にしかアクセスできない。デフォルトは true
        /// </summary>
        static public bool IsAccessWithinParentPathOnly = true;



        public string Value;// { get; private set; }


        public PathUnit(string path) => this.Value = path;

        public static implicit operator string(PathUnit path) => path.Value;
        public static implicit operator PathUnit(string path) => new PathUnit(path);// 楽だけどやめたほうがいいかも…




        // パスのキャッシュ。Application.xxxPath はメインスレッドからしかよべないというイミフさなので持っておく
        public static string CacheFolderPath { get; private set; }
        public static string DataFolderPath { get; private set; }
        public static string PersistentFolderPath { get; private set; }

        public static void InitPath()
        {
            PathUnit.CacheFolderPath = Application.temporaryCachePath;
            PathUnit.DataFolderPath = Application.dataPath;
            PathUnit.PersistentFolderPath = Application.persistentDataPath;

#if UNITY_ANDROID && !UNITY_EDITOR
            PathUnit.ParentPath = Application.persistentDataPath;
#else
            PathUnit.ParentPath = Application.dataPath;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"CacheFolderPath : {PathUnit.CacheFolderPath}".ShowDebugLog();
            $"DataFolderPath : {PathUnit.DataFolderPath}".ShowDebugLog();
            $"PersistentFolderPath : {PathUnit.PersistentFolderPath}".ShowDebugLog();
            $"ParentPath : {PathUnit.ParentPath}".ShowDebugLog();
#endif
        }

        //static PathUnit()
        //{
        //    PathUnit.InitPath();
        //}


        /// <summary>
        /// .ToPath() で付加される親パスを指定する。
        /// デフォルトは Application.dataPath 
        /// </summary>
        static public string ParentPath { get; private set; }



        /// <summary>
        /// フルパスモードをセットすると、ParentPath が変化する。
        /// </summary>
        static public FullPathMode mode
        {
            set
            {
                PathUnit.ParentPath = value switch
                {
                    FullPathMode.PersistentDataPath => PathUnit.PersistentFolderPath,
                    FullPathMode.DataPath => PathUnit.DataFolderPath,
                    _ => "",
                };
            }
        }







        // dictionary 用 boxing 回避 ------------------------------------
        public override bool Equals(object obj)
        {
            return obj is PathUnit unit && Equals(unit);
        }

        public bool Equals(PathUnit other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
        // dictionary 用 boxing 回避 ------------------------------------
    }






    public struct ResourceName
    {
        public string Value { get; private set; }

        public ResourceName(string name) => this.Value = name;

        public static implicit operator string(ResourceName res) => res.Value;
    }

    public struct QueryString
    {
        public string Value { get; private set; }

        public QueryString(string name) => this.Value = name;


        public static implicit operator string(QueryString res) => res.Value;

        public static PathUnit operator +(PathUnit path, QueryString queryString) =>
            (queryString.Value ?? "") != ""
                ? $"{path.Value}?{queryString.Value}"
                : path;
    }





    public static class PathUtilityExtension
    {



        /// <summary>
        /// 単純に文字列を PathUnit で包んで返す。
        /// </summary>
        public static PathUnit ToPath(this string path) =>
            new PathUnit(path);


        //public static PathUnit Add(this PathUnit path, string str) =>
        //    (str ?? "") != ""
        //        ? $"{path.Value}/{str}"
        //        : "";




        /// <summary>
        /// フルパスでなければ parentpath を付加して返す。null は "" を返す。
        /// </summary>
        public static PathUnit ToFullPath(this PathUnit path, string parentpath) =>
            path.IsFullPath() || path.IsBlank()
                ? (path.Value ?? "")
                : $"{parentpath}/{path.Value}"
            ;

        /// <summary>
        /// フルパスでなければ PathUnit.ParentPath を付加して返す。null は "" を返す。
        /// </summary>
        public static PathUnit ToFullPath(this PathUnit path) =>
            path.ToFullPath(PathUnit.ParentPath)
            .show_(path)
            ;

        //public static PathUnit ToFullPath(this PathUnit path, IArchive archive) =>
        //    archive == null
        //        ? path.ToFullPath(PathUnit.ParentPath)
        //        : path
        //    .show_(path)
        //    ;


        /// <summary>
        /// フルパスなら true を返す。
        /// フルパスは ドライブレター/ＵＲＬスキームから始まる（１～７文字目までに : を含む）, / から始まる, as resource で終わる
        /// </summary>
        public static bool IsFullPath(this PathUnit path) =>
            !path.IsBlank()
            &&
            (
                path.Value[1..6].Contains(':')// ドライブレター、ＵＲＩスキーム
                ||
                path.Value[0] == '/'
                ||
                path.IsResource()
            );




        public static bool IsHttp(this PathUnit path) =>
            path.Value.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
            ||
            path.Value.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase);


        /// <summary>
        /// .../xxx.xxx? のように、「?」で終わるものにも対応
        /// </summary>
        public static PathUnit TrimQueryString(this PathUnit weburl)
        {
            var ist = weburl.Value.IndexOf('?');
            if (ist == -1) return weburl;

            var ied = weburl.Value.IndexOf('/', ist);
            if (ied == -1) return weburl.Value[..ist];

            var st = weburl.Value[..ist];
            var ed = weburl.Value[ied..];
            return (st + ed).ToPath();
        }

        /// <summary>
        /// .../xxx.xxx? のように、「?」で終わるものにも対応
        /// </summary>
        public static (PathUnit path, QueryString querystring) DividToPathAndQueryString(this PathUnit weburl)
        {
            var ist = weburl.Value.IndexOf('?');
            if (ist == -1) return (weburl, new QueryString(""));

            var ied = weburl.Value.IndexOf('/', ist);
            if (ied == -1) return (weburl.Value[..ist], new QueryString(weburl.Value[(ist+1)..]));

            var st = weburl.Value[..ist];
            var ed = weburl.Value[ied..];
            var queryString = weburl.Value[(ist+1)..ied];
            return ((st + ed).ToPath(), new QueryString(queryString));
        }




        public static bool IsResource(this PathUnit path) =>
            path.Value.EndsWith("as resource", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// as resource が付加されていれば除去した名前を、そうでなければ "" を返す。
        /// </summary>
        public static ResourceName ToResourceName(this PathUnit path) =>
            path.IsResource()
                ? new ResourceName(path.Value[0..^("as resource".Length)].TrimEnd())
                : new ResourceName("");






        /// <summary>
        /// .zip で終わるなら true を返す
        /// </summary>
        public static bool IsZipArchive(this PathUnit path) =>
            path.Value.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);



        /// <summary>
        /// 
        /// </summary>
        public static bool IsZipEntry(this PathUnit path) =>
            path.Value.Contains(".zip/", StringComparison.InvariantCultureIgnoreCase);
            //||
            //path.Value.Contains(".zip");

        /// <summary>
        /// パスを .zip/ で分割し、.zip までと / より後ろを返す。
        /// zip でなければ ("", "") を返す。
        /// </summary>
        public static (PathUnit zipPath, PathUnit entryPath) DividZipToArchiveAndEntry(this PathUnit path)
        {
            var i = path.Value.IndexOf(".zip/", StringComparison.InvariantCultureIgnoreCase);
            return (i >= 0) switch
            {
                true =>
                    divpath(),
                false when path.IsZipArchive() => 
                    (path, ""),
                false =>
                    ("", ""),
            };
            
            (PathUnit, PathUnit) divpath()
            {
                var zippath = path.Value[..(i + 4)];
                var entrypath = path.Value[(i + 5)..];
                return (zippath, entrypath);
            }
        }

        /// <summary>
        /// パスを .zip/ で分割し、/ より後ろを返す。
        /// zip でなければ "" を返す。
        /// </summary>
        public static PathUnit ToZipEntryPath(this PathUnit path)
        {
            var i = path.Value.IndexOf(".zip/", StringComparison.InvariantCultureIgnoreCase);
            if (i == -1) return "".ToPath();

            return path.Value[(i + 5)..];
        }






        public static string GetExt(this PathUnit path) =>
            Path.GetExtension(path);

        //public static PathUnit GetExtEx(this PathUnit path) =>
        //    Path.GetExtension(path)
        //        .Split("?", StringSplitOptions.RemoveEmptyEntries)[0]
        //        .ToPath();







        static PathUnit show_(this PathUnit fullpath, PathUnit path)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{path.Value} => {fullpath.Value}");
#endif
            return fullpath;
        }



        static public bool IsBlank(this PathUnit path) =>
            (path.Value ?? "") == "";
            //path.Value == default || path.Value == "";


        //public static PathType DetectPathType(this PathUnit path)
        //{
        //    var isResource = path.IsResource();
        //    var isWeb = path.IsHttp();
        //    var isZip = 
        //}


        /// <summary>
        /// target が PathUnit.ParentPath 以下なら true を返す。
        /// リソースや web かどうかは考慮しない。
        /// </summary>
        static public bool IsWithinParentFolder(this PathUnit target)
        {
            var _parent = Path.GetFullPath(PathUnit.ParentPath + "/");
            var _target = Path.GetFullPath(target);

            return
                _parent.Length < _target.Length
                &&
                _parent == _target[0 .._parent.Length];
        }

        /// <summary>
        /// target が PathUnit.ParentPath の外側をさしていれば IOException をスローする。
        /// ただし、リソースや Http の web url であれば許容する。
        /// </summary>
        static public void ThrowIfAccessedOutsideOfParentFolder(this PathUnit path)
        {
            if (path.IsHttp() || path.IsResource()) return;
            if (!PathUnit.IsAccessWithinParentPathOnly || path.IsWithinParentFolder()) return;

            throw new IOException("Attempted to access a file beyond the scope of 'PathUnit.ParentPath'.");
        }

    }



    //public class PathUnit_Initializer
    //{

    //    static PathUnit_Initializer()
    //    {
    //        PathUnit.InitPath();
    //    }


    //}

#if UNITY_EDITOR
    [InitializeOnLoad]
    public class PathUnit_EditorInitializer
    {
        static PathUnit_EditorInitializer()
        {
            PathUnit.InitPath();
        }
    }

    //public class PathUnit_BuildProcessor : IPreprocessBuildWithReport
    //{
    //    public int callbackOrder => 0;

    //    public void OnPreprocessBuild(BuildReport report)
    //    {
    //        PathUnit.InitPath();
    //    }
    //}
#endif


}


