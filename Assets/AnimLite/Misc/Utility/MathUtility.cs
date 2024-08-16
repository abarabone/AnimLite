using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Utility
{
    //public class ProgressState : IDisposable
    //{
    //    TaskCompletionSource<bool> tcs;

    //    public bool IsInProgress => !this.tcs?.Task.IsCompleted ?? false;


    //    public ProgressState Start()
    //    {
    //        //Debug.Log("start");
    //        this.tcs = new TaskCompletionSource<bool>();

    //        return this;
    //    }

    //    public async Awaitable WaitForCompleteAsync()
    //    {
    //        //Debug.Log("wait on");
    //        await this.tcs?.Task;
    //        Debug.Log("wait off");
    //    }

    //    public void Dispose()
    //    {
    //        //Debug.Log("end");
    //        this.tcs.SetResult(true);
    //    }
    //}



    public static class MathUtilityExtenstion
    {

        public static bool IsZero(this Vector3 v) => (v.x * v.x + v.y * v.y + v.z * v.z) == 0.0f;


        //public static float3 As3(this float4 v) => (float3)v;
        public static float3 As3(this float4 v) => new float3(v.x, v.y, v.z);

        public static float3 AsXZ(this float3 v) => new float3(v.x, 0, v.z);

        public static float3 As_float3(this float4 v) => new float3(v.x, v.y, v.z);

        public static quaternion As_quaternion(this float4 v) => v;

        public static float3 As_float3(this Vector3 v) => (float3)v;
        public static Vector3 AsVector3(this float3 v) => (Vector3)v;
        public static quaternion As_quaternion(this Quaternion q) => (quaternion)q;
        public static Quaternion AsQuaternion(this quaternion q) => (Quaternion)q;

    }
}
