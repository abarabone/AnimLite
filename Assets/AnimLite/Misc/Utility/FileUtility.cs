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
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;


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
        /// true �ɂ���ƁAParentPath �ȉ��ɂ����A�N�Z�X�ł��Ȃ��B�f�t�H���g�� true
        /// </summary>
        static public bool IsAccessWithinParentPathOnly = true;



        public string Value;// { get; private set; }


        public PathUnit(string path) => this.Value = path;

        public static implicit operator string(PathUnit path) => path.Value;
        public static implicit operator PathUnit(string path) => new PathUnit(path);// �y�����ǂ�߂��ق������������c




        // �p�X�̃L���b�V���BApplication.xxxPath �̓��C���X���b�h���炵����ׂȂ��Ƃ����C�~�t���Ȃ̂Ŏ����Ă���
        public static string CacheFolderPath { get; private set; }
        public static string DataFolderPath { get; private set; }
        public static string PersistentFolderPath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitPath()
        {
            PathUnit.CacheFolderPath = Path.GetFullPath(Application.temporaryCachePath);
            PathUnit.DataFolderPath = Path.GetFullPath(Application.dataPath);
            PathUnit.PersistentFolderPath = Path.GetFullPath(Application.persistentDataPath);

#if UNITY_ANDROID && !UNITY_EDITOR
            PathUnit.ParentPath = Path.GetFullPath(Application.persistentDataPath);
#else
            PathUnit.ParentPath = Path.GetFullPath(Application.dataPath);
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
        /// .ToPath() �ŕt�������e�p�X���w�肷��B
        /// �f�t�H���g�� Application.dataPath 
        /// </summary>
        static public string ParentPath { get; private set; }



        /// <summary>
        /// �t���p�X���[�h���Z�b�g����ƁAParentPath ���ω�����B
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







        // dictionary �p boxing ��� ------------------------------------
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
        // dictionary �p boxing ��� ------------------------------------
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
        /// �P���ɕ������ PathUnit �ŕ��ŕԂ��B
        /// </summary>
        public static PathUnit ToPath(this string path) =>
            new PathUnit(path);


        //public static PathUnit Add(this PathUnit path, string str) =>
        //    (str ?? "") != ""
        //        ? $"{path.Value}/{str}"
        //        : "";




        /// <summary>
        /// �t���p�X�łȂ���� parentpath ��t�����ĕԂ��Bnull �� "" ��Ԃ��B
        /// </summary>
        public static PathUnit ToFullPath(this PathUnit path, string parentpath) =>
            path.IsFullPath() || path.IsBlank()
                ? (path.Value ?? "")
                : $"{parentpath}/{path.Value}"
            ;

        /// <summary>
        /// �t���p�X�łȂ���� PathUnit.ParentPath ��t�����ĕԂ��Bnull �� "" ��Ԃ��B
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
        /// �t���p�X�Ȃ� true ��Ԃ��B
        /// �t���p�X�� �h���C�u���^�[/�t�q�k�X�L�[������n�܂�i�P�`�V�����ڂ܂ł� : ���܂ށj, / ����n�܂�, as resource �ŏI���
        /// </summary>
        public static bool IsFullPath(this PathUnit path) =>
            !path.IsBlank()
            &&
            (
                path.Value[1..6].Contains(':')// �h���C�u���^�[�A�t�q�h�X�L�[��
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
        /// .../xxx.xxx? �̂悤�ɁA�u?�v�ŏI�����̂ɂ��Ή�
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
        /// .../xxx.xxx? �̂悤�ɁA�u?�v�ŏI�����̂ɂ��Ή�
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
        /// as resource ���t������Ă���Ώ����������O���A�����łȂ���� "" ��Ԃ��B
        /// </summary>
        public static ResourceName ToResourceName(this PathUnit path) =>
            path.IsResource()
                ? new ResourceName(path.Value[0..^("as resource".Length)].TrimEnd())
                : new ResourceName("");






        /// <summary>
        /// .zip �ŏI���Ȃ� true ��Ԃ�
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
        /// �p�X�� .zip/ �ŕ������A.zip �܂ł� / ������Ԃ��B
        /// zip �łȂ���� ("", "") ��Ԃ��B
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
        /// �p�X�� .zip/ �ŕ������A/ ������Ԃ��B
        /// zip �łȂ���� "" ��Ԃ��B
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
        /// target �� PathUnit.ParentPath �ȉ��Ȃ� true ��Ԃ��B
        /// ���\�[�X�� web ���ǂ����͍l�����Ȃ��B
        /// </summary>
        static public bool IsWithinParentFolder(this PathUnit target)
        {
            // Path.GetFullPath() �̓p�X�𐳋K�����Ă����B.. �� . ���������Ă���邵�A�V���[�g�������Ă����B
            var _parent = Path.GetFullPath(PathUnit.ParentPath + "/");
            var _target = Path.GetFullPath(target);

            return
                _parent.Length < _target.Length
                &&
                _parent == _target[0 .._parent.Length];
        }

        /// <summary>
        /// target �� PathUnit.ParentPath �̊O���������Ă���� IOException ���X���[����B
        /// �������A���\�[�X�� Http �� web url �ł���΋��e����B
        /// </summary>
        static public void ThrowIfAccessedOutsideOfParentFolder(this PathUnit path)
        {
            if (path.IsHttp() || path.IsResource()) return;
            if (!PathUnit.IsAccessWithinParentPathOnly || path.IsWithinParentFolder()) return;

            throw new IOException("Attempted to access a file beyond the scope of 'PathUnit.ParentPath'.");
        }

    }



    public struct PathList
    {

        public IEnumerable<PathUnit> Paths;// = new EmptyEnumerableStruct<PathUnit>();


        public static implicit operator PathList (PathUnit path) => path.ToPathList();


        // dictionary �p boxing ��� ------------------------------------
        public override bool Equals(object obj)
        {
            return obj is PathList unit && Equals(unit);
        }

        public bool Equals(PathList other)
        {
            return this.Paths.SequenceEqual(other.Paths);
        }

        public override int GetHashCode()
        {
            if (this.Paths == null) return 0;

            return this.Paths
                .Select(x => x.GetHashCode())
                .Aggregate((pre, cur) => HashCode.Combine(pre, cur));
        }
        // dictionary �p boxing ��� ------------------------------------
    }

    public static class PathListExtension
    {
        public static PathList ToPathList(this PathUnit path) => new PathList
        {
            Paths = path.WrapEnumerable(),
        };

        public static PathList Merge(this PathUnit path, PathList append) => new PathList
        {
            Paths = path.WrapEnumerable().Concat(append.Paths),
        };
    }





    //public class PathUnit_Initializer
    //{
    //    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    //    static void Initialize()
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


