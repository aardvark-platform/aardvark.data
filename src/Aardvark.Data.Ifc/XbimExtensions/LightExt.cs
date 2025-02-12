using System;
using System.Collections.Generic;
using Aardvark.Base;
using Aardvark.Data.Photometry;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ElectricalDomain;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.RepresentationResource;

namespace Aardvark.Data.Ifc
{
    public static class IfcLightingExtensions
    {
        public static IfcLightFixtureType CreateLightType(this IModel model, IfcLightFixtureTypeEnum lightType, IEnumerable<IfcRepresentationMap> repMaps, IEnumerable<IfcPropertySetDefinition> properties, string name = "")
        {
            var proxy = ProductExt.CreateTypeProduct<IfcLightFixtureType>(model, repMaps, properties, name);
            proxy.PredefinedType = lightType;
            return proxy;
        }

        public static IfcLightFixture Instantiate(this IfcLightFixtureType lightType, string name, IfcObjectPlacement placement, Trafo3d trafo, IfcLightFixtureTypeEnum? ltenum = null)
        {
            var instance = ProductExt.Instantiate<IfcLightFixtureType, IfcLightFixture>(lightType, name, placement, trafo);
            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue) instance.PredefinedType = ltenum;
            return instance;
        }

        public static IfcLightFixture Instantiate(this IfcLightFixtureType lightType, string name, IfcObjectPlacement placement, Dictionary<IfcRepresentationMap, Trafo3d> trafos, IfcLightFixtureTypeEnum? ltenum = null)
        {
            var instance = ProductExt.Instantiate<IfcLightFixtureType, IfcLightFixture>(lightType, name, placement, trafos);
            if (lightType.PredefinedType != IfcLightFixtureTypeEnum.NOTDEFINED && ltenum.HasValue) instance.PredefinedType = ltenum;
            return instance;
        }

        public static IfcPropertySet Pset_LightFixtureTypeCommon(this IfcLightFixture light, string reference, int numberOfSources, double totalWattage,
            double maintenanceFactor, double maximumPlenumSensibleLoad, double maximumSpaceSensibleLoad, double sensibleLoadToRadiant,
            string status, string lightFixtureMountingType, string lightFixturePlacingType)
        {
            var propertySet = light.Model.New<IfcPropertySet>(pset => {
                pset.Name = "Pset_LightFixtureTypeCommon";
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("Reference", new IfcIdentifier(reference)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("NumberOfSources", new IfcInteger(numberOfSources)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("TotalWattage", new IfcPowerMeasure(totalWattage)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("MaintenanceFactor", new IfcReal(maintenanceFactor)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("MaximumPlenumSensibleLoad", new IfcPowerMeasure(maximumPlenumSensibleLoad)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("MaximumSpaceSensibleLoad", new IfcPowerMeasure(maximumSpaceSensibleLoad)));
                pset.HasProperties.Add(light.Model.CreatePropertySingleValue("SensibleLoadToRadiant", new IfcPositiveRatioMeasure(sensibleLoadToRadiant)));
                pset.HasProperties.Add(light.Model.CreatePropertyEnumeratedValue("Status", new IfcLabel(status)));
                pset.HasProperties.Add(light.Model.CreatePropertyEnumeratedValue("LightFixtureMountingType", new IfcLabel(lightFixtureMountingType)));
                pset.HasProperties.Add(light.Model.CreatePropertyEnumeratedValue("LightFixturePlacingType", new IfcLabel(lightFixturePlacingType)));
            });

            light.AddPropertySet(propertySet);
            return propertySet;
        }

        public static IIfcElementQuantity Qto_LightFixtureBaseQuantities(this IfcLightFixture light, double weight)
        {
            return light.AddQuantity("Qto_LightFixtureBaseQuantities", light.Model.CreateQuantityWeight("GrossWeight", weight));
        }

        public static IfcLightFixture CreateLightEmpty(this IModel model, string name, IfcObjectPlacement placement, IfcShapeRepresentation lightShape, IfcLightFixtureTypeEnum? lightType = null)
        {
            return model.New<IfcLightFixture>(t =>
            {
                if (lightType != null) t.PredefinedType = lightType.Value;
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(lightShape));
            });
        }

        public static IfcLightFixture CreateLightAmbient(this IModel model, string name, C3d color, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingAmbient(color));
            return light;
        }

        public static IfcLightFixture CreateLightPositional(this IModel model, string name, C3d color, V3d position, double radius, V3d attenuation, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingPositional(color, position, radius, attenuation.X, attenuation.Y, attenuation.Z));
            return light;
        }

        public static IfcLightFixture CreateLightDirectional(this IModel model, string name, C3d color, V3d direction, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingDirectional(color, direction));
            return light;
        }

        public static IfcLightFixture CreateLightSpot(this IModel model, string name, C3d color, V3d position, V3d direction, double radius, V3d attenuation, double spreadAngle, double beamWidthAngle, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.DIRECTIONSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingSpot(color, position, direction, radius, attenuation.X, attenuation.Y, attenuation.Z, spreadAngle, beamWidthAngle));
            return light;
        }

        public static IfcLightIntensityDistribution CreateLightGoniometricDistribution(this IModel model, LightMeasurementData data)
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

        public static IfcLightFixture CreateLightGoniometric(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, LightMeasurementData data, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
            => CreateLightGoniometric(model, name, color, position, colourTemperature, luminousFlux, CreateLightGoniometricDistribution(model, data), placement, lightShape);

        public static IfcLightFixture CreateLightGoniometric(this IModel model, string name, C3d color, V3d position, double colourTemperature, double luminousFlux, IfcLightIntensityDistribution distribution, IfcObjectPlacement placement, IfcShapeRepresentation lightShape)
        {
            var light = CreateLightEmpty(model, name, placement, lightShape, IfcLightFixtureTypeEnum.POINTSOURCE);
            light.Representation.Representations.Add(model.CreateShapeRepresentationLightingGoniometric(color, position, colourTemperature, luminousFlux, distribution));
            return light;
        }
    }

}
