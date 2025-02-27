using System;
using System.Collections.Generic;
using Aardvark.Base;
using Aardvark.Data.Photometry;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;

namespace Aardvark.Data.Ifc
{
    public static class IfcLightingExtensions
    {
        public static IIfcLightFixtureType CreateLightType(this IModel model, IfcLightFixtureTypeEnum lightType, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties, string name = "")
        {
            var proxy = model.Factory().LightFixtureType(p =>
            {
                p.PredefinedType = lightType;
                if (name == "") p.Name = $"LightType_{lightType}"; 
                else p.Name = name;
            });
            proxy.AttachGeometriesAndProperties(repMaps, properties);
            return proxy;
        }

        public static IIfcLightFixture Instantiate(this IIfcLightFixtureType lightType, string name, IIfcObjectPlacement placement, Trafo3d trafo, IfcLightFixtureTypeEnum? ltenum = null)
        {
            // only support for IFC4+
            var light = lightType.Model.LightFixtureExt();
            light.Name = name;
            
            var instance = (IIfcLightFixture) light.CreateAttachInstancedRepresentation(lightType, placement, trafo);

            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue) instance.PredefinedType = ltenum;
            return instance;
        }

        public static IIfcLightFixture Instantiate(this IIfcLightFixtureType lightType, string name, IIfcObjectPlacement placement, IDictionary<IIfcRepresentationMap, Trafo3d> trafos, IfcLightFixtureTypeEnum? ltenum = null)
        {
            // only support for IFC4+
            var light = lightType.Model.LightFixtureExt();
            light.Name = name;

            var instance = (IIfcLightFixture) light.CreateAttachInstancedRepresentation(lightType, placement, trafos);

            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue) instance.PredefinedType = ltenum;
            return instance;
        }

        public static IIfcPropertySet Pset_LightFixtureTypeCommon(this IModel model, string reference, int numberOfSources, double totalWattage,
            double maintenanceFactor, double maximumPlenumSensibleLoad, double maximumSpaceSensibleLoad, double sensibleLoadToRadiant,
            string status, string lightFixtureMountingType, string lightFixturePlacingType)
        {
            return model.Factory().PropertySet(pset =>
            {
                pset.Name = "Pset_LightFixtureTypeCommon";
                pset.HasProperties.Add(model.CreatePropertySingleValue("Reference", new IfcIdentifier(reference)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("NumberOfSources", new IfcInteger(numberOfSources)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("TotalWattage", new IfcPowerMeasure(totalWattage)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("MaintenanceFactor", new IfcReal(maintenanceFactor)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("MaximumPlenumSensibleLoad", new IfcPowerMeasure(maximumPlenumSensibleLoad)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("MaximumSpaceSensibleLoad", new IfcPowerMeasure(maximumSpaceSensibleLoad)));
                pset.HasProperties.Add(model.CreatePropertySingleValue("SensibleLoadToRadiant", new IfcPositiveRatioMeasure(sensibleLoadToRadiant)));
                pset.HasProperties.Add(model.CreatePropertyEnumeratedValue("Status", new IfcLabel(status)));
                pset.HasProperties.Add(model.CreatePropertyEnumeratedValue("LightFixtureMountingType", new IfcLabel(lightFixtureMountingType)));
                pset.HasProperties.Add(model.CreatePropertyEnumeratedValue("LightFixturePlacingType", new IfcLabel(lightFixturePlacingType)));
            });
        }

        // IIfcLightFixture replaces general IIfcFlowTerminal in IFC4+
        public static void Qto_LightFixtureBaseQuantities(this IIfcFlowTerminal light, double weight)
        {
            var q = light.Model.CreatePhysicalSimpleQuantity(XbimQuantityTypeEnum.Weight, weight, "GrossWeight");
            light.AddQuantity("Qto_LightFixtureBaseQuantities", q);
        }

        public static IIfcLightFixture CreateLightEmpty(this IModel model, string name, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape, IfcLightFixtureTypeEnum? lightType = null)
        {
            // CAUTION only supports IFC4+
            return model.LightFixtureExt(t =>
            {
                if (lightType != null) t.PredefinedType = lightType.Value;
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = model.Factory().ProductDefinitionShape(r => r.Representations.Add(lightShape));
            });
        }

        public static IIfcLightFixture CreateLightAmbient(this IModel model, string name, C3d color, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingAmbient(color));
            return light;
        }

        public static IIfcLightFixture CreateLightPositional(this IModel model, string name, C3d color, V3d position, double radius, V3d attenuation, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingPositional(color, position, radius, attenuation.X, attenuation.Y, attenuation.Z));
            return light;
        }

        public static IIfcLightFixture CreateLightDirectional(this IModel model, string name, C3d color, V3d direction, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingDirectional(color, direction));
            return light;
        }

        public static IIfcLightFixture CreateLightSpot(this IModel model, string name, C3d color, V3d position, V3d direction, double radius, V3d attenuation, double spreadAngle, double beamWidthAngle, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingSpot(color, position, direction, radius, attenuation.X, attenuation.Y, attenuation.Z, spreadAngle, beamWidthAngle));
            return light;
        }

        public static IIfcLightIntensityDistribution CreateLightGoniometricDistribution(this IModel model, LightMeasurementData data)
        {
            // TODO: resolve symmetry modes from light measurment data 
            // TODO: convert intensities to candela per lumen

            throw new NotImplementedException();

            //IEnumerable<IFCHelper.LightIntensityDistributionData> lightData = [
            //    // Main plane-angle and its secondary-plane-angles     
            //    new IFCHelper.LightIntensityDistributionData(0, [new IFCHelper.AngleAndIntensity(0.0, 100.0), new IFCHelper.AngleAndIntensity(90.0, 200.0), new IFCHelper.AngleAndIntensity(180.0, 100.0)]),
            //    new IFCHelper.LightIntensityDistributionData(180, [new IFCHelper.AngleAndIntensity(0.0, 10.0), new IFCHelper.AngleAndIntensity(45.0, 15.0), new IFCHelper.AngleAndIntensity(90.0, 20.0), new IFCHelper.AngleAndIntensity(135.0, 15.0), new IFCHelper.AngleAndIntensity(180.0, 10.0)])
            //];

            //return model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, lightData);
        }

        public static IIfcLightFixture CreateLightGoniometric(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, LightMeasurementData data, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
            => CreateLightGoniometric(model, name, color, position, colourTemperature, luminousFlux, CreateLightGoniometricDistribution(model, data), placement, lightShape);

        public static IIfcLightFixture CreateLightGoniometric(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, IIfcLightIntensityDistribution distribution, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingGoniometric(color, position, colourTemperature, luminousFlux, distribution));
            return light;
        }


        #region Lights with BackWardCompability (IFC2x3 - FlowTerminal)

        public static IIfcFlowTerminal InstantiateIFC2x3(this IIfcLightFixtureType lightType, string name, IIfcObjectPlacement placement, Trafo3d trafo, IfcLightFixtureTypeEnum? ltenum = null)
        {
            var light = lightType.Model.LightFixtureExtIFC2x3();
            light.Name = name;

            var instance = (IIfcFlowTerminal)light.CreateAttachInstancedRepresentation(lightType, placement, trafo);

            // Set predefined type for IIfcLightFixture
            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue && instance is IIfcLightFixture lf) lf.PredefinedType = ltenum;

            return instance;
        }

        public static IIfcFlowTerminal InstantiateIFC2x3(this IIfcLightFixtureType lightType, string name, IIfcObjectPlacement placement, IDictionary<IIfcRepresentationMap, Trafo3d> trafos, IfcLightFixtureTypeEnum? ltenum = null)
        {
            var light = lightType.Model.LightFixtureExtIFC2x3();
            light.Name = name;

            var instance = (IIfcFlowTerminal)light.CreateAttachInstancedRepresentation(lightType, placement, trafos);

            // Set predefined type for IIfcLightFixture
            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue && instance is IIfcLightFixture lf) lf.PredefinedType = ltenum;

            return instance;
        }

        public static IIfcFlowTerminal CreateLightEmptyIFC2x3(this IModel model, string name, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape, IfcLightFixtureTypeEnum? lightType = null)
        {
            Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum? ifc2x3LightType = null;

            if (model.SchemaVersion == XbimSchemaVersion.Ifc2X3)
            {
                if (lightType == null)
                {
                    ifc2x3LightType = Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum.NOTDEFINED;
                }
                else
                {
                    ifc2x3LightType = lightType.Value switch
                    {
                        IfcLightFixtureTypeEnum.POINTSOURCE => Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum.POINTSOURCE,
                        IfcLightFixtureTypeEnum.DIRECTIONSOURCE => Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum.DIRECTIONSOURCE,
                        IfcLightFixtureTypeEnum.USERDEFINED => Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum.USERDEFINED,
                        _ => Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum.NOTDEFINED
                    };
                }
            }

            var t = model.LightFixtureExtIFC2x3(ifc2x3LightType);

            t.Name = name;
            t.ObjectPlacement = placement;
            t.Representation = model.Factory().ProductDefinitionShape(r => r.Representations.Add(lightShape));
            if (lightType != null && t is IIfcLightFixture lf) lf.PredefinedType = lightType.Value;

            return t;
        }

        public static IIfcFlowTerminal CreateLightAmbientIFC2x3(this IModel model, string name, C3d color, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmptyIFC2x3(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingAmbient(color));
            return light;
        }

        public static IIfcFlowTerminal CreateLightPositionalIFC2x3(this IModel model, string name, C3d color, V3d position, double radius, V3d attenuation, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmptyIFC2x3(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingPositional(color, position, radius, attenuation.X, attenuation.Y, attenuation.Z));
            return light;
        }

        public static IIfcFlowTerminal CreateLightDirectionalIFC2x3(this IModel model, string name, C3d color, V3d direction, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmptyIFC2x3(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingDirectional(color, direction));
            return light;
        }

        public static IIfcFlowTerminal CreateLightSpotIFC2x3(this IModel model, string name, C3d color, V3d position, V3d direction, double radius, V3d attenuation, double spreadAngle, double beamWidthAngle, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmptyIFC2x3(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingSpot(color, position, direction, radius, attenuation.X, attenuation.Y, attenuation.Z, spreadAngle, beamWidthAngle));
            return light;
        }

        public static IIfcFlowTerminal CreateLightGoniometricDistributionIFC2x3(this IModel model, LightMeasurementData data)
        {
            // TODO: resolve symmetry modes from light measurment data 
            // TODO: convert intensities to candela per lumen

            throw new NotImplementedException();

            //IEnumerable<IFCHelper.LightIntensityDistributionData> lightData = [
            //    // Main plane-angle and its secondary-plane-angles     
            //    new IFCHelper.LightIntensityDistributionData(0, [new IFCHelper.AngleAndIntensity(0.0, 100.0), new IFCHelper.AngleAndIntensity(90.0, 200.0), new IFCHelper.AngleAndIntensity(180.0, 100.0)]),
            //    new IFCHelper.LightIntensityDistributionData(180, [new IFCHelper.AngleAndIntensity(0.0, 10.0), new IFCHelper.AngleAndIntensity(45.0, 15.0), new IFCHelper.AngleAndIntensity(90.0, 20.0), new IFCHelper.AngleAndIntensity(135.0, 15.0), new IFCHelper.AngleAndIntensity(180.0, 10.0)])
            //];

            //return model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, lightData);
        }

        public static IIfcFlowTerminal CreateLightGoniometricIFC2x3(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, LightMeasurementData data, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
            => CreateLightGoniometric(model, name, color, position, colourTemperature, luminousFlux, CreateLightGoniometricDistribution(model, data), placement, lightShape);

        public static IIfcFlowTerminal CreateLightGoniometricIFC2x3(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, IIfcLightIntensityDistribution distribution, IIfcObjectPlacement placement, IIfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmptyIFC2x3(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingGoniometric(color, position, colourTemperature, luminousFlux, distribution));
            return light;
        }

        #endregion
    }
}