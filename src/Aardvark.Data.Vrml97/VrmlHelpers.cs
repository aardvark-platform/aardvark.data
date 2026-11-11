using Aardvark.Base;

namespace Aardvark.Data.Vrml97
{
    /// <summary>
    /// Various helper methods.
    /// </summary>
    public static class VrmlHelpers
    {
        /// <summary>
        /// Build a texture coordinate transformation from the given parameters as specified in TextureTransform
        /// http://gun.teipir.gr/VRML-amgem/spec/part1/nodesRef.html#TextureTransform
        /// </summary>
        public static Trafo2d BuildVrmlTextureTrafo(V2d center, double rotation, V2d scale, V2d translation)
        {
            M33d C = M33d.Translation(center), Ci = M33d.Translation(-center);
            M33d R = M33d.Rotation(rotation), Ri = M33d.Rotation(-rotation);
            M33d S = M33d.Scale(scale), Si = M33d.Scale(1 / scale);
            M33d T = M33d.Translation(translation), Ti = M33d.Translation(-translation);

            return new Trafo2d(
                            Ci * S * R * C * T,
                            Ti * Ci * Ri * Si * C);
        }

        /// <summary>
        /// Extracts texture transform from given node.
        /// </summary>
        public static Trafo2d ExtractVrmlTextureTrafo(this SymMapBase m)
        {
            if (m == null) return Trafo2d.Identity;

            // get trafo parts
            var c = (V2d)m.Get<V2f>(Vrml97Sym.center, V2f.Zero);
            var r = (double)m.Get<float>(Vrml97Sym.rotation, 0.0f);
            var s = (V2d)m.Get<V2f>(Vrml97Sym.scale, new V2f(1, 1));
            var t = (V2d)m.Get<V2f>(Vrml97Sym.translation, V2f.Zero);

            M33d C = M33d.Translation(c), Ci = M33d.Translation(-c);
            M33d R = M33d.Rotation(r), Ri = M33d.Rotation(-r);
            M33d S = M33d.Scale(s), Si = M33d.Scale(1 / s);
            M33d T = M33d.Translation(t), Ti = M33d.Translation(-t);

            return new Trafo2d(
                            Ci * S * R * C * T,
                            Ti * Ci * Ri * Si * C);
        }

        /// <summary>
        /// Build a geometry transformation from the given parameters as specified in Transform
        /// http://gun.teipir.gr/VRML-amgem/spec/part1/nodesRef.html#Transform
        /// </summary>
        public static Trafo3d BuildVrmlGeometryTrafo(V3d center, V4d rotation, V3d scale, V4d scaleOrientation, V3d translation)
        {
            // create composite trafo (naming taken from vrml97 spec)
            M44d C = M44d.Translation(center), Ci = M44d.Translation(-center);
            var scaleRotAxis = scaleOrientation.XYZ.Normalized; // NOTE: values in the vrml (limited number of digits) are often not normalized
            M44d SR = M44d.Rotation(scaleRotAxis, scaleOrientation.W), SRi = M44d.Rotation(scaleRotAxis, -scaleOrientation.W);
            M44d T = M44d.Translation(translation), Ti = M44d.Translation(-translation);

            //if (m_aveCompatibilityMode) r.W = -r.W;
            var rotationAxis = rotation.XYZ.Normalized; // NOTE: values in the vrml (limited number of digits) are often not normalized
            M44d R = M44d.Rotation(rotationAxis, rotation.W), Ri = M44d.Rotation(rotationAxis, -rotation.W);

            // in case some axis scales by 0 the best thing for the inverse scale is also 0
            var si = new V3d(scale.X.IsTiny() ? 0 : 1 / scale.X,
                             scale.Y.IsTiny() ? 0 : 1 / scale.Y,
                             scale.Z.IsTiny() ? 0 : 1 / scale.Z);
            M44d S = M44d.Scale(scale), Si = M44d.Scale(si);

            return new Trafo3d(
                            T * C * R * SR * S * SRi * Ci,
                            C * SR * Si * SRi * Ri * Ci * Ti);
        }

        /// <summary>
        /// Returns geometry transform from given node.
        /// </summary>
        public static Trafo3d ExtractVrmlGeometryTrafo(this SymMapBase m)
        {
            // get trafo parts
            var c = (V3d)m.Get<V3f>(Vrml97Sym.center, V3f.Zero);

            var r = (V4d)m.Get<V4f>(Vrml97Sym.rotation, V4f.Zero);
            if (r.X == 0 && r.Y == 0 && r.Z == 0) r.Z = 1;

            var s = (V3d)m.Get<V3f>(Vrml97Sym.scale, new V3f(1, 1, 1));

            var sr = (V4d)m.Get<V4f>(Vrml97Sym.scaleOrientation, V4f.Zero);
            if (sr.X == 0 && sr.Y == 0 && sr.Z == 0) sr.Z = 1;

            var t = (V3d)m.Get<V3f>(Vrml97Sym.translation, V3f.Zero);

            return BuildVrmlGeometryTrafo(c, r, s, sr, t);
        }
    }
}
