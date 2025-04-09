using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Jobs;

using AnimLite.Vmd.experimental;
using AnimLite.Loader;
using AnimLite.Utility;
using AnimLite.Vmd;
using AnimLite.Vrm;
using AnimLite;
using AnimLite.Utility.Linq;

using System.Security.Claims;
using Unity.Mathematics;
using System;

public class VmdJobPlayer : MonoBehaviour
{

    [FilePath]
    public PathUnit VmdFilePath;

    public Animator[] anims;

    // ���̃X�N���v�g�ł́A�t�F�C�V�����͑ΏۊO�Ƃ���
    //public SkinnedMeshRenderer faceRenderer;

    public VmdFootIkMode ikmode;

    public PrototypeCacheManager Cache;


    PlayableGraph graph;


    async Awaitable Start()
    {
        try
        {

            await using var vmdbag = new AsyncDisposableBag { };

            // ���f���P�̂����s���ă��[�h����
            var q = await Task.WhenAll(this.anims.Select(async anim =>
            {
                // �u�l�c�f�[�^���t�@�C������p�[�X���A�X�g���[���f�[�^���r���h����
                var vmdpath = this.VmdFilePath;
                var vmd = await this.Cache.Holder.VmdCache.GetOrLoadVmdAsync(vmdpath, "", null, this.destroyCancellationToken);
                vmdbag.Add(vmd);

                // �L�[�����I�u�W�F�N�g���\�z����
                // �W�F�l���N�X�ɂ��u�L�[��ԕ����A���Ԃ̃N���b�v���@�A�������@�v���w��ł���

                var rotKeyFinder = vmd.Value.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                var posKeyFinder = vmd.Value.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                // �u�l�c���Đ����邽�߂̏����\�z����
                var bodyAdjust = await BodyAdjustLoader.DefaultBodyAdjustAsync;
                var bones = anim.BuildVmdTransformMappings(bodyAdjust);

                // ���f�����Ƃ̃W���u�p�����[�^���\�z����B����͑��h�j�̐ݒ肾���w�肵�A�̂̐ݒ�͎��������ɔC����
                var footop = anim.ToVmdFootIkTransformOperator(bones).WithIkUsage(vmd, this.ikmode);
                var param = anim.BuildJobParams(bones, posKeyFinder, rotKeyFinder, footop);
                return param;
            }));


            // �W���u�p�̃o�b�t�@���\�z����B�A�j���[�V�����̂��߂̃W���u�́A�������f�����܂Ƃ߂ď�������
            var countlist = q.CountParams();
            using var buf = q.BuildJobBuffers(countlist);


            this.graph = PlayableGraph.Create();

            this.graph.Play();


            for (; ; )
            {
                // �W���u�̃X�P�W���[���́AUpdate() �̓x�ɕK�v
                using var dep = buf.BuildMotionJobsAndSchedule(Time.deltaTime)
                    .AsDisposable(dep => dep.Complete());

                // animator ���W���u�Ɉˑ�������
                q.ForEach((p, i) =>
                {
                    p.model_data.anim.AddJobDependency(dep);
                });

                await Awaitable.NextFrameAsync(this.destroyCancellationToken);
            }
        }
        catch (OperationCanceledException e)
        {
            Debug.LogWarning(e);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        graph.Destroy();
    }
}
