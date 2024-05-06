using Aardvark.Base;
using Aardvark.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;

namespace Aardvark.Data.Ifc
{
    public class IFCContent
    {
        public IIfcObject IFCObject { get; private set; }
        public IFCMaterial Material { get; private set; }
        public PolyMesh Mesh { get; private set; }
        public Trafo3d Trafo { get; private set; }
        public XbimGeometryRepresentationType RepresentationType { get; private set; }
        public string Name { get { return IFCObject.Name; } }
        public string ExpressType { get { return IFCObject.ExpressType.Name; } }

        public IFCContent(IIfcObject o, PolyMesh mesh, Trafo3d trafo, IFCMaterial mat,
                          XbimGeometryRepresentationType representationType)
        {
            IFCObject = o;
            Mesh = mesh;
            Trafo = trafo;
            Material = mat;
            RepresentationType = representationType;
        }
    }
}
