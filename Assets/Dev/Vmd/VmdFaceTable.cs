using System;
using System.Collections.Generic;

namespace AnimLite.Vmd
{


    public static class VmdFace
    {

        public static Dictionary<VrmFaceName, UniVRM10.ExpressionKey> FaceNameToId = new()
        {
            {"A", UniVRM10.ExpressionKey.Aa},
            {"I", UniVRM10.ExpressionKey.Ih},
            {"U", UniVRM10.ExpressionKey.Ou},
            {"E", UniVRM10.ExpressionKey.Ee},
            {"O", UniVRM10.ExpressionKey.Oh},
            {"a", UniVRM10.ExpressionKey.Aa},
            {"i", UniVRM10.ExpressionKey.Ih},
            {"u", UniVRM10.ExpressionKey.Ou},
            {"e", UniVRM10.ExpressionKey.Ee},
            {"o", UniVRM10.ExpressionKey.Oh},

            {"Aa", UniVRM10.ExpressionKey.Aa},
            {"Ih", UniVRM10.ExpressionKey.Ih},
            {"Ou", UniVRM10.ExpressionKey.Ou},
            {"Ee", UniVRM10.ExpressionKey.Ee},
            {"Oh", UniVRM10.ExpressionKey.Oh},
            {"aa", UniVRM10.ExpressionKey.Aa},
            {"ih", UniVRM10.ExpressionKey.Ih},
            {"ou", UniVRM10.ExpressionKey.Ou},
            {"ee", UniVRM10.ExpressionKey.Ee},
            {"oh", UniVRM10.ExpressionKey.Oh},


            {"joy", UniVRM10.ExpressionKey.Happy},
            {"angry", UniVRM10.ExpressionKey.Angry},
            {"fun", UniVRM10.ExpressionKey.Relaxed},
            {"sorrow", UniVRM10.ExpressionKey.Sad},
            {"neutral", UniVRM10.ExpressionKey.Neutral},

            {"Joy", UniVRM10.ExpressionKey.Happy},
            {"Angry", UniVRM10.ExpressionKey.Angry},
            {"Fun", UniVRM10.ExpressionKey.Relaxed},
            {"Sorrow", UniVRM10.ExpressionKey.Sad},
            {"Neutral", UniVRM10.ExpressionKey.Neutral},


            {"happy", UniVRM10.ExpressionKey.Happy},
            {"relaxed", UniVRM10.ExpressionKey.Relaxed},
            {"sad", UniVRM10.ExpressionKey.Sad},
            {"suprised", UniVRM10.ExpressionKey.Surprised},

            {"Happy", UniVRM10.ExpressionKey.Happy},
            {"Relaxed", UniVRM10.ExpressionKey.Relaxed},
            {"Sad", UniVRM10.ExpressionKey.Sad},
            {"Suprised", UniVRM10.ExpressionKey.Surprised},


            {"wink_L", UniVRM10.ExpressionKey.BlinkLeft},
            {"wink_R", UniVRM10.ExpressionKey.BlinkRight},
            {"blink", UniVRM10.ExpressionKey.Blink},

            {"Wink_L", UniVRM10.ExpressionKey.BlinkLeft},
            {"Wink_R", UniVRM10.ExpressionKey.BlinkRight},
            {"Blink", UniVRM10.ExpressionKey.Blink},
        };
    }
}

