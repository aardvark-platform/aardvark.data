using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace Aardvark.Data.Ifc
{
    public static class MaterialExt
    {
        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcMaterial mat)
        {
            return mat.HasProperties
                    .SelectMany(mp => mp.Properties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static Dictionary<string, IIfcPropertySingleValue> GetPropertiesDict(this IIfcMaterial mat) => PropertiesExt.DistinctDictionaryFromPropertiesValues(mat.GetProperties());

        private static void AddProperty(this IIfcMaterialProperties ps, IIfcProperty property)
        {
            // the cross-schema interface only exposes Properties read-only, adding needs the concrete schema type
            switch (ps)
            {
                case Xbim.Ifc4.MaterialResource.IfcMaterialProperties p4:
                    p4.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)property);
                    break;
                case Xbim.Ifc4x3.MaterialResource.IfcMaterialProperties p4x3:
                    p4x3.Properties.Add((Xbim.Ifc4x3.PropertyResource.IfcProperty)property);
                    break;
                default:
                    throw new NotSupportedException($"IfcMaterialProperties are not supported in schema {ps.Model.SchemaVersion}");
            }
        }

        public static IIfcMaterialProperties CreateAttachPsetMaterialCommon(this IIfcMaterial material, double molecularWeight, double porosity, double massDensity)
        {
            var model = material.Model;
            return model.MaterialPropertiesFactory(ps => {
                ps.Name = "Pset_MaterialCommon";
                ps.Material = material;
                ps.AddProperty(model.CreatePropertySingleValue("MolecularWeight", new IfcMolecularWeightMeasure(molecularWeight)));
                ps.AddProperty(model.CreatePropertySingleValue("Porosity", new IfcNormalisedRatioMeasure(porosity)));
                ps.AddProperty(model.CreatePropertySingleValue("MassDensity", new IfcMassDensityMeasure(massDensity)));
            });
        }

        public static IIfcMaterialProperties CreateAttachPsetMaterialThermal(this IIfcMaterial material, double thermalConductivity, double specificHeatCapacity, double boilingPoint = 100.0, double freezingPoint = 0.0)
        {
            var model = material.Model;
            return model.MaterialPropertiesFactory(ps => {
                ps.Name = "Pset_MaterialThermal";
                ps.Material = material;
                ps.AddProperty(model.CreatePropertySingleValue("ThermalConductivity", new IfcThermalConductivityMeasure(thermalConductivity)));
                ps.AddProperty(model.CreatePropertySingleValue("SpecificHeatCapacity", new IfcSpecificHeatCapacityMeasure(specificHeatCapacity)));
                ps.AddProperty(model.CreatePropertySingleValue("BoilingPoint", new IfcThermodynamicTemperatureMeasure(boilingPoint)));
                ps.AddProperty(model.CreatePropertySingleValue("FreezingPoint", new IfcThermodynamicTemperatureMeasure(freezingPoint)));
            });
        }

        public static IIfcMaterialDefinitionRepresentation CreateAttachStyledRepresentation(this IIfcMaterial material, C3d surfaceColor)
        {
            var factory = material.Model.Factory();
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcrepresentationresource/lexical/ifcmaterialdefinitionrepresentation.htm
            return factory.MaterialDefinitionRepresentation(def =>
            {
                def.RepresentedMaterial = material;
                def.Representations.Add(factory.StyledRepresentation(rep => {
                    rep.ContextOfItems = material.Model.GetGeometricRepresentationContextModel();
                    rep.Items.Add(factory.StyledItem(styleItem => {
                        //styleItem.Item -> NOTE: If the IfcStyledItem is used within a reference from an IfcMaterialDefinitionRepresentation then no Item shall be provided.
                        styleItem.Styles.Add(material.Model.CreateSurfaceStyle(surfaceColor));
                    }));
                }));
            });
        }
    }
}
