using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using System;
using System.Threading;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.Utility;

    using AnimLite.Bvh;
    using AnimLite.Vmd;


    public class BvhPlayer : MonoBehaviour
    {

        [FilePath]
        public PathUnit BvhFilePath;


        public Animator anim;

        public SkinnedMeshRenderer FaceMeshRenderer;


        TransformMappings bone;


        DisposableBag disposabes;

        Key4StreamCache<quaternion> rot_cache;
        Key4StreamCache<float4> pos_cache;

        StreamIndex rot_index;
        StreamIndex pos_index;

        StreamData<quaternion> rot_data;
        StreamData<float4> pos_data;



        StreamingTimer timer;


        BodyMotionOperator<TransformMappings, Tf> bodyOperator;



        private void OnDisable()
        {
            if (this.disposabes == null) return;
            this.disposabes.Dispose();
            this.disposabes = null;

            if (this.anim.IsUnityNull()) return;
            this.anim.UnbindAllStreamHandles();
            this.anim.ResetPose();
        }



        async Awaitable OnEnable()
        {
            try
            {
                // �t�@�C������f�[�^��ǂ݉���
                var bvh = this.BvhFilePath.ParseBvh();
                var vmdStreamData = Bvh.BvhParser.BvhToVmdMotionData(bvh);

                // �f�[�^�𗘗p�ł���`���ɕϊ�����
                this.rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData2(BvhParser.LogicalToPhysicalBones);
                this.pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData2();

                // �f�[�^�A�N�Z�X�����������邽�߂̍������쐬����
                this.rot_index = rot_data.CreateIndex(indexBlockLength: 100);
                this.pos_index = pos_data.CreateIndex(indexBlockLength: 100);

                // Forward �ŗ��p����L�[�L���b�V���o�b�t�@�𐶐�����
                this.rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
                this.pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);

                // �j���p�ɂ܂Ƃ߂Ă���
                this.disposabes = new DisposableBag
                {
                    this.rot_data.ToHolderWith(this.rot_cache, this.rot_index),
                    this.pos_data.ToHolderWith(this.pos_cache, this.pos_index),
                };

                // ���Ԕ͈͂Ȃǂ̏����������^�C�}�[���쐬����
                this.timer = new StreamingTimer(rot_data.GetLastKeyTime());

                // �q���[�}�m�C�h���f���̏����\�z����
                this.bone = this.anim.BuildTransformMappings();

                //// �u�l�c���Đ��̂��߂̏����\�z����
                this.bodyOperator = this.anim.ToBodyTransformMotionOperator(this.bone);
                //this.footOperator = this.anim.ToFootIkTransformOperator(this.bone);
                //this.faceOperator = this.anim.ToVrmExpressionOperator(this.face);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            if (!this.enabled) this.OnDisable();
        }


        void Update()
        {
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


            // �u�l�c���Đ�����i�L�[���������A�v�Z���� Transform �ɏ����o���j
            var tfAnim = this.anim.transform;
            this.bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
        }

    }
}