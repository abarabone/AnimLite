using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;


    public class VmdPlayerEasyWithCache : MonoBehaviour
    {
        public VmdStreamDataCache Cache;

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // �u�l�c�f�[�^���t�@�C������p�[�X���A�X�g���[���f�[�^���r���h����
            var vmdpath = this.VmdFilePath;
            var facemappath = this.FaceMappingFilePath;
            var (vmddata, facemap) = await this.Cache.GetOrLoadVmdStreamDataAsync(vmdpath, facemappath, this.destroyCancellationToken);
            using var _ = vmddata;
            
            // �u�l�c���Đ��̂��߂̏����\�z����
            var bone = this.anim.BuildVmdTransformMappings();
            var face = facemap.BuildStreamingFace();
            //var face = this.anim.FindFaceRendererIfNothing(this.faceRenderer)?.sharedMesh?.BuildStreamingFace(facemap) ?? default;
            var bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(bone);
            var footOperator = this.anim.ToFootIkTransformOperator(bone);
            var faceOperator = this.anim.ToVrmExpressionOperator(face);

            // ���Ԕ͈͂Ȃǂ̏����������^�C�}�[���쐬����
            var timer = new StreamingTimer(vmddata.RotationStreams.Streams.GetLastKeyTime());


            var tfAnim = this.anim.transform;

            for (; ; )
            {
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);


                // �^�C�}�[��i�߂�
                timer.ProceedTime(Time.deltaTime);


                // �L�[�����I�u�W�F�N�g���\�z����
                // �W�F�l���N�X�ɂ��u�L�[��ԕ����A���Ԃ̃N���b�v���@�A�������@�v���w��ł���

                var rotKeyFinder = vmddata.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp, Forward>(timer);

                var posKeyFinder = vmddata.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp, Forward>(timer);

                var faceKeyFinder = vmddata.FaceStreams
                    .ToKeyFinderWith<Key2NearestShift, Clamp, Forward>(timer);


                // �u�l�c���Đ�����i�L�[���������A�v�Z���� Transform �ɏ����o���j
                bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
                footOperator.SolveLegPositionIk(posKeyFinder, tfAnim.position, tfAnim.rotation);
                footOperator.SolveFootRotationIk(rotKeyFinder, tfAnim.position, tfAnim.rotation);
                faceOperator.SetFaceExpressions(faceKeyFinder);
            }
        }

    }
}