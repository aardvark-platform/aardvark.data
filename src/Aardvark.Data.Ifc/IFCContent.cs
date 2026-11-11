using Aardvark.Base;
using Aardvark.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;

namespace Aardvark.Data.Ifc
{
    /// <summary>
    /// One geometry of a product: a mesh tessellated by xBIM or a line set read from a curve representation ('Axis', 'Annotation',
    /// 'FootPrint', curve items of a 'Body'). A product with several geometries has one entry per geometry under its GUID in IFCData.Content.
    /// </summary>
    public class IFCContent
    {
        public IIfcObject IFCObject { get; private set; }
        public IFCMaterial Material { get; private set; }

        /// <summary>Tessellated geometry in local coordinates, null for a line-set entry (see HasMesh).</summary>
        public PolyMesh Mesh { get; private set; }

        /// <summary>Polylines in local coordinates, null for a mesh entry (see HasLineSet).</summary>
        public PolyLineSet LineSet { get; private set; }

        /// <summary>Local to model coordinates: the shape instance transformation of a mesh, the product placement of a line set.</summary>
        public Trafo3d Trafo { get; private set; }

        /// <summary>xBIM's opening handling of a mesh; a line set has no openings and reports OpeningsAndAdditionsIncluded.</summary>
        public XbimGeometryRepresentationType RepresentationType { get; private set; }

        /// <summary>RepresentationIdentifier of the shape representation a line set came from ('Axis', 'Annotation', 'FootPrint', 'Body'), null for a mesh entry.</summary>
        public string RepresentationIdentifier { get; private set; }
        public string Name { get { return IFCObject.Name; } }
        public string ExpressType { get { return IFCObject.ExpressType.Name; } }
        public bool HasMesh { get { return Mesh != null; } }
        public bool HasLineSet { get { return LineSet != null; } }

        public IFCContent(IIfcObject o, PolyMesh mesh, Trafo3d trafo, IFCMaterial mat,
                          XbimGeometryRepresentationType representationType)
        {
            IFCObject = o;
            Mesh = mesh;
            Trafo = trafo;
            Material = mat;
            RepresentationType = representationType;
        }

        public IFCContent(IIfcObject o, PolyLineSet lineSet, Trafo3d trafo, IFCMaterial mat, string representationIdentifier)
        {
            IFCObject = o;
            LineSet = lineSet;
            Trafo = trafo;
            Material = mat;
            RepresentationType = XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded;
            RepresentationIdentifier = representationIdentifier;
        }
    }
}
