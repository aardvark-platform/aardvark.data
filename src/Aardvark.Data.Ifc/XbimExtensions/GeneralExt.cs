using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc;
using Xbim.Common.ExpressValidation;
using Xbim.Common.Enumerations;

namespace Aardvark.Data.Ifc
{
    public enum ProjectUnitsExtended
    {
        SIUnitsM,          // Xbim.Common.ProjectUnits in m
        SIUnitsDM,         // Xbim.Common.ProjectUnits in dm
        SIUnitsCM,         // Xbim.Common.ProjectUnits in cm
        SIUnitsUK,         // Xbim.Common.ProjectUnits in mm  
        ImperialUnits,     // Xbim.Common.ProjectUnits
        USCustomaryUnits   // Xbim.Common.ProjectUnits
    }
    public static class GeneralExt
    {
        #region Convenient Functions
        public static T New<T>(this IModel model, Action<T> func) where T : IInstantiableEntity
        {
            // Convenient function to directly create entities from model
            // TODO...should not be used -> use model.Factory to guarantee ifc-schema independent creation!
            return model.Instances.New(func);
        }

        public static EntityCreator Factory(this IModel model)
            => new (model);

        public static IIfcProject GetProject(this IfcStore model)
            => model.Instances.FirstOrDefault<IIfcProject>();

        public static IIfcObjectDefinition GetParent(this IIfcObjectDefinition o)
            => o.Decomposes.Select(r => r.RelatingObject).FirstOrDefault();

        public static IEnumerable<IIfcObjectDefinition> GetSiblings(this IIfcObjectDefinition o)
            => o.Decomposes.SelectMany(r => r.RelatedObjects).Where(x => !o.Equals(x));

        public static IEnumerable<IIfcObjectDefinition> GetChildren(this IIfcObjectDefinition o)
        {
            var children = o.IsDecomposedBy.SelectMany(r => r.RelatedObjects);

            if ((o as IIfcSpatialStructureElement) != null)
            {
                children = children.Concat(((IIfcSpatialStructureElement)o).ContainsElements.SelectMany(rel => rel.RelatedElements).Cast<IIfcObjectDefinition>());
            }

            return children;
        }

        #endregion

        #region Scene
        public static IEnumerable<ValidationResult> ValidateModel(this IfcStore model)
        {
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var result = validator.Validate(model.Instances);

            result.ForEach(error =>
            {
                Report.Line(error.Message + " with " + error.Details.Count());
                error.Details.ForEach(detail => Report.Line(detail.IssueSource + " " + detail.IssueType));
            });

            return result;
        }

        public static void CreateMinimalProject(this IfcStore model, ProjectUnitsExtended units = ProjectUnitsExtended.SIUnitsCM, string projectName = "Project", string siteName = "Site")
        {
            using var txnInit = model.BeginTransaction("Init Project");
            // there should always be one project in the model
            var project = model.Factory().Project(p => p.Name = projectName);

            // our shortcut to define basic default units

            var xBimDefaultUnits = units switch
            {
                ProjectUnitsExtended.ImperialUnits => ProjectUnits.ImperialUnits,
                ProjectUnitsExtended.USCustomaryUnits => ProjectUnits.USCustomaryUnits,
                ProjectUnitsExtended.SIUnitsUK => ProjectUnits.SIUnitsUK,
                _ => ProjectUnits.SIUnitsUK,
            };

            project.Initialize(xBimDefaultUnits);

            switch (units)
            {
                case ProjectUnitsExtended.SIUnitsM : project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null); break;
                case ProjectUnitsExtended.SIUnitsDM: project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, IfcSIPrefix.DECI); break;
                case ProjectUnitsExtended.SIUnitsCM : project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, IfcSIPrefix.CENTI); break;
            };

            // add site
            var site = model.Factory().Site(w => w.Name = siteName);
            project.AddSite(site);

            txnInit.Commit();
        }
        #endregion
    }
}