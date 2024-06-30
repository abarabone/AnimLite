using UnityEngine;
using UnityEngine.Playables;
using System;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;

    using AnimLite.Vmd;
    using AnimLite.Vrm;


    public class VmdPlayerWithPlayableJob : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // �t�@�C������f�[�^��ǂ݉���
            var vmdpath = this.VmdFilePath.ToFullPath();
            var facemap = this.FaceMappingFilePath.ToFullPath();
            var vmdStreamData = await VmdParser.ParseVmdAsync(vmdpath, this.destroyCancellationToken);
            var faceMapping = await VrmParser.ParseFaceMapExAsync(facemap, this.destroyCancellationToken);

            // �f�[�^�𗘗p�ł���`���ɕϊ�����
            using var rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData();
            using var pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData();
            using var face_data = vmdStreamData.faceKeyStreams.CreateFaceData(faceMapping);

            // �f�[�^�A�N�Z�X�����������邽�߂̍������쐬����
            using var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            using var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            using var face_index = face_data.CreateIndex(indexBlockLength: 100);

            // ���Ԕ͈͂Ȃǂ̏����������^�C�}�[���쐬����
            var timer = new StreamingTimer(rot_data.GetLastKeyTime());

            // Forward �ŗ��p����L�[�L���b�V���o�b�t�@�𐶐�����
            using var rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            using var pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            using var face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);


            // �q���[�}�m�C�h���f���̏����\�z����
            using var bone = this.anim.BuildVmdPlayableJobTransformMappings();
            var face = this.faceRenderer.sharedMesh.BuildStreamingFace(faceMapping);




            // �L�[�����I�u�W�F�N�g���\�z����
            // �W�F�l���N�X�ɂ��u�L�[��ԕ����A���Ԃ̃N���b�v���@�v���w��ł���
            // �i�������@�� job �̒��ŁAAbsolute �� Forward ��K�X�g��������悤�ɂȂ��Ă���j

            var rotKeyFinder = rot_data
                .ToKeyFinder(rot_cache, rot_index)
                .With<Key4CatmulRot, Clamp>();

            var posKeyFinder = pos_data
                .ToKeyFinder(pos_cache, pos_index)
                .With<Key4CatmulPos, Clamp>();

            var faceKeyFinder = face_data
                .ToKeyFinder(face_cache, face_index)
                .With<Key2NearestShift, Clamp>();



            // �v���C�A�u���̃O���t���\�z���čĐ�����

            var graph = PlayableGraph.Create(name + " anim");


            var job = this.anim.create(bone, posKeyFinder, rotKeyFinder, timer);

            graph.CreateVmdAnimationJobWithSyncScript(this.anim, job, delay: 1);

            graph.CreateVmdFaceAnimation(anim, faceKeyFinder, face, timer, delay: 1);


            graph.Play();


            var cts = this.destroyCancellationToken.Register(() => graph.Destroy());

            await Awaitable.WaitForSecondsAsync(float.PositiveInfinity, cts.Token);

        }

    }
}