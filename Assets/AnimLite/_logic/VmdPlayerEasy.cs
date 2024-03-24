using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;


    public class VmdPlayerEasy : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // 
            var vmd = await VmdData.BuildVmdStreamDataAsync(this.VmdFilePath.ToFullPath(), this.FaceMappingFilePath.ToFullPath(), this.destroyCancellationToken);
            using var vmddata = vmd.data;

            // �u�l�c���Đ��̂��߂̏����\�z����
            var bone = this.anim.BuildVmdTransformMappings();
            var face = this.anim.FindFaceRendererIfNothing(this.faceRenderer)?.sharedMesh?.BuildStreamingFace(vmd.facemap) ?? default;
            var bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(bone);
            var footOperator = this.anim.ToFootIkTransformOperator(bone);
            var faceOperator = this.anim.ToVrmExpressionOperator(face);


            // ���Ԕ͈͂Ȃǂ̏����������^�C�}�[���쐬����
            var timer = new StreamingTimer(vmd.data.RotationStreams.Streams.GetLastKeyTime());


            var tfAnim = this.anim.transform;

            for (; ; )
            {
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);


                // �^�C�}�[��i�߂�
                timer.ProceedTime(Time.deltaTime);


                // �L�[�����I�u�W�F�N�g���\�z����
                // �W�F�l���N�X�ɂ��u�L�[��ԕ����A���Ԃ̃N���b�v���@�A�������@�v���w��ł���

                var rotKeyFinder = vmd.data.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp, Forward>(timer);

                var posKeyFinder = vmd.data.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp, Forward>(timer);

                var faceKeyFinder = vmd.data.FaceStreams
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