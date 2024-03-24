using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;
    using AnimLite.Utility;

    using AnimLite.Vmd;
    using AnimLite.Vrm;


    public class VmdPlayer : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;

        public SkinnedMeshRenderer FaceMeshRenderer;


        TransformMappings bone;
        VrmExpressionMappings face;


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


        VmdBodyMotionOperator<TransformMappings, Tf> bodyOperator;
        FootIkOperator<Tf> footOperator;
        VrmExpressionOperator faceOperator;



        private void OnDisable()
        {
            this.disposabes?.Dispose();
            this.disposabes = null;

            if (this.anim.IsUnityNull()) return;
            this.anim.ResetPose();
        }



        async Awaitable OnEnable()
        {
            //this.anim.ResetPose();//

            // �t�@�C������f�[�^��ǂ݉���
            var vmdStreamData = await VmdParser.ParseVmdAsync(this.VmdFilePath.ToFullPath(), this.destroyCancellationToken);
            var faceMapping = await VrmParser.ParseFaceMapAsync(this.FaceMappingFilePath.ToFullPath(), this.destroyCancellationToken);

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
            this.bone = this.anim.BuildVmdTransformMappings();
            this.face = this.FaceMeshRenderer.sharedMesh.BuildStreamingFace(faceMapping);

            // �u�l�c���Đ��̂��߂̏����\�z����
            this.bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(this.bone);
            this.footOperator = this.anim.ToFootIkTransformOperator(this.bone);
            this.faceOperator = this.anim.ToVrmExpressionOperator(this.face);
        }


        void Update()
        {
            //if (!this.rot_data.KeyStreams.Values.IsCreated) return;
            if (this.disposabes == null) return;


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