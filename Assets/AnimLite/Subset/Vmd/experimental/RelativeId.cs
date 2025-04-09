using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite.Vmd.experimental
{
    using AnimLite.Utility;
    

    // モデルを個別に削除したり追加したりできるように使用と思い、swap back でＩＤを再配布しないで済むようにワンクッションおく目的でつくったが、使ってない
    // （ pos, rot が大量になるので、ひとつひとつに swap back するのも面倒になったので、モデル個別の削除や追加はやめた）

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
        // 使用中エリア：  実id を格納
        // 未使用エリア：  空きid（空き使用中エリアの id ）を格納
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

        // 返した時、スキマが空く
        // スキマは次回取得されるときに使われる
        // 次回取得されるとき、未使用エリアにある id は使用中エリアの空きを指している
        // 未使用エリアから使用した場合、未使用エリアの最後から使用中エリアの 空きid を持ってきて埋める（最後の 空きid 領域は１つ消される）
        public static void Release(RelativeId relativeId)
        {
            idList.Add(relativeId.iRel);
            //idList[relativeId.iRel] = -1;
        }

    }

}
