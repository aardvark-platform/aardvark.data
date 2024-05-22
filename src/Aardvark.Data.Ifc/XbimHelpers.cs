using Aardvark.Base;
using Xbim.Common.Geometry;

namespace Aardvark.Data.Ifc
{
    public static class XbimHelpers
    {
        public static M44d ToM44d(this XbimMatrix3D m)
        {
            return new M44d(m.M11, m.M21, m.M31, m.OffsetX,
                            m.M12, m.M22, m.M32, m.OffsetY,
                            m.M13, m.M23, m.M33, m.OffsetZ,
                            m.M14, m.M24, m.M34, m.M44);
        }

        public static Trafo3d ToTrafo3d(this XbimMatrix3D m)
        {
            var mat = m.ToM44d();
            var inf = mat.Inverse;
            return new Trafo3d(mat, mat.Inverse);
        }
    }
}
