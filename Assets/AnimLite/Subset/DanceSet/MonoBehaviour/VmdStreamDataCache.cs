using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;
using VRM;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;
    using System.IO.Compression;

    /// <summary>
    /// �u�l�c�f�[�^���L���b�V������B
    /// �L�[�̓p�X�B�t�F�C�X�}�b�v���قȂ���͈̂قȂ�f�[�^�Ƃ��ĕێ�����B
    /// �ێ������f�[�^�� �{�f�B��]�A�{�f�B�ʒu�A�t�F�C�X�A�ł���A��������X�g���[���L���b�V�����������A�R�A�f�[�^�Ƃ�����B
    /// �X�g���[���L���b�V����t������ɂ́A.CloneShallowlyWithCache() �Ńf�[�^�̃N���[�����쐬����B
    /// VmdStreamDataCache.enabled �� false �ɂȂ����Ƃ��L���b�V���̓N���A�����B
    /// �������u�l�c�f�[�^�͎Q�ƃJ�E���g�t���ŁA�Q�Ƃ��S�ă[���ɂȂ������ɔj�������B
    /// �Q�ƃJ�E���g�̓R�A�f�[�^�������A�N���[���쐬���A�ɂP�����Z�����B
    /// </summary>
    public class VmdStreamDataCache : MonoBehaviour
    {

        VmdCacheDictionary Cache = new();


        public async Awaitable OnDisable()
        {
            await this.Cache.ClearCache();
        }




        public Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct)
        =>
            this.GetOrLoadVmdStreamDataAsync(vmdFilePath, faceMapFilePath, null, ct);


        public async Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, ZipArchive archive, CancellationToken ct)
        {
            var (_vmddata, facemap) = await this.Cache.GetOrLoadAsync(vmdFilePath, faceMapFilePath, archive, ct);

            var vmddata = _vmddata.CloneShallowlyWithCache();

            return (vmddata, facemap);
        }


    }

}