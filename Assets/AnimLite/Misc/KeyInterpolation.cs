using Unity.Mathematics;

namespace AnimLite
{


    ////    // できれば Key と Interpolation を分けたいんだが、指定するジェネリクス型が増えるので妥協、IKeyInterpolation は使ってない
    ////    // c# のジェネリクスはもう少し自由度ほしい

    ////    public struct None<T> : IKeyInterpolation<T>
    ////        where T : unmanaged
    ////    { }



    ////    /// <summary>
    ////    /// ごく普通の２値の線型補間（だと思う）
    ////    /// </summary>
    ////    public struct LeapPosition : IKeyInterpolation<float4>
    ////    {
    ////        public float4 Interpolate(float4 v0, float4 v1, float4 v2, float4 v3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            return math.lerp(v1, v2, t);
    ////        }
    ////    }
    ////    public struct LeapRotation : IKeyInterpolation<quaternion>
    ////    {
    ////        public quaternion AdjustNext(quaternion p0, quaternion p1) => Interpolation.adjust_quaterion(p0, p1);

    ////        public quaternion Interpolate(quaternion q0, quaternion q1, quaternion q2, quaternion q3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            return math.lerp(q1.value, q2.value, t);
    ////        }
    ////    }

    ////    public struct DualLeapPosition : IKeyInterpolation<float4>
    ////    {
    ////        public float4 Interpolate(float4 v0, float4 v1, float4 v2, float4 v3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            var _0 = math.lerp(v0, v1, t);
    ////            var _1 = math.lerp(v2, v3, t);
    ////            return math.lerp(_0, _1, t);
    ////        }
    ////    }
    ////    public struct DualLeapRotation : IKeyInterpolation<quaternion>
    ////    {
    ////        public quaternion AdjustNext(quaternion p0, quaternion p1) => Interpolation.adjust_quaterion(p0, p1);

    ////        public quaternion Interpolate(quaternion q0, quaternion q1, quaternion q2, quaternion q3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            var _0 = math.lerp(q0.value, q1.value, t);
    ////            var _1 = math.lerp(q2.value, q3.value, t);
    ////            return math.lerp(_0, _1, t);
    ////        }
    ////    }


    ////    public struct CatmullRomPosition : IKeyInterpolation<float4>
    ////    {
    ////        public float4 Interpolate(float4 v0, float4 v1, float4 v2, float4 v3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            return Interpolation.CatmullRom(v0, v1, v2, v3, t);
    ////        }
    ////    }
    ////    public struct CatmullRomRotation : IKeyInterpolation<quaternion>
    ////    {
    ////        public quaternion AdjustNext(quaternion p0, quaternion p1) => Interpolation.adjust_quaterion(p0, p1);

    ////        public quaternion Interpolate(quaternion q0, quaternion q1, quaternion q2, quaternion q3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            var t = (time - time_from) * section_ratio;

    ////            var v0 = q0.value;
    ////            var v1 = q1.value;
    ////            var v2 = q2.value;
    ////            var v3 = q3.value;
    ////            return Interpolation.CatmullRom(v0, v1, v2, v3, t);
    ////        }
    ////    }



    ////    public struct NearestShift : IKeyInterpolation<float>
    ////    {
    ////        public float Interpolate(
    ////            float v0, float v1, float v2, float v3, float time, float time_from, float time_to, float section_ratio)
    ////        {
    ////            const float limit = 0.3f;
    ////            const float ratio = 1.0f / limit;

    ////            //var t = (time - time_from) * section_ratio;
    ////            var t = math.min(time_to - time, limit) * ratio;

    ////            return math.lerp(v1, v2, t * t);
    ////        }
    ////    }


    ////    //public struct XIp<T> : IKeyInterpolation<T>
    ////    //    where T : unmanaged
    ////    //{

    ////    //    public Type type;


    ////    //    public float Interpolate(
    ////    //        float v0, float v1, float v2, float v3, float time, float time_from, float time_to, float section_ratio)
    ////    //    {
    ////    //        return this.type switch
    ////    //        {
    ////    //            LeapPosition => ,
    ////    //            _ => default,
    ////    //        };
    ////    //    }
    ////    //}




    public static class Interpolation
    {

        public static quaternion adjust_quaterion(quaternion q0, quaternion q1)
        {
            return math.select(-q1.value, q1.value, math.dot(q0, q1) >= 0);
            //return math.select(math.inverse(q1).value, q1v.value, math.dot(q0, q1) > 0);

            //quaternion reverse_(quaternion q) => new quaternion(q.value.x, q.value.y, q.value.z, q.value.w);
        }



        //public static T Interpolate<T, TIp>(this TIp ip, Key4Cursor cursor, Key4Value<T> values, float time)
        //    where T : unmanaged
        //    where TIp : IKeyInterpolation<T>
        //{
        //    //var t = (time - src.cursor.TimeFrom) * src.cursor.FromToTimeRate;
        //    //t = math.clamp(t, 0, 1);
        //    //return math.slerp(src.values.From, src.values.To, t);
        //    return ip.Interpolate(
        //        values.Prev, values.From, values.To, values.Next,
        //        time, cursor.TimeFrom, cursor.TimeTo, cursor.FromToTimeRate);
        //}

        //public static T Interpolate<T, TIp>(this TIp ip, Key2Cursor cursor, Key2Value<T> values, float time)
        //    where T : unmanaged
        //    where TIp : IKeyInterpolation<T>
        //{
        //    //var t = (time - src.cursor.TimeFrom) * src.cursor.FromToTimeRate;
        //    //t = math.clamp(t, 0, 1);
        //    //return math.slerp(src.values.From, src.values.To, t);
        //    return ip.Interpolate(
        //        values.From, values.From, values.To, values.To,
        //        time, cursor.TimeFrom, cursor.TimeTo, cursor.FromToTimeRate);
        //}

        public static float NearestShift(float v1, float v2, float time, float time_from, float time_to, float section_ratio)
        {
            const float limit = 0.3f;
            const float ratio = 1.0f / limit;

            //var _t = (time - time_from) * section_ratio;
            var _t = math.min(time_to - time, limit) * ratio;
            var t = _t;// math.clamp(_t, 0, 1);

            return math.lerp(v2, v1, t * t);
        }

        public static float4 DualLerp(float4 v0, float4 v1, float4 v2, float4 v3, float t)
        {
            var _0 = math.lerp(v0, v1, t);
            var _1 = math.lerp(v2, v3, t);
            return math.lerp(_0, _1, t);
        }



        /// <summary>
        /// 
        /// </summary>
        public static float4 CatmullRom(float4 v0, float4 v1, float4 v2, float4 v3, float t)
        {
            //if (t > 1) Debug.Log(t);
            //return Quaternion.Slerp( new quaternion(v1), new quaternion(v2), Mathf.Clamp01(t) ).ToFloat4();
            return v1 + 0.5f * t * ((v2 - v0) + ((2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3) + (-v0 + 3.0f * v1 - 3.0f * v2 + v3) * t) * t);
        }

        /// <summary>
        /// 
        /// </summary>
        public static float CatmullRom(float v0, float v1, float v2, float v3, float t)
        {
            //if (t > 1) Debug.Log(t);
            return v1 + 0.5f * t * ((v2 - v0) + ((2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3) + (-v0 + 3.0f * v1 - 3.0f * v2 + v3) * t) * t);
        }

    }

}
