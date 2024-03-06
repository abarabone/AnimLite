using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.IK;
    using AnimLite.Utility;


    public class VmdPlayer : MonoBehaviour
    {

        [FilePath]
        public string VmdFilePath;

        [FilePath]
        public string FaceMappingFilePath;

        public Animator anim;

        public SkinnedMeshRenderer FaceMeshRenderer;


        TransformStreamingBone bone;
        StreamingFace face;


        DisposableBag disposabes;

        Key4StreamCache<quaternion> rot_cache;
        Key4StreamCache<float4> pos_cache;
        Key2StreamCache<float> face_cache;

        StreamIndex rot_index;
        StreamIndex pos_index;
        StreamIndex face_index;

        StreamData<quaternion> rot_data;
        StreamData<float4> pos_data;
        StreamData<float> face_data;



        StreamingTimer timer;


        VmdBodyMotionOperator<TransformStreamingBone, Tf> bodyOperator;
        FootIkOperator<Tf> footOperator;
        VmdFaceOperator faceOperator;



        private void OnDestroy()
        {
            if (this.disposabes == null) return;

            this.disposabes.Dispose();
        }



        async Awaitable OnEnable()
        {
            //this.anim.ResetPose();//

            // �t�@�C������f�[�^��ǂ݉���
            var vmdStreamData = await VmdParser.ParseVmdAsync(this.VmdFilePath, this.destroyCancellationToken);
            var faceMapping = await VmdParser.ParseFaceMapAsync(this.FaceMappingFilePath, this.destroyCancellationToken);

            // �f�[�^�𗘗p�ł���`���ɕϊ�����
            this.rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData();
            this.pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData();
            this.face_data = vmdStreamData.faceKeyStreams.CreateFaceData(faceMapping);

            // �f�[�^�A�N�Z�X�����������邽�߂̍������쐬����
            this.rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            this.pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            this.face_index = face_data.CreateIndex(indexBlockLength: 100);

            // Forward �ŗ��p����L�[�L���b�V���o�b�t�@�𐶐�����
            this.rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            this.pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            this.face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);

            // �j���p�ɂ܂Ƃ߂Ă���
            this.disposabes = new DisposableBag
            {
                this.rot_data.ToHolderWith(this.rot_cache, this.rot_index),
                this.pos_data.ToHolderWith(this.pos_cache, this.pos_index),
                this.face_data.ToHolderWith(this.face_cache, this.face_index),
            };

            // ���Ԕ͈͂Ȃǂ̏����������^�C�}�[���쐬����
            this.timer = new StreamingTimer(rot_data.GetLastKeyTime());

            // �q���[�}�m�C�h���f���̏����\�z����
            this.bone = this.anim.BuildVmdTransformStreamingBone();
            this.face = this.FaceMeshRenderer.sharedMesh.BuildStreamingFace(faceMapping);

            // �u�l�c���Đ��̂��߂̏����\�z����
            this.bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(this.bone);
            this.footOperator = this.anim.ToFootIkTransformOperator(this.bone);
            this.faceOperator = this.anim.ToVmdFaceOperator(this.face);
        }


        void Update()
        {
            if (!this.rot_data.KeyStreams.Values.IsCreated) return;


            // �^�C�}�[��i�߂�
            this.timer.ProceedTime(Time.deltaTime);


            // �L�[�����I�u�W�F�N�g���\�z����
            // �W�F�l���N�X�ɂ��u�L�[��ԕ����A���Ԃ̃N���b�v���@�A�������@�v���w��ł���

            var posKeyFinder = this.pos_data
                .ToKeyFinder(this.pos_cache, this.pos_index)
                .With<Key4CatmulPos, Clamp, Forward>(this.timer);

            var rotKeyFinder = this.rot_data
                .ToKeyFinder(this.rot_cache, this.rot_index)
                .With<Key4CatmulRot, Clamp, Forward>(this.timer);

            var faceKeyFinder = this.face_data
                .ToKeyFinder(this.face_cache, this.face_index)
                .With<Key2NearestShift, Clamp, Forward>(this.timer);


            // �u�l�c���Đ�����i�L�[���������A�v�Z���� Transform �ɏ����o���j
            var tfAnim = this.anim.transform;
            this.bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
            this.footOperator.SolveLegPositionIk(posKeyFinder, tfAnim.position, tfAnim.rotation);
            this.footOperator.SolveFootRotationIk(rotKeyFinder, tfAnim.position, tfAnim.rotation);
            this.faceOperator.SetFaceExpressions(faceKeyFinder);
        }

    }
}