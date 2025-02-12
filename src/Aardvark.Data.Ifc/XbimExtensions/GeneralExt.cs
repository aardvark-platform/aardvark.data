using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc;
using Xbim.Common.ExpressValidation;
using Xbim.Common.Enumerations;

namespace Aardvark.Data.Ifc
{
    public static class GeneralExt
    {
        #region Convenient Functions
        public static T New<T>(this IModel model, Action<T> func) where T : IInstantiableEntity
        {
            // Convenient function to directly create entities from model
            return model.Instances.New(func);
        }

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

        public static void CreateMinimalProject(this IfcStore model, ProjectUnits units = ProjectUnits.SIUnitsUK, string projectName = "Project", string siteName = "Site")
        {
            using var txnInit = model.BeginTransaction("Init Project");
            // there should always be one project in the model
            var project = model.New<IfcProject>(p => p.Name = projectName);
            // our shortcut to define basic default units
            project.Initialize(units);

            // add site
            var site = model.New<IfcSite>(w => w.Name = siteName);
            project.AddSite(site);

            txnInit.Commit();
        }
        #endregion
    }
}