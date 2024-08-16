using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;
using UnityEngine.AddressableAssets;

namespace AnimLite.Utility
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using System.IO.Compression;
    //using UnityEditor.VersionControl;
    using static AnimLite.DancePlayable.DanceGraphy2;


    // todo
    // �Ehttp �� zip �͋�������̂őΉ�����
    // �E���\�[�X�Ahttp ��ėp���ł��Ȃ���
    // �E�A�Z�b�g�����[�h�͉\���H�i���f����A�j���[�V��������P�́A�܂� zip �̂悤�ɓ���q�ł������j

    // ���\�[�X�� zip ���T�|�[�g���Ȃ�

    // zip �������� archive ���J��
    // zip �̓t���p�X�̂݁A���΃p�X�ł� zip ���l�����Ȃ�
    // archive �����΃p�X�Ȃ� zip entry �Ƃ��Ē��g�����[�h����
    // archive �ł��t���p�X�Ȃ�Azip entry �ł͂Ȃ�
    // �t���p�X�� zip �łȂ����̂́A���ʂ̃��[�h

    // json �́Azip first entry, zip entry, �Ȃ� zip entry �Ƃ��� archive ���J���Ă���A���̒��� json ���J��
    // 

    public static class DanceSceneLoader
    {

        public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(this PathUnit path, ZipArchive archive, CancellationToken ct)
        {

            var json = await path.LoadJsonAsync<DanceSetJson>(archive, ct);

            return json.ToData();

        }


        public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(this PathUnit path, CancellationToken ct)
        {

            var json = await path.LoadJsonAsync<DanceSetJson>(ct);

            return json.ToData();

        }



    }


    public static class JsonLoader
    {


        public static async ValueTask<T> LoadJsonAsync<T>(
            this PathUnit path, ZipArchive archive, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            if (archive != null)
            {
                var json = path.ToZipEntryPath().Value switch
                {
                    var entrypath when entrypath != "" =>
                        await archive.UnzipAsync(entrypath, DeserializeJsonAsync<T>),
                    _ =>
                        await archive.UnzipFirstEntryAsync(".json", DeserializeJsonAsync<T>),
                    // .json ������ .zip ���̂� entry path ���Q�Ƃ���B
                    // ���̃��f�B�A�ł� .json �ɋL���ꂽ�p�X�� entry path �Ɖ��߂���B
                };

                if (json != null) return json;
            }

            return await path.LoadJsonAsync<T>(ct);
        });



        public static async ValueTask<T> LoadJsonAsync<T>(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, DeserializeJsonAsync<T>),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath).UnzipFirstEntryAsync(".json", DeserializeJsonAsync<T>),
                var (_, _) =>
                    await openAsync_(fullpath).UsingAsync(DeserializeJsonAsync<T>),
            };
        });




        static async ValueTask<T> DeserializeJsonAsync<T>(this Stream s)
        {
            var json = await new StreamReader(s).ReadToEndAsync();

            return JsonUtility.FromJson<T>(json);
        }



    }


}