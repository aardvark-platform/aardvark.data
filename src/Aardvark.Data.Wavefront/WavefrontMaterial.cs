using Aardvark.Base;

namespace Aardvark.Data.Wavefront
{
    public class WavefrontMaterial : SymMapBase
    {
        public static class Property
        {
            /// <summary>
            /// Ka
            /// </summary>
            public static readonly Symbol AmbientColor = "AmbientColor";

            /// <summary>
            /// Ke
            /// </summary>
            public static readonly Symbol EmissiveColor = "EmissiveColor";

            /// <summary>
            /// Kd
            /// </summary>
            public static readonly Symbol DiffuseColor = "DiffuseColor";

            /// <summary>
            /// Ks
            /// </summary>
            public static readonly Symbol SpecularColor = "SpecularColor";

            /// <summary>
            /// Ns
            /// </summary>
            public static readonly Symbol SpecularExponent = "SpecularExponent";

            /// <summary>
            /// 1-Tr or d (last one will overwrite if both are present)
            /// </summary>
            public static readonly Symbol Opacity = "Opacity";

            /// <summary>
            /// Tf
            /// </summary>
            public static readonly Symbol TransmissionFilter = "TransmissionFilter";

            /// <summary>
            /// illum
            /// </summary>
            public static readonly Symbol IlluminationModel = "IlluminationModel";

            /// <summary>
            /// sharpness
            /// </summary>
            public static readonly Symbol Sharpness = "Sharpness";

            /// <summary>
            /// Ni
            /// </summary>
            public static readonly Symbol OpticalDensity = "OpticalDensity";

            /// <summary>
            /// map_Ka
            /// </summary>
            public static readonly Symbol AmbientColorMap = "AmbientColorMap";

            /// <summary>
            /// map_Kd
            /// </summary>
            public static readonly Symbol DiffuseColorMap = "DiffuseColorMap";

            /// <summary>
            /// map_Ks
            /// </summary>
            public static readonly Symbol SpecularColorMap = "SpecularColorMap";

            /// <summary>
            /// map_Ke
            /// </summary>
            public static readonly Symbol EmissiveColorMap = "EmissiveColorMap";

            /// <summary>
            /// map_Ns
            /// </summary>
            public static readonly Symbol SpecularExponentMap = "SpecularExponentMap";

            /// <summary>
            /// map_d
            /// </summary>
            public static readonly Symbol OpacityMap = "OpacityMap";

            /// <summary>
            /// map_decal
            /// </summary>
            public static readonly Symbol DecalMap = "DecalMap";

            /// <summary>
            /// disp
            /// </summary>
            public static readonly Symbol DisplacementMap = "DisplacementMap";

            /// <summary>
            /// map_bump or bump (last one will overwrite if both are present)
            /// </summary>
            public static readonly Symbol BumpMap = "BumpMap";

            /// <summary>
            /// map_Kn
            /// </summary>
            public static readonly Symbol NormalMap = "NormalMap";

            /// <summary>
            /// NOT USED
            /// </summary>
            public static readonly Symbol ReflectionMap = "ReflectionMap";

            /// <summary>
            /// Base directory of texture maps
            /// </summary>
            public static readonly Symbol Path = "Path";
        }

        public string Name;

        public WavefrontMaterial(string name)
        {
            Name = name;
        }
    }
}
