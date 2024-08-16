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

        public string Value;// { get; private set; }


        public PathUnit(string path) => this.Value = path;

        public static implicit operator string(PathUnit path) => path.Value;
        public static implicit operator PathUnit(string path) => new PathUnit(path);

        /// <summary>
        /// .ToPath() �ŕt�������e�p�X���w�肷��B
        /// �f�t�H���g�� Application.dataPath 
        /// </summary>
        static public string ParentPath { get; private set; } = Application.dataPath;
        /// <summary>
        /// �t���p�X���[�h���Z�b�g����ƁAParentPath ���ω�����B
        /// </summary>
        static public FullPathMode mode
        {
            set
            {
                PathUnit.ParentPath = value switch
                {
                    FullPathMode.PersistentDataPath => Application.persistentDataPath,
                    FullPathMode.DataPath => Application.dataPath,
                    _ => "",
                };
            }
        }

        /// <summary>
        /// true �ɂ���ƁAParentPath �ȉ��ɂ����A�N�Z�X�ł��Ȃ��B�f�t�H���g�� true
        /// </summary>
        static public bool IsAccessWithinParentPathOnly = true;



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


    public static class PathUtilityExtension
    {

        /// <summary>
        /// �P���ɕ������ PathUnit �ŕ��ŕԂ��B
        /// </summary>
        public static PathUnit ToPath(this string path) =>
            new PathUnit(path);



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

        //public static PathUnit ToFullPath(this PathUnit path, ZipArchive archive) =>
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
            //path.IsBlank()
            //||
            path.Value[1..6].Contains(':')// �h���C�u���^�[�A�t�q�h�X�L�[��
            ||
            path.Value[0] == '/'
            ||
            path.IsResource()
            ;




        public static bool IsHttp(this PathUnit path) =>
            path.Value.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
            ||
            path.Value.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase);



        public static bool IsResource(this PathUnit path) =>
            path.Value.EndsWith("as resource", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// as resource ���t������Ă���Ώ����������O���A�����łȂ���� "" ��Ԃ��B
        /// </summary>
        public static ResourceName ToResourceName(this PathUnit path) =>
            path.IsResource()
                ? new ResourceName(path.Value[0..^("as resource".Length)].TrimEnd())
                : new ResourceName("");


        public static bool IsZip(this PathUnit path) =>
            path.Value.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsZipEntry(this PathUnit path) =>
            path.Value.Contains(".zip/", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// �p�X�� .zip/ �ŕ������A.zip �܂ł� / ������Ԃ��B
        /// zip �łȂ���� ("", "") ��Ԃ��B
        /// </summary>
        public static (PathUnit zipPath, PathUnit entryPath) DividZipAndEntry(this PathUnit path)
        {
            var i = path.Value.IndexOf(".zip/", StringComparison.InvariantCultureIgnoreCase);
            return (i >= 0) switch
            {
                true =>
                    divpath(),
                false when path.IsZip() => 
                    (path, ""),
                false =>
                    ("", ""),
            };
            
            (PathUnit, PathUnit) divpath()
            {
                var zippath = path.Value[0..(i + 4)];
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



        static PathUnit show_(this PathUnit fullpath, PathUnit path)
        {
//#if UNITY_EDITOR
//            Debug.Log($"{path.Value} => {fullpath.Value}");
//#endif
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



}


