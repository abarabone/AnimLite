using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite.Vmd.experimental
{
    using AnimLite.Utility;
    

    // ���f�����ʂɍ폜������ǉ�������ł���悤�Ɏg�p�Ǝv���Aswap back �łh�c���Ĕz�z���Ȃ��ōςނ悤�Ƀ����N�b�V���������ړI�ł��������A�g���ĂȂ�
    // �i pos, rot ����ʂɂȂ�̂ŁA�ЂƂЂƂ� swap back ����̂��ʓ|�ɂȂ����̂ŁA���f���ʂ̍폜��ǉ��͂�߂��j

    public struct RelativeId
    {
        public int iRel;

        public int iAbs => RelativeIdManager.GetAbsoluteId(this);


        public static RelativeId New(int absoluteId)
        {
            var newid = RelativeIdManager.New(absoluteId);
            RelativeIdManager.SetAbsoluteId(newid, absoluteId);
            return newid;
        }

        public RelativeId Relese()
        {
            RelativeIdManager.Release(this);
            this.iRel = -1;
            return this;
        }

        public void ReAbs(int newAbsoluteId) => RelativeIdManager.SetAbsoluteId(this, newAbsoluteId);


        public static RelativeId DefaultInstance => new RelativeId
        {
            iRel = -1,
        };
    }


    public static class RelativeIdManager
    {
        // �g�p���G���A�F  ��id ���i�[
        // ���g�p�G���A�F  ��id�i�󂫎g�p���G���A�� id �j���i�[
        static List<int> idList;

        static int usedLength;




        public static void SetAbsoluteId(RelativeId relativeId, int newAbsoluteId) =>
            idList[relativeId.iRel] = newAbsoluteId;

        public static int GetAbsoluteId(RelativeId relativeId) =>
            idList[relativeId.iRel];



        public static RelativeId New(int absoluteId)
        {
            return usedLength == idList.Count
                ? getNew_()
                : getFromUnused_();

            RelativeId getNew_()
            {
                idList.Add(absoluteId);
                return new RelativeId
                {
                    iRel = usedLength++,
                };
            }

            RelativeId getFromUnused_()
            {
                var ilast = idList.Count - 1;
                var id_unused = idList[ilast];
                idList.RemoveAt(ilast);

                return new RelativeId
                {
                    iRel = idList[id_unused],
                };
            }
        }

        // �Ԃ������A�X�L�}����
        // �X�L�}�͎���擾�����Ƃ��Ɏg����
        // ����擾�����Ƃ��A���g�p�G���A�ɂ��� id �͎g�p���G���A�̋󂫂��w���Ă���
        // ���g�p�G���A����g�p�����ꍇ�A���g�p�G���A�̍Ōォ��g�p���G���A�� ��id �������Ă��Ė��߂�i�Ō�� ��id �̈�͂P�������j
        public static void Release(RelativeId relativeId)
        {
            idList.Add(relativeId.iRel);
            //idList[relativeId.iRel] = -1;
        }

    }

}
