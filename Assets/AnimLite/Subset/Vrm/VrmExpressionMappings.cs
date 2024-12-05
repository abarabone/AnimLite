namespace AnimLite.Vrm
{
    public struct VrmExpressionMappings// : IDisposable
    {
        public VrmFaceReference[] FaceReferences;
        public UniVRM10.ExpressionKey[] Expressions;
        //public void Dispose() => this.BlendShapeIndexes.Dispose();

        public bool IsCreated => this.FaceReferences != null && this.Expressions != null;
    }

    public struct VrmFaceReference
    {
        public UniVRM10.ExpressionKey expid;
        public int faceIndex;
        public int istream;
    }
    //public static class FaceStreamingExtension
    //{
    //    public static void 
    //}

}