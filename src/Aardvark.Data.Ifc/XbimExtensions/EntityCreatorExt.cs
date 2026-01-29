using System;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class EntityCreatorExtensions
    {
        public static @IIfcLightFixture LightFixtureFactory(this IModel model, Action<@IIfcLightFixture> init = null)
        {
            return model.SchemaVersion switch
            {
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.ElectricalDomain.IfcLightFixture>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.ElectricalDomain.IfcLightFixture>(init),
                _ => throw new NotSupportedException($"Type IfcLightFixture is not supported in schema {model.SchemaVersion}") // IfcLightFixture Not supported in 2x3
            };
        }

        public static @IIfcFlowTerminal LightFixtureIFC2x3Factory(this IModel model, Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum? lightType = null)
        {
            return model.SchemaVersion switch
            {
                XbimSchemaVersion.Ifc2X3 =>
                    // NOTE: Ifc2X3 requires a LightFixtureType to type FlowTerminal as Light

                    // CAUTION - Instantiation from LightFixtureType: CreateAttachInstancedRepresentation links LightFixtureType to Flowterminal! (lightType == null)
                    // CAUTION - Direct light creation: IFC2x3 requires an IfcLightFixtureTypeEnum! (lightType != null)
                    model.Instances.New<Xbim.Ifc2x3.SharedBldgServiceElements.IfcFlowTerminal>(o => {
                        if (lightType != null)
                        {
                            o.AddDefiningType(model.Instances.New<Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureType>(lt => {
                                lt.PredefinedType = lightType.Value;
                                lt.Name = "LightType";
                            }));    
                        }
                    }),
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.ElectricalDomain.IfcLightFixture>(),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.ElectricalDomain.IfcLightFixture>(),
                _ => throw new NotSupportedException($"Type IfcLightFixture is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcCartesianPointList2D CartesianPointList2DFactory(this IModel model, Action<@IIfcCartesianPointList2D> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcCartesianPointList2D Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometricModelResource.IfcCartesianPointList2D>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometricModelResource.IfcCartesianPointList2D>(init),
                _ => throw new NotSupportedException($"Type IfcCartesianPointList2D is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcCartesianPointList3D CartesianPointList3DFactory(this IModel model, Action<@IIfcCartesianPointList3D> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcCartesianPointList3D Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometricModelResource.IfcCartesianPointList3D>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometricModelResource.IfcCartesianPointList3D>(init),
                _ => throw new NotSupportedException($"Type IfcCartesianPointList3D is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcIndexedPolygonalFace IndexedPolygonalFaceFactory(this IModel model, Action<IIfcIndexedPolygonalFace> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcIndexedPolygonalFace Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometricModelResource.IfcIndexedPolygonalFace>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometricModelResource.IfcIndexedPolygonalFace>(init),
                _ => throw new NotSupportedException($"Type IfcIndexedPolygonalFace is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcPolygonalFaceSet PolygonalFaceSetFactory(this IModel model, Action<IIfcPolygonalFaceSet> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcPolygonalFaceSet Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometricModelResource.IfcPolygonalFaceSet>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometricModelResource.IfcPolygonalFaceSet>(init),
                _ => throw new NotSupportedException($"Type IfcPolygonalFaceSet is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcTriangulatedFaceSet TriangulatedFaceSetFactory(this IModel model, Action<IIfcTriangulatedFaceSet> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcTriangulatedFaceSet Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometricModelResource.IfcTriangulatedFaceSet>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometricModelResource.IfcTriangulatedFaceSet>(init),
                _ => throw new NotSupportedException($"Type IfcTriangulatedFaceSet is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcIndexedPolyCurve IndexedPolyCurveFactory(this IModel model, Action<IIfcIndexedPolyCurve> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcIndexedPolyCurve Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.GeometryResource.IfcIndexedPolyCurve>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.GeometryResource.IfcIndexedPolyCurve>(init),
                _ => throw new NotSupportedException($"Type IfcIndexedPolyCurve is not supported in schema {model.SchemaVersion}")
            };
        }

        public static IIfcMaterialProperties MaterialPropertiesFactory(this IModel model, Action<IIfcMaterialProperties> init = null)
        {
            return model.SchemaVersion switch
            {
                // IIfcMaterialProperties Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.MaterialResource.IfcMaterialProperties>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.MaterialResource.IfcMaterialProperties>(init),
                _ => throw new NotSupportedException($"Type IfcMaterialProperties is not supported in schema {model.SchemaVersion}")
            };
        }
    }
}