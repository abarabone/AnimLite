namespace AnimLite.Vrm
{
    public struct VrmExpressionName
    {
        public string name;

        static public implicit operator VrmExpressionName(string name) => name.AsVrmExpressionName();
    }


    public static class VrmUtilityExtension
    {
        public static VrmExpressionName AsVrmExpressionName(this string name) => new VrmExpressionName { name = name };
    }
}