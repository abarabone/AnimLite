using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;
using Unity.Mathematics;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;
    using AnimLite.Loader;

    using AnimLite.Vrm;
    using AnimLite.Vmd;
    using static AnimLite.DancePlayable.DanceGraphy;


    public static class DanceSceneLoader
    {

        public static bool UseSeaquentialLoading = false;

        public static ZipMode ZipLoaderMode = ZipMode.ParallelOpenMultiFiles;
        public enum ZipMode
        {
            Sequential,
            ParallelOpenSingleFile,
            ParallelOpenMultiFiles,
        }


        /// <summary>
        /// FileStream で完全な非同期モードを使用する。ただしサイズが 3MB 以上のファイルのみ。
        /// </summary>
        public static bool UseAsyncModeForFileStreamApi = false;



        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<IArchive> OpenArchiveAsync(this PathUnit archivepath, IArchive fallback, CancellationToken ct)
        {
            if (archivepath.IsBlank()) return fallback;

            var (fullpath, queryString) = archivepath.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath switch
            {
                _ when fullpath.IsZipArchive() || fullpath.IsZipEntry() =>
                    DanceSceneLoader.ZipLoaderMode switch
                    {
                        ZipMode.Sequential when DanceSceneLoader.UseSeaquentialLoading =>
                            await fullpath.OpenZipArchiveSequentialAsync(queryString, fallback, ct),
                        ZipMode.Sequential =>
                            await fullpath.OpenZipArchiveSequentialConcurrentAsync(queryString, fallback, ct),
                        ZipMode.ParallelOpenSingleFile =>
                            await fullpath.OpenZipArchiveParallelAsync(queryString, fallback, ct),
                        ZipMode.ParallelOpenMultiFiles =>
                            await fullpath.OpenDummyArchiveParallelAsync(queryString, fallback, ct),
                        _ =>
                            default,
                    },
                _ =>
                    fullpath.OpenFolderArchive(fallback, ct),
            };
        }

        public static ValueTask<IArchive> OpenArchiveAsync(this PathUnit path, CancellationToken ct) =>
            path.OpenArchiveAsync(null, ct);




        public static (PathUnit archivePath, PathUnit entryPath, QueryString queryString) DividPath(
            this PathUnit fullpath, string extensionList)
        {
            if (fullpath.IsResource()) return ("", fullpath, "");
            // いずれアセットバンドル？的なもののパスをかえせるようになりたい


            var (path, queryString) = fullpath.DividToPathAndQueryString();


            if (path.IsZipArchive())
            {
                return (path, "", queryString);
            }


            var isFile = extensionList.Split(';')
                .Where(ext => path.Value.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                .Any();

            // フォルダ
            if (!isFile)
            {
                return (fullpath, "", "");
            }

            // ファイル
            {
                // 下記だと http://.../xx.xxx のときに / が \ に返られてしまうので使えない。しかも // は \ になる
                //var archivePath = Path.GetDirectoryName(path);
                //var entryPath = Path.GetFileName(path).ToPath();

                // / と \ が混在しているかも知れないので両方やる
                var ix = path.Value.LastIndexOf('/');
                var iy = path.Value.LastIndexOf('\\');
                var i = math.max(ix, iy);
                var archivePath = path.Value[..i];
                var entryPath = path.Value[(i + 1)..];

                return (archivePath, entryPath, queryString);
            }
        }



        //public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(
        //    this PathUnit path, IArchive archive, CancellationToken ct)
        //{

        //    var json = await archive.LoadJsonAsync<DanceSetJson>(path, ct);

        //    return json.ToData();

        //}


        //public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(
        //    this PathUnit path, CancellationToken ct)
        //{

        //    var json = await path.LoadJsonAsync<DanceSetJson>(ct);

        //    return json.ToData();

        //}



        //public static async IAsyncEnumerable<(DanceSetJson danceset, IArchive archive)> LoadDaceSceneAsync(
        //    this IEnumerable<PathUnit> jsonpaths, IArchive fallbackArchive, CancellationToken ct)
        //{
        //    IArchive ac = null;
        //    DanceSetJson ds = null;

        //    foreach (var path in jsonpaths)
        //    {
        //        ac = await path.OpenWhenZipAsync(archive, ct);

        //        ds = await ac.LoadJsonAsync<DanceSetJson>(path, ds, ct);

        //        yield return (ds, archive);
        //    }
        //}


        public struct ArchiveDanceScene : IDisposable
        {
            public IArchive archive;
            public DanceSceneJson dancescene;

            public void Dispose() => this.archive?.Dispose();

            public void Deconstruct(out IArchive archive, out DanceSceneJson dancescene)
            {
                archive = this.archive;
                dancescene = this.dancescene;
            }
        }

        public static ValueTask<ArchiveDanceScene> LoadDanceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, CancellationToken ct) =>
                jsonpaths.LoadDaceSceneAsync(null, null, ct);

        public static async ValueTask<ArchiveDanceScene> LoadDaceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, IArchive fallbackArchive, DanceSceneJson dancescene, CancellationToken ct)
        {
            IArchive ac = fallbackArchive;
            DanceSceneJson ds = dancescene;

            foreach (var path in jsonpaths.Where(x => !x.IsBlank()))
            {
                var (archpath, entpath, qstr) = path.DividPath(".json");

                ac = await (archpath + qstr).OpenArchiveAsync(ac, ct);

                ds = await ac.LoadJsonAsync<DanceSceneJson>(entpath, ds, ct);
            }

            return new ArchiveDanceScene
            {
                archive = ac,
                dancescene = ds
            };
        }

    }
}

