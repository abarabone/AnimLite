namespace AnimLite
{
    public interface ITransformMappings<TTf>
        where TTf : ITransformProxy
    {
        (HumanBoneReference<TTf> human, BoneRotationInitialPose initpose, OptionalBoneChecker option) this[int i] { get; }

        int BoneLength { get; }
    }


}
