//using static Unity.VisualScripting.AnnotationUtility;

namespace AnimLite
{


    public struct HumanBoneName
    {
        public string name;

        static public implicit operator HumanBoneName(string name) => name.AsHumanBoneName();
    }


    public static class HumanoidUtilityExtension
    {

        public static HumanBoneName AsHumanBoneName(this string name) => new HumanBoneName { name = name };

    }

}

