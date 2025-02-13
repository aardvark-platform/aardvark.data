using Aardvark.Base;
using System.Collections.Generic;
using System.Linq;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MaterialResource;
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

        public static IfcMaterialProperties CreateAttachPsetMaterialCommon(this IfcMaterial material, double molecularWeight, double porosity, double massDensity)
        {
            return material.Model.New<IfcMaterialProperties>(ps => {
                ps.Name = "Pset_MaterialCommon";
                ps.Material = material;
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("MolecularWeight", new IfcMolecularWeightMeasure(molecularWeight)));
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("Porosity", new IfcNormalisedRatioMeasure(porosity)));
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("MassDensity", new IfcMassDensityMeasure(massDensity)));
            });
        }

        public static IfcMaterialProperties CreateAttachPsetMaterialThermal(this IfcMaterial material, double thermalConductivity, double specificHeatCapacity, double boilingPoint = 100.0, double freezingPoint = 0.0)
        {
            return material.Model.New<IfcMaterialProperties>(ps => {
                ps.Name = "Pset_MaterialThermal";
                ps.Material = material;
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("ThermalConductivity", new IfcThermalConductivityMeasure(thermalConductivity)));
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("SpecificHeatCapacity", new IfcSpecificHeatCapacityMeasure(specificHeatCapacity)));
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("BoilingPoint", new IfcThermodynamicTemperatureMeasure(boilingPoint)));
                ps.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)material.Model.CreatePropertySingleValue("FreezingPoint", new IfcThermodynamicTemperatureMeasure(freezingPoint)));
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
