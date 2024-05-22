using Aardvark.Base;
using Aardvark.Geometry;
using System.Collections.Generic;

namespace Aardvark.Data.Collada
{
    #region library stuff

    public class ColladaMaterial
    {
        public C4f Emission;
        public C4f Ambient;
        public C4f Diffuse;
        public C4f Specular;
        public double Alpha;
        public double Shininess;
        public string Name;

        public string DiffuseColorTexturePath;
        public string SpecularColorTexturePath;
        public string NormalMapPath;

        public ColladaMaterial()
        {
            Emission = new C4f(0, 0, 0, 1);
            Ambient = new C4f(0, 0, 0, 1);
            Diffuse = new C4f(1, 1, 1, 1);
            Specular = new C4f(1, 1, 1, 1);
            Alpha = 1.0;
            Shininess = 32;

            DiffuseColorTexturePath = null;
            SpecularColorTexturePath = null;
            NormalMapPath = null;
        }
    }

    public class ColladaLight
    {
        public C3f Color;
    }

    /// <summary>
    /// A = constant_attenuation + ( Dist * linear_attenuation ) + (( Dist^2 ) * quadratic_attenuation ) 
    /// </summary>
    public class ColladaPointLight : ColladaLight
    {
        // [Constant, Linear, Quadratic]
        public V3d Attenuation = V3d.IOO;
    }

    /// <summary>
    /// light in [0, 0, -1] direction
    /// </summary>
    public class ColladaDirectionalLight : ColladaLight
    {
    }

    public class ColladaSpotLight : ColladaLight
    {
        // [Constant, Linear, Quadratic]
        public V3d Attenuation = V3d.IOO;
        public double FalloffAngle = 180;
        public double FalloffExponent = 0;
    }

    public class ColladaAmbientLight : ColladaLight
    {
    }

    public class ColladaCamera
    {
        public double ZNear;
        public double ZFar;
        //Imager
    }

    public class ColladaCameraPerspective : ColladaCamera
    {
        public double HFov;
        public double VFov;
        public double ASpectRatio;
    }

    public class ColladaCameraOrthographic : ColladaCamera
    {
        public double XMag;
        public double YMag;
        public double AspectRatio;
    }

    // skin
    public class ColladaController
    {

    }

    #endregion

    public class ColladaNode
    {
        private string m_name;
        //private string m_path;
        private bool m_hasTrafo;
        private Trafo3d m_trafo;
        public List<ColladaNode> Children;

        public ColladaNode()
        {
            m_trafo = Trafo3d.Identity;
            //Mesh = null;
            //Material = null;
            Children = new List<ColladaNode>();
        }

        public void Add(ColladaNode node)
        {
            Children.Add(node);
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        //public ColladaMaterial Material
        //{
        //    get { return m_material; }
        //    set { m_material = value; }
        //}

        //public bool HasMaterial
        //{
        //    get { return m_material != null; }
        //}

        public Trafo3d Trafo
        {
            get { return m_trafo; }
            set
            {
                m_trafo = value;
                m_hasTrafo = true;
            }
        }

        //public string Path
        //{
        //    get { return m_path; }
        //}

        //public bool HasMesh
        //{
        //    get { return Mesh != null; }
        //}

        public bool HasTrafo
        {
            get { return m_hasTrafo; }
        }

        public bool HasChildren
        {
            get { return Children.Count > 0; }
        }

        //public Box3d BoundingBox
        //{
        //    get 
        //    {
        //        var result = new Box3d();

        //        if (Mesh != null) result.ExtendBy(Mesh.BoundingBox3d.Transformed(Trafo));
        //        if (Children.Count > 0)
        //        {
        //            foreach (var c in Children) result.ExtendBy(c.BoundingBox);
        //        }

        //        return result;
        //    }
        //}
    }

    public class ColladaGeometryNode : ColladaNode
    {
        public PolyMesh Mesh;
        public ColladaMaterial Material;
    }

    public class ColladaLightNode : ColladaNode
    {
        public ColladaLight Light;
    }

    public class ColladaControllerNode : ColladaNode
    {
        //public PolyMesh Skin;
        //public ColladaMaterial Material;
    }
    
    //public static class ColladaNodeExtensions
    //{
    //    public static ColladaNode FlipYZ(this ColladaNode node)
    //    {
    //        var result = new ColladaNode(node.Path);
    //        result.Mesh = node.Mesh;
    //        result.Children = node.Children;
    //        result.Trafo = node.Trafo * Trafo3d.FromOrthoNormalBasis(V3d.IOO, V3d.OOI, V3d.OIO);
    //        result.Material = node.Material;

    //        return result;
    //    }

    //    public static List<ColladaNode> FlipYZ(this List<ColladaNode> nodes)
    //    {
    //        return nodes.Select(n => FlipYZ(n)).ToList();
    //    }

    //    public static ColladaNode[] FlipYZ(this ColladaNode[] nodes)
    //    {
    //        return nodes.Select(n => FlipYZ(n)).ToArray();
    //    }

    //    public static IEnumerable<ColladaNode> FlipYZ(this IEnumerable<ColladaNode> nodes)
    //    {
    //        return nodes.Select(n => FlipYZ(n));
    //    }
    //}

}
