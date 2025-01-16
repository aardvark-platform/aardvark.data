using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc.Extensions;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ElectricalDomain;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.PresentationDefinitionResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace Aardvark.Data.Ifc
{
    public static class IFCHelper
    {
        #region Properties

        private static bool EqualOrContainsName(this IIfcPropertySingleValue value, string queryString)
           => string.Equals(value.Name, queryString, StringComparison.OrdinalIgnoreCase) || value.Name.ToString().ToLower().Contains(queryString.ToLower());

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcObject o)
        {
            return o.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcObject o, string propertySetName)
        {
            return o.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .Where(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).Name == propertySetName)
                    .SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static IEnumerable<IIfcPropertySingleValue> GetPropertiesFromType(this IIfcObject o)
        {
            return
                o.IsTypedBy
                .SelectMany(o => o.RelatingType.HasPropertySets)
                .OfType<IIfcPropertySet>()
                .SelectMany(pset => pset.HasProperties)
                .OfType<IIfcPropertySingleValue>();
        }

        public static IEnumerable<IIfcPropertySingleValue> GetAllProperties(this IIfcObject o)
        {
            var p1 = o.GetProperties();
            var p2 = o.GetPropertiesFromType();

            return p1.Concat(p2);
        }


        public static IIfcValue GetPropertyFromType(this IIfcObject o, string name)
            => o.GetPropertiesFromType().FirstOrDefault(p => p.EqualOrContainsName(name))?.NominalValue;

        public static IIfcValue GetPropertyLocal(this IIfcObject o, string name) 
            => o.GetProperties().FirstOrDefault(p => p.EqualOrContainsName(name))?.NominalValue;

        public static IIfcValue GetProperty(this IIfcObject o, string name)
        {
            // first check for local properties
            var res = o.GetPropertyLocal(name);
            if (res != null) return res;

            // if not available check for properties from type-object
            return o.GetPropertyFromType(name);
        }

        #region Property-Value

        public static bool TryGetSimpleValue2<T>(this IExpressValueType ifcValue, out T result) where T : struct
        {
            static DateTime ReadDateTime(string str)
            {
                try
                {
                    var parts = str.Split([':', '-', 'T', 'Z'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 6) //it is a date time
                    {
                        var year = Convert.ToInt32(parts[0]);
                        var month = Convert.ToInt32(parts[1]);
                        var day = Convert.ToInt32(parts[2]);
                        var hours = Convert.ToInt32(parts[3]);
                        var minutes = Convert.ToInt32(parts[4]);
                        var seconds = Convert.ToInt32(parts[5]);
                        return new DateTime(year, month, day, hours, minutes, seconds, str.Last() == 'Z' ? DateTimeKind.Utc : DateTimeKind.Unspecified);
                    }
                    if (parts.Length == 3) //it is a date
                    {
                        var year = Convert.ToInt32(parts[0]);
                        var month = Convert.ToInt32(parts[1]);
                        var day = Convert.ToInt32(parts[2]);
                        return new DateTime(year, month, day);
                    }
                }
                catch (Exception)
                {
                    Report.Warn("Date Time Conversion: An illegal date time string has been found [{stringValue}]", str);
                }
                return default;
            }

            var value = new T();

            try
            {
                if (ifcValue is IfcMonetaryMeasure)
                {
                    value = (T)Convert.ChangeType(ifcValue.Value, typeof(T));
                }
                else if (ifcValue is IfcTimeStamp timeStamp)
                {
                    value = (T)Convert.ChangeType(timeStamp.ToDateTime(), typeof(T));
                }
                else if (value is DateTime) //sometimes these are written as strings in the ifc file
                {
                    value = (T)Convert.ChangeType(ReadDateTime(ifcValue.Value.ToString()), typeof(T));
                }
                else if (ifcValue.UnderlyingSystemType == typeof(int) || ifcValue.UnderlyingSystemType == typeof(long) || ifcValue.UnderlyingSystemType == typeof(short) || ifcValue.UnderlyingSystemType == typeof(byte))
                {
                    value = (T)Convert.ChangeType(ifcValue.Value, typeof(T));
                }
                else if (ifcValue.UnderlyingSystemType == typeof(double) || ifcValue.UnderlyingSystemType == typeof(float))
                {
                    value = (T)Convert.ChangeType(ifcValue.Value, typeof(T));
                }
                else if (ifcValue.UnderlyingSystemType == typeof(string))
                {
                    value = (T)Convert.ChangeType(ifcValue.Value, typeof(T));
                }
                else if (ifcValue.UnderlyingSystemType == typeof(bool) || ifcValue.UnderlyingSystemType == typeof(bool?))
                {
                    value = (T)Convert.ChangeType(ifcValue.Value, typeof(T));
                }
            }
            catch (Exception)
            {
                result = default;
                return false;
            }

            result = value;
            return true;
        }

        public static bool TryGetSimpleValue<T>(this IExpressValueType ifcValue, out T result) where T : struct
        {
            var targetType = typeof(T);

            //handle null value if is it acceptable
            if (ifcValue == null || ifcValue.Value == null)
            {
                result = default;
                //return true if null is acceptable value
                return targetType.IsClass ||
                       (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>));
            }

            if (targetType == typeof(string))
            {
                result = (T)(object)ifcValue.ToString();
                return true;
            }

            if (targetType == typeof(float) || targetType == typeof(float?) || targetType == typeof(double) || targetType == typeof(double?))
            {
                try
                {
                    result = (T)(object)Convert.ToDouble(ifcValue.Value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException || ex is ArgumentNullException || ex is FormatException || ex is OverflowException)
                    {
                        if (typeof(T) == typeof(float?) ||
                        typeof(T) == typeof(double?))
                        {
                            result = default;
                            return true;
                        }
                    }
                }
                
                result = default;
                return false;
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                try
                {
                    result = (T)(object)Convert.ToBoolean(ifcValue.Value);
                    return true;
                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException || ex is ArgumentNullException)
                    {
                        if (typeof(T) == typeof(bool?))
                        {
                            result = default;
                            return true;
                        }
                    }

                    if (ex is FormatException) Report.Warn("Boolean Conversion: String does not consist of an " + "optional sign followed by a series of digits.");
                    if (ex is OverflowException) Report.Warn("Boolean Conversion: Overflow in string to int conversion.");
                }
                
                result = default;
                return false;
            }

            if (targetType == typeof(int) || targetType == typeof(int?) || targetType == typeof(long) || targetType == typeof(long?))
            {
                try
                {
                    result = (T)(object)Convert.ToInt32(ifcValue.Value);
                    return true;
                }
                catch (Exception ex) {
                    if (ex is NullReferenceException || ex is ArgumentNullException) { 

                        if (targetType == typeof(int?) || targetType == typeof(long?))
                        {
                            result = default;
                            return true;
                        }
                    }
                }

                result = default;
                return false;
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                try
                {
                    result = (T)(object)Convert.ToDateTime(ifcValue.Value);
                    return true;
                }
                catch (Exception)
                {
                    result = default;
                    return targetType == typeof(DateTime?);
                }
            }

            result = default;
            return false;
        }

        public static bool TryGetSimpleValue<T>(this IIfcPhysicalQuantity ifcQuantity, out T result) where T : struct
        {
            if (ifcQuantity is IIfcQuantityLength ifcQuantityLength)
                return TryGetSimpleValue(ifcQuantityLength.LengthValue, out result);

            if (ifcQuantity is IIfcQuantityArea ifcQuantityArea)
                return TryGetSimpleValue(ifcQuantityArea.AreaValue, out result);

            if (ifcQuantity is IIfcQuantityVolume ifcQuantityVolume)
                return TryGetSimpleValue(ifcQuantityVolume.VolumeValue, out result);

            if (ifcQuantity is IIfcQuantityCount ifcQuantityCount)
                return TryGetSimpleValue(ifcQuantityCount.CountValue, out result);

            if (ifcQuantity is IIfcQuantityWeight ifcQuantityWeight)
                return TryGetSimpleValue(ifcQuantityWeight.WeightValue, out result);

            if (ifcQuantity is IIfcQuantityTime ifcQuantityTime)
                return TryGetSimpleValue(ifcQuantityTime.TimeValue, out result);
            
            if (ifcQuantity is IIfcPhysicalComplexQuantity)
            {
                Report.Warn("Complex Types are not supported!");
            }
            result = default;
            return false;
        }

        public static bool TryGetSimpleValue<T>(this IIfcPropertySingleValue property, out T result) where T : struct
        {
            var isValid = property.NominalValue.TryGetSimpleValue(out T res);
            result = res;

            return isValid;
        }

        #endregion

        public static Dictionary<string, string> DistinctDictionaryFromPropertiesString(IEnumerable<IIfcPropertySingleValue> input)
            => input.ToDictionaryDistinct(x => x.Name.ToString(), x => x.NominalValue.ToString(), (x, w) => true);

        public static Dictionary<string, IIfcPropertySingleValue> DistinctDictionaryFromPropertiesValues(IEnumerable<IIfcPropertySingleValue> input)
            => input.ToDictionaryDistinct(x => x.Name.ToString(), x => x, (x, w) => true);

        public static T TryGetProperty<T>(this Dictionary<string, IIfcPropertySingleValue> dict, string input) where T : struct
        {
            if (dict.TryGetValue(input, out var value))
            {
                if (value.TryGetSimpleValue(out T result)) return result;
            }

            return default;
        }

        public static Dictionary<string, T> DistinctDictionaryFromPropertiesOfType<T>(IEnumerable<IIfcPropertySingleValue> input) where T : struct
        {
            return input
                .Select(x => {
                    var valid = x.TryGetSimpleValue(out T result);
                    return Tuple.Create(x.Name.ToString(), valid, result);
                })
                .Where(t => t.Item2)
                .ToDictionaryDistinct(t => t.Item1, t => t.Item3, (x,w) => true);
        }

        public static Dictionary<string, string> GetPropertiesDict(this IIfcObject o, string propertySetName = null)
            => DistinctDictionaryFromPropertiesString((propertySetName == null) ? o.GetProperties() : o.GetProperties(propertySetName));

        public static Dictionary<string, string> GetSharedPropertiesDict(this IIfcObject o)
            => DistinctDictionaryFromPropertiesString(GetPropertiesFromType(o));

        public static Dictionary<string, string> GetAllPropertiesDict(this IIfcObject o) 
            => DistinctDictionaryFromPropertiesString(o.GetAllProperties());

        [Obsolete]
        public static Dictionary<string, string> GetHilitePropertiesDict(this IIfcObject o)
            => o.GetPropertiesDict("Hilite");


        public static IfcPropertySet CreatePropertySet(this IModel model, string setName, Dictionary<string, object> parameters)
        {
            // supports the following types: https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcmeasureresource/lexical/ifcvalue.htm
            var set = model.New<IfcPropertySet>(pset => {
                pset.Name = setName;
                pset.HasProperties.AddRange(
                    parameters.Select(x => model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = x.Key;
                        p.NominalValue = x.Value switch
                        {
                            double d => new IfcReal(d), //double lum => new IfcLuminousFluxMeasure(lum),
                            float r => new IfcReal(r),
                            int i => new IfcInteger(i),
                            bool b => new IfcBoolean(b),
                            _ => new IfcText(x.Value.ToString()),
                        };
                    }))
                );
            });

            return set;
        }

        public static IfcPropertySet CreateAttachPropertySet(this IfcObject o, string setName, Dictionary<string, object> parameters)
        {
            var set = o.Model.CreatePropertySet(setName, parameters);

            o.AddPropertySet(set);

            return set;
        }


        public static void PurgePropertySingleValue(this IfcObject o, string pSetName, string propertyName)
        {
            // removes property and in case of single-propset resulting empty set globally
            // CAUTION: affects all objects re-using it

            IIfcPropertySet propertySet = o.GetPropertySet(pSetName);
            if (propertySet != null)
            {
                IIfcPropertySingleValue ifcPropertySingleValue = propertySet.HasProperties.FirstOrDefault((IIfcPropertySingleValue p) => p.Name == (IfcIdentifier)propertyName);
                if (ifcPropertySingleValue != null)
                {
                    propertySet.HasProperties.Remove(ifcPropertySingleValue);

                    // delete property from model
                    o.Model.Delete(ifcPropertySingleValue);
                }

                if (propertySet.HasProperties.IsEmpty())
                {
                    // remove reference of empty set and object
                    var rel = o.IsDefinedBy.FirstOrDefault((IfcRelDefinesByProperties r) => r.RelatingPropertyDefinition.PropertySetDefinitions.FirstOrDefault(a => a.Name == pSetName) != null);
                    if (rel != null) o.Model.Delete(rel);

                    // remove empty set from model
                    o.Model.Delete(propertySet);
                }
            }
        }

        public static void PurgePropertySet(this IfcObject o, string pSetName)
        {
            // removes set and all its properties globally
            // CAUTION: affects all objects re-using it

            IIfcPropertySet propertySet = o.GetPropertySet(pSetName);
            if (propertySet != null)
            {
                // stash for later removal
                var temp = propertySet.HasProperties.ToArray();

                // release properties from set
                propertySet.HasProperties.Clear();

                // detlete properties from model
                temp.ForEach(o.Model.Delete);

                // remove reference of empty set and object
                var rel = o.IsDefinedBy.FirstOrDefault((IfcRelDefinesByProperties r) => r.RelatingPropertyDefinition.PropertySetDefinitions.FirstOrDefault(a => a.Name == pSetName) != null);
                if (rel != null) o.Model.Delete(rel);

                // remove empty set from model
                o.Model.Delete(propertySet);
            }
        }

        #endregion

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

        #region Hierarchy

        public static IFCNode CreateHierarchy(IfcStore model)
            => CreateHierarchy(model.GetProject());

        private static IFCNode CreateHierarchy(IIfcObjectDefinition obj)
            => new IFCNode(obj, obj.GetChildren().Select(x => (IIFCNode)CreateHierarchy(x)).ToList());

        public static void PrintHierarchy(string file)
        {
            using var model = IfcStore.Open(file);
            var project = model.Instances.FirstOrDefault<IIfcProject>();
            Report.Line("HIRARCHY of file: {0}\n", file);
            PrintHierarchy(project, 0);
        }

        public static void PrintHierarchy(IIfcObjectDefinition o, int level)
        {
            Report.Line(string.Format("{0}{1} [{2}]", GetIndent(level), o.Name, o.GetType().Name));

            var parent = o.GetParent();
            if (parent != null) Report.Line("parent: " + parent.ToString());

            var children = GetChildren(o);
            Report.Line("children count: {0}", children.Count());
            children.ForEach(element => Report.Line(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name)));

            var siblings = o.GetSiblings();
            Report.Line("sibling count: {0}", siblings.Count());
            siblings.ForEach(s => Report.Line("siblings: "+s.ToString()));

            Report.Line();
            
            ////only spatial elements can contain building elements
            //var spatialElement = o as IIfcSpatialStructureElement;
            //if (spatialElement != null)
            //{
            //    //using IfcRelContainedInSpatialElement to get contained elements
            //    var containedElements = spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
            //    foreach (var element in containedElements)
            //        Console.WriteLine(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name));
            //}

            //using IfcRelAggregares to get spatial decomposition of spatial structure elements
            foreach (var item in o.IsDecomposedBy.SelectMany(r => r.RelatedObjects))
                PrintHierarchy(item, level + 1);
        }

        private static string GetIndent(int level)
        {
            var indent = "";
            for (int i = 0; i < level; i++)
                indent += "  ";
            return indent;
        }

        #endregion

        #region Group

        public static IfcGroup CreateGroup(this IModel model, string groupName)
            => model.New<IfcGroup>(g => g.Name = groupName);

        public static IfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IfcObjectDefinition> relatedObjects, IfcObjectTypeEnum groupType = IfcObjectTypeEnum.PRODUCT)
        {
            var group = model.CreateGroup(groupName);

            // Link related objects to group via IfcRelAssignsToGroup
            model.New<IfcRelAssignsToGroup>(rel => {
                rel.RelatingGroup = group;
                rel.RelatedObjects.AddRange(relatedObjects);
                rel.RelatedObjectsType = groupType;
            });

            return group;
        }

        public static IfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IfcGroup> relatedObjects)
            => model.CreateGroup(groupName, relatedObjects, IfcObjectTypeEnum.GROUP);

        #endregion

        #region Layer

        public static IfcPresentationLayerAssignment CreateLayer(this IModel model, string layerName, IEnumerable<IfcLayeredItem> items = null)
        {
            // IfcPresentationLayerAssignment only allows: IFC4.IFCSHAPEREPRESENTATION", "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM
            return model.New<IfcPresentationLayerAssignment>(layer => {
                layer.Name = layerName;
                if (!items.IsEmptyOrNull()) layer.AssignedItems.AddRange(items);
            });
        }

        public static IfcPresentationLayerAssignment CreateLayer(this IModel model, string layerName, IfcLayeredItem items)
            => model.CreateLayer(layerName, [items]);

        public static IfcPresentationLayerWithStyle CreateLayerWithStyle(this IModel model, string layerName, IEnumerable<IfcPresentationStyle> styles, bool layerVisibility = true, IEnumerable<IfcGeometricRepresentationItem> items = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationorganizationresource/lexical/ifcpresentationlayerwithstyle.htm
            // IfcPresentationLayerWithStyle only allows: "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM"

            return model.New<IfcPresentationLayerWithStyle>(layer => {
                layer.Name = layerName;

                // Visibility Control
                layer.LayerOn = layerVisibility; // visibility control allows to define a layer to be either 'on' or 'off', and/or 'frozen' or 'not frozen'
                //layer.LayerFrozen = true;

                // Access control
                //layer.LayerBlocked = true;    // access control allows to block graphical entities from manipulations

                // NOTE: ORDER seems to be important! BIM-Viewer tend to use only color information of first item!
                layer.LayerStyles.AddRange(styles);
                if (items != null && !items.IsEmpty()) layer.AssignedItems.AddRange(items);
            });
        }

        public static IfcPresentationLayerWithStyle CreateLayerWithStyle(this IModel model, string layerName, IEnumerable<IfcPresentationStyle> styles, IfcGeometricRepresentationItem item, bool layerVisibility = true)
            => model.CreateLayerWithStyle(layerName, styles, layerVisibility, [item]);

        public static IfcLayeredItem AssignLayer(this IfcLayeredItem item, IfcPresentationLayerAssignment layer)
        {
            if (layer is IfcPresentationLayerWithStyle && item is IfcShapeRepresentation)
            {
                // IfcPresentationLayerWithStyle only allows "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM"
                throw new ArgumentException("IfcShapeRepresentation cannot be assigened to IfcPresentationLayerWithStyle");
            }
            // IfcPresentationLayerAssignment only allows: IFC4.IFCSHAPEREPRESENTATION", "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }

        public static IfcGeometricRepresentationItem AssignLayer(this IfcGeometricRepresentationItem item, IfcPresentationLayerAssignment layer)
        {
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }

        public static IfcMappedItem AssignLayer(this IfcMappedItem item, IfcPresentationLayerAssignment layer)
        {
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }

        #endregion

        #region Placement

        public static IfcCartesianPoint CreatePoint(this IModel model, V2d point)
            => model.New<IfcCartesianPoint>(c => c.Set(point));

        public static IfcCartesianPoint CreatePoint(this IModel model, V3d point)
            => model.New<IfcCartesianPoint>(c => c.Set(point));

        public static IfcDirection CreateDirection(this IModel model, V2d direction)
            => model.New<IfcDirection>(rd => rd.Set(direction)); // NOTE: Direction may be normalized!

        public static IfcDirection CreateDirection(this IModel model, V3d direction)
            => model.New<IfcDirection>(rd => rd.Set(direction)); // NOTE: Direction may be normalized!

        public static IfcVector CreateVector(this IModel model, V3d vector)
        {
            return model.New<IfcVector>(v => {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }

        public static IfcVector CreateVector(this IModel model, V2d vector)
        {
            return model.New<IfcVector>(v => {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }

        public static IfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location, V3d refDir, V3d axis)
        {
            return model.New<IfcAxis2Placement3D>(a => {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir);
                a.Axis = model.CreateDirection(axis);
            });
        }
        public static IfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location)
            => model.CreateAxis2Placement3D(location, V3d.XAxis, V3d.ZAxis);

        public static IfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location, V2d refDir)
        {
            return model.New<IfcAxis2Placement2D>(a => {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir);
            });
        }
        public static IfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location)
            => model.CreateAxis2Placement2D(location, V2d.XAxis);

        public static IfcLocalPlacement CreateLocalPlacement(this IModel model, V3d shift)
            => model.New<IfcLocalPlacement>(p => p.RelativePlacement = model.CreateAxis2Placement3D(shift));

        #endregion

        #region Grid
        public static IfcGridAxis CreateGridAxis(this IModel model, string name, V2d start, V2d end)
        {
            return model.New<IfcGridAxis>(a =>
            {
                a.AxisTag = name;
                a.AxisCurve = model.CreatePolyLine(start, end);
                a.SameSense = true;
            });
        }

        public static IfcGrid CreateGrid(this IModel model, string name, string[] xAxis, string[] vAxes, double offset)
        {
            // Create axis
            var xAxisEntities = xAxis.Select((a, i) => model.CreateGridAxis(a, new V2d(-offset / 2.0, offset * i), new V2d(offset * vAxes.Length, offset * i)));
            var yAxisEntities = vAxes.Select((a, i) => model.CreateGridAxis(a, new V2d(offset * i, -offset / 2.0), new V2d(offset * i, offset * xAxis.Length)));

            // Create regular grid
            var grid = model.New<IfcGrid>(g =>
            {
                g.Name = name;
                g.UAxes.AddRange(xAxisEntities);
                g.VAxes.AddRange(yAxisEntities);
                g.PredefinedType = IfcGridTypeEnum.RECTANGULAR;
                g.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);
            });

            return grid;
        }

        #endregion

        #region Styling
        public static IfcColourRgb CreateColor(this IModel model, C3f colour)
            => model.New<IfcColourRgb>(x => x.Set(colour));

        public static IfcColourRgb CreateColor(this IModel model, C3d colour)
            => model.New<IfcColourRgb>(x => x.Set(colour));

        #region Text Styling
        public static IfcTextStyleForDefinedFont CreateTextStyleForDefinedFont(this IModel model, C3f colour, C3f background, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstylefordefinedfont.htm

            return model.New<IfcTextStyleForDefinedFont>(f => {
                f.Colour = model.CreateColor(colour);
                // optional
                f.BackgroundColour = model.CreateColor(background);
            });
        }

        public enum TextDecoration { None, UnderLine, Overline, Linethrough }
        public enum TextTransform { Capitalize, Uppercase, Lowercase, None }
        public enum TextAlignment { Left, Right, Center, Justify }

        public static IfcTextStyleTextModel CreateTextStyleTextModel(this IModel model, double textIndent, TextAlignment textAlign, TextDecoration textDecoration, TextTransform textTransform, double letterSpacing, double wordSpacing, double lineHeight)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstyletextmodel.htm

            string decoration = textDecoration switch
            {
                TextDecoration.UnderLine => "underLine",
                TextDecoration.Overline => "overline",
                TextDecoration.Linethrough => "line-through",
                _ => "none"
            };

            string transform = textTransform switch
            {
                TextTransform.Capitalize => "capitalize",
                TextTransform.Uppercase => "uppercase",
                TextTransform.Lowercase => "lowercase",
                _ => "none"
            };

            string alignment = textAlign switch
            {
                TextAlignment.Left => "left",
                TextAlignment.Right => "right",
                TextAlignment.Center => "center",
                _ => "justify"
            };

            return model.New<IfcTextStyleTextModel>(tm =>
            {
                // optional
                tm.TextIndent = new IfcLengthMeasure(textIndent);           // The property specifies the indentation that appears before the first formatted line.
                tm.TextAlign = new IfcTextAlignment(alignment);
                tm.TextDecoration = new IfcTextDecoration(decoration);
                tm.TextTransform = new IfcTextTransformation(transform);
                tm.LetterSpacing = new IfcLengthMeasure(letterSpacing);
                tm.WordSpacing = new IfcLengthMeasure(wordSpacing);
                tm.LineHeight = new IfcLengthMeasure(lineHeight);
            });
        }

        public enum FontStyle { Normal, Italic, Oblique }
        public enum FontWeight { Normal, Bold }
        public enum FontVariant { Normal, Smallcaps }
        public static IfcTextStyleFontModel CreateTextStyleFontModel(this IModel model, double fontSize, string fontFamily, string fontModelName, FontStyle fontStyle = FontStyle.Normal, FontWeight fontWeight = FontWeight.Normal, FontVariant fontVariant = FontVariant.Normal)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstylefontmodel.htm

            string style = fontStyle switch
            {
                FontStyle.Normal => "normal",
                FontStyle.Italic => "italic",
                _ => "oblique"
            };

            string weight = fontWeight switch
            {
                FontWeight.Normal => "400",
                _ => "700",
            };

            string variant = fontVariant switch
            {
                FontVariant.Normal => "normal",
                _ => "small-caps",
            };

            return model.New<IfcTextStyleFontModel>(f =>
            {
                f.Name = fontModelName;
                f.FontSize = new IfcLengthMeasure(fontSize);
                f.FontFamily.Add(new IfcTextFontName(fontFamily)); // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextfontname.htm
                // optional
                f.FontStyle = new IfcFontStyle(style);
                f.FontWeight = new IfcFontWeight(weight);
                f.FontVariant = new IfcFontVariant(variant);
            });
        }

        public static IfcTextStyle CreateTextStyle(this IModel model, double fontSize, C3f colour, C3f background, string fontModelName, string fontFamily = "serif", bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstyle.htm

            return model.New<IfcTextStyle>(ts =>
            {
                ts.ModelOrDraughting = modelOrDrauting;

                if (name != null) ts.Name = name;

                ts.TextFontStyle = model.CreateTextStyleFontModel(fontSize, fontFamily, fontModelName);

                // optional
                ts.TextCharacterAppearance = model.CreateTextStyleForDefinedFont(colour, background);
                ts.TextStyle = model.CreateTextStyleTextModel(10, TextAlignment.Right, TextDecoration.None, TextTransform.None, 10, 10, 20);
            });
        }

        #endregion

        #region Surface Styling
        public static IfcSurfaceStyleShading CreateSurfaceStyleShading(this IModel model, C3d surface, double transparency = 0.0)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestyleshading.htm

            return model.New<IfcSurfaceStyleShading>(l =>
            {
                l.SurfaceColour = model.CreateColor(surface);
                l.Transparency = new IfcNormalisedRatioMeasure(transparency); // [0 = opaque .. 1 = transparent]
            });
        }

        public static IfcSurfaceStyleRendering CreateSurfaceStyleRendering(this IModel model, C3d surface, double transparency, C3d diffuse, C3d diffuseTransmission, C3d transmission, C3d specular, double specularHighlight, C3d reflection, IfcReflectanceMethodEnum reflectionType)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylerendering.htm

            return model.New<IfcSurfaceStyleRendering>(l =>
            {
                l.SurfaceColour = model.CreateColor(surface);
                l.Transparency = new IfcNormalisedRatioMeasure(transparency); // [0 = opaque .. 1 = transparent]
                l.DiffuseColour = model.CreateColor(diffuse);

                l.TransmissionColour = model.CreateColor(transmission);
                l.DiffuseTransmissionColour = model.CreateColor(diffuseTransmission);

                l.ReflectionColour = model.CreateColor(reflection);
                l.ReflectanceMethod = reflectionType;

                // The IfcSpecularExponent defines the datatype for exponent determining the sharpness of the 'reflection'.
                // The reflection is made sharper with large values of the exponent, such as 10.0.
                // Small values, such as 1.0, decrease the specular fall - off.
                // IfcSpecularExponent is of type REAL.
                l.SpecularHighlight = new IfcSpecularExponent(specularHighlight);
                l.SpecularColour = model.CreateColor(specular);
            });
        }

        public static IfcSurfaceStyleLighting CreateSurfaceStyleLighting(this IModel model, C3d diffuseTransmission, C3d diffuseReflection, C3d transmission, C3d reflectance)
        {
            // https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylelighting.htm

            return model.New<IfcSurfaceStyleLighting>(l =>
            {
                l.DiffuseTransmissionColour = model.CreateColor(diffuseTransmission);
                l.DiffuseReflectionColour = model.CreateColor(diffuseReflection);
                l.TransmissionColour = model.CreateColor(transmission);
                l.ReflectanceColour = model.CreateColor(reflectance);
            });
        }

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, IEnumerable<IfcSurfaceStyleElementSelect> styles, string name = null)
        {
            // IfcSurfaceStyle is an assignment of one or many surface style elements to a surface, defined by subtypes of
            //     IfcSurface, IfcFaceBasedSurfaceModel, IfcShellBasedSurfaceModel, or by subtypes of IfcSolidModel. 
            // The positive direction of the surface normal relates to the positive side. In case of solids the outside of the solid is to be taken as positive side.

            return model.New<IfcSurfaceStyle>(style =>
            {
                if (name != null) style.Name = name;
                style.Side = IfcSurfaceSide.BOTH;
                style.Styles.AddRange(styles); // [1:5] [IfcSurfaceStyleShading -> IfcSurfaceStyleRendering | IfcSurfaceStyleLighting | IfcSurfaceStyleWithTextures | IfcExternallyDefinedSurfaceStyle | IfcSurfaceStyleRefraction
            });
        }

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, IfcSurfaceStyleElementSelect style, string name = null)
            => CreateSurfaceStyle(model, [style], name);

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, C3d surface, double transparency = 0.0, string name = null)
            => CreateSurfaceStyle(model, model.CreateSurfaceStyleShading(surface, transparency), name);

        #endregion

        #region Curve Styling
        public static IfcCurveStyle CreateCurveStyle(this IModel model, C3d color, double width, double visibleLengh = 0, double invisibleLength = 0, bool modelOrDraughting = true)
        {
            return model.New<IfcCurveStyle>(c =>
            {
                c.ModelOrDraughting = modelOrDraughting;
                c.CurveColour = model.CreateColor(color);
                c.CurveWidth = new IfcPositiveLengthMeasure(width);
                if (visibleLengh > 0)
                {
                    c.CurveFont = model.New<IfcCurveStyleFont>(f =>
                        f.PatternList.Add(model.New<IfcCurveStyleFontPattern>(p =>
                        {
                            p.VisibleSegmentLength = visibleLengh;
                            if (invisibleLength > 0) p.InvisibleSegmentLength = invisibleLength;
                        })
                    ));
                }
            });
        }
        #endregion

        #region Area Styling
        public static IfcFillAreaStyleHatching CreateFillAreaStyleHatching(this IModel model, double angle, double startOfNextHatchLine, IfcCurveStyle curveStyle)
        {
            return model.New<IfcFillAreaStyleHatching>(h =>
            {
                h.HatchLineAppearance = curveStyle;
                h.HatchLineAngle = new IfcPlaneAngleMeasure(angle);
                h.StartOfNextHatchLine = new IfcPositiveLengthMeasure(startOfNextHatchLine);
            });
        }

        public static IfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d backgroundColor, bool modelOrDrauting = true, string name = null)
        {
            // NOTE: Color information of surfaces for rendering is assigned by using IfcSurfaceStyle, not by using IfcFillAreaStyle. 
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            return model.New<IfcFillAreaStyle>(a =>
            {
                if (name != null) a.Name = name;
                a.ModelorDraughting = modelOrDrauting;
                // Solid fill for areas and surfaces by only assigning IfcColour to the set of FillStyles. It then provides the background colour for the filled area or surface.
                a.FillStyles.Add(model.CreateColor(backgroundColor));
            });
        }

        public static IfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d hatchingColour, double angle, double startOfNextHatchLine, IfcCurveStyle curveStyle, bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            // 
            return model.New<IfcFillAreaStyle>(a =>
            {
                if (name != null) a.Name = name;
                a.ModelorDraughting = modelOrDrauting;
                // Solid fill for areas and surfaces by only assigning IfcColour to the set of FillStyles. It then provides the background colour for the filled area or surface.
                a.FillStyles.AddRange([
                    model.CreateColor(hatchingColour),
                    model.CreateFillAreaStyleHatching(angle, startOfNextHatchLine, curveStyle)
                ]);
            });
        }
        #endregion

        #region Style Item
        public static IfcStyledItem CreateStyleItem(this IfcRepresentationItem item, IEnumerable<IfcPresentationStyle> styles)
        {
            // Each subtype of IfcPresentationStyle is assigned to the IfcGeometricRepresentationItem's through an intermediate IfcStyledItem.
            return item.Model.New<IfcStyledItem>(styleItem => {
                styleItem.Styles.AddRange(styles);
                if (item != null) styleItem.Item = item;
            });
        }

        public static IfcStyledItem CreateStyleItem(this IfcRepresentationItem item, IfcPresentationStyle style)
            => CreateStyleItem(item, [style]);

        #endregion

        #endregion

        #region Material

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcMaterial mat)
        {
            return mat.HasProperties
                    .SelectMany(mp => mp.Properties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static Dictionary<string, IIfcPropertySingleValue> GetPropertiesDict(this IIfcMaterial mat) => DistinctDictionaryFromPropertiesValues(mat.GetProperties());

        public static IfcMaterialProperties CreateAttachPsetMaterialCommon(this IfcMaterial material, double molecularWeight, double porosity, double massDensity)
        {
            return material.Model.New<IfcMaterialProperties>(ps => {
                ps.Name = "Pset_MaterialCommon";
                ps.Material = material;
                ps.Properties.AddRange([
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "MolecularWeight";
                        p.NominalValue = new IfcMolecularWeightMeasure(molecularWeight);
                    }),
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Porosity";
                        p.NominalValue = new IfcNormalisedRatioMeasure(porosity);
                    }),
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "MassDensity";
                        p.NominalValue = new IfcMassDensityMeasure(massDensity);
                    })
                ]);
            });
        }

        public static IfcMaterialProperties CreateAttachPsetMaterialThermal(this IfcMaterial material, double thermalConductivity, double specificHeatCapacity, double boilingPoint = 100.0, double freezingPoint = 0.0)
        {
            return material.Model.New<IfcMaterialProperties>(ps => {
                ps.Name = "Pset_MaterialThermal";
                ps.Material = material;
                ps.Properties.AddRange([
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "ThermalConductivity";
                        p.NominalValue = new IfcThermalConductivityMeasure(thermalConductivity);
                    }),
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "SpecificHeatCapacity";
                        p.NominalValue = new IfcSpecificHeatCapacityMeasure(specificHeatCapacity);
                    }),
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "BoilingPoint";
                        p.NominalValue = new IfcThermodynamicTemperatureMeasure(boilingPoint);
                    }),
                    material.Model.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "FreezingPoint";
                        p.NominalValue = new IfcThermodynamicTemperatureMeasure(freezingPoint);
                    })
                ]);
            });
        }
        public static IfcMaterialDefinitionRepresentation CreateAttachPresentation(this IfcMaterial material, C3d surfaceColor)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcrepresentationresource/lexical/ifcmaterialdefinitionrepresentation.htm

            var model = material.Model;

            return model.New<IfcMaterialDefinitionRepresentation>(def =>
            {
                def.RepresentedMaterial = material;
                def.Representations.Add(model.New<IfcStyledRepresentation>(rep => {
                    rep.ContextOfItems = model.GetGeometricRepresentationContextModel();
                    rep.Items.Add(model.New<IfcStyledItem>(styleItem => {
                        //styleItem.Item -> NOTE: If the IfcStyledItem is used within a reference from an IfcMaterialDefinitionRepresentation then no Item shall be provided.
                        styleItem.Styles.Add(model.CreateSurfaceStyle(surfaceColor));
                    }));
                }));
            });
        }
        #endregion

        #region IfcGeometricRepresentationItem

        public static IfcGeometricRepresentationContext GetGeometricRepresentationContextPlan(this IModel model)
            => model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => c.ContextType == "Plan").First();

        public static IfcGeometricRepresentationContext GetGeometricRepresentationContextModel(this IModel model)
            => model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => c.ContextType == "Model").First();

        #region Lines

        public static IfcCartesianPointList2D CreateCartesianPointList2D(this IModel model, params V2d[] points)
        {
            return model.New<IfcCartesianPointList2D>(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IfcCartesianPointList3D CreateCartesianPointList3D(this IModel model, params V3d[] points)
        {
            return model.New<IfcCartesianPointList3D>(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IfcLine CreateLine(this IModel model, V2d start, V2d end)
        {
            var diff = end - start;

            return model.New<IfcLine>(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IfcLine CreateLine(this IModel model, V3d start, V3d end)
        {
            var diff = end - start;

            return model.New<IfcLine>(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IfcLine CreateLine(this IModel model, Line2d line)
            => model.CreateLine(line.P0, line.P1);

        public static IfcLine CreateLine(this IModel model, Line3d line)
            => model.CreateLine(line.P0, line.P1);

        public static IfcPolyline CreatePolyLine(this IModel model, params V2d[] points)
        {
            return model.New<IfcPolyline>(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IfcPolyline CreatePolyLine(this IModel model, params V3d[] points)
        {
            return model.New<IfcPolyline>(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IfcPolyline CreatePolyLine(this IModel model, IEnumerable<V2d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IfcPolyline CreatePolyLine(this IModel model, IEnumerable<V3d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IfcIndexedPolyCurve CreateIndexedPolyCurve(this IModel model, IEnumerable<V2d> points, IEnumerable<int[]> indices = null)
        {
            // NOTE: Indices start with 1!
            return model.New<IfcIndexedPolyCurve>(poly =>
            {
                poly.Points = model.CreateCartesianPointList2D(points.ToArray());

                if (indices != null)
                {
                    var index = indices.Select(i => {
                        if (i.Length == 3) return (IfcSegmentIndexSelect)new IfcArcIndex(i.Select(x => new IfcPositiveInteger(x)).ToList());
                        else if (i.Length == 2) return (IfcSegmentIndexSelect)new IfcLineIndex(i.Select(x => new IfcPositiveInteger(x)).ToList());
                        else return null;
                    });
                    poly.Segments.AddRange(index);
                }
            });
        }

        #endregion

        #region Surfaces

        public static IfcPlane CreatePlane(this IModel model, Plane3d plane)
        {
            var refDir = (plane.Normal.MajorDim == 2) ? plane.Normal.ZXY : plane.Normal.YXZ;
            return model.New<IfcPlane>(pl => pl.Position = model.CreateAxis2Placement3D(plane.Point, refDir, plane.Normal));
        }

        public static IfcCurveBoundedPlane CreateCurveBoundedPlane(this IModel model, Plane3d plane, Polygon2d poly)
        {
            return model.New<IfcCurveBoundedPlane>(p =>
            {
                p.BasisSurface = model.CreatePlane(plane);
                p.OuterBoundary = model.CreatePolyLine(poly.Points);
            });
        }

        #endregion

        #region Lights

        // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/lighting-geometry.htm

        public static IfcLightSourceAmbient CreateLightSourceAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceAmbient>(ls => {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;
            });
        }

        public static IfcLightSourceDirectional CreateLightSourceDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceDirectional>(ls => {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Directional properties
                ls.Orientation = model.CreateDirection(direction);
            });
        }

        public readonly struct AngleAndIntensity
        {
            public double AnglesInDegree { get; }
            public double IntentsityInCandelaPerLumen { get; }

            public AngleAndIntensity(double anglesInDegree, double intentsityInCandelaPerLumen)
            {
                AnglesInDegree = anglesInDegree;
                IntentsityInCandelaPerLumen = intentsityInCandelaPerLumen;
            }
        }

        public readonly struct LightIntensityDistributionData
        {
            public double MainAngleInDegree { get; }
            public AngleAndIntensity[] SecondaryAnglesAndIntensities { get; }

            public LightIntensityDistributionData(double angleInDegree, AngleAndIntensity[] data)
            {
                MainAngleInDegree = angleInDegree;
                SecondaryAnglesAndIntensities = data;
            }
        }

        public static IfcLightIntensityDistribution CreateLightIntensityDistribution(this IModel model, IfcLightDistributionCurveEnum distributionEnum, IEnumerable<LightIntensityDistributionData> data)
        {
            return model.New<IfcLightIntensityDistribution>(d =>
            {
                // Type C is the recommended standard system. The C-Plane system equals a globe with a vertical axis. C-Angles are valid from 0° to 360°, γ-Angles are valid from 0° (south pole) to 180° (north pole).
                // Type B is sometimes used for floodlights.The B-Plane System has a horizontal axis.B - Angles are valid from - 180° to + 180° with B 0° at the bottom and B180°/ B - 180° at the top, β - Angles are valid from - 90° to + 90°.
                // Type A is basically not used.For completeness the Type A Photometry equals the Type B rotated 90° around the Z - Axis counter clockwise.
                d.LightDistributionCurve = distributionEnum;
                d.DistributionData.AddRange(data.Select(a =>
                {
                    return model.New<IfcLightDistributionData>(data =>
                    {
                        // The main plane angle (A, B or C angles, according to the light distribution curve chosen).
                        data.MainPlaneAngle = new IfcPlaneAngleMeasure(a.MainAngleInDegree.RadiansFromDegrees()); // measured in radians

                        // The list of secondary plane angles (the α, β or γ angles) according to the light distribution curve chosen.
                        // NOTE: The SecondaryPlaneAngle and LuminousIntensity lists are corresponding lists.
                        data.SecondaryPlaneAngle.AddRange(a.SecondaryAnglesAndIntensities.Select(sa => new IfcPlaneAngleMeasure(sa.AnglesInDegree.RadiansFromDegrees())));

                        // The luminous intensity distribution measure for this pair of main and secondary plane angles according to the light distribution curve chosen.	
                        data.LuminousIntensity.AddRange(a.SecondaryAnglesAndIntensities.Select(m => new IfcLuminousIntensityDistributionMeasure(m.IntentsityInCandelaPerLumen))); // measured in Candela/Lumen (cd/lm) or (cd/klm).
                    });
                }));
            });
        }

        public static IfcLightSourceGoniometric CreateLightSourceGoniometric(this IModel model, C3d color, double colourTemperature, double luminousFlux,
            IfcLightEmissionSourceEnum lightEmissionSource, IfcLightIntensityDistribution data, IfcAxis2Placement3D placement, C3d? appearance = null, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceGoniometric>(ls => {

                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Goniometric Properties
                ls.Position = placement;
                if (appearance.HasValue) ls.ColourAppearance = model.CreateColor(appearance.Value);
                ls.ColourTemperature = new IfcThermodynamicTemperatureMeasure(colourTemperature);
                ls.LuminousFlux = new IfcLuminousFluxMeasure(luminousFlux);

                ls.LightEmissionSource = lightEmissionSource;

                // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/lighting-geometry.htm
                ls.LightDistributionDataSource = data;

            });
        }

        #endregion

        #endregion

        #region IfcShapeRepresentation

        #region SurveyPoints (broken) - IfcShapeRepresentation
        public static IfcShapeRepresentation CreateShapeRepSurveyPoints(this IModel model, IfcPresentationLayerWithStyle layer, params V2d[] points)
        {
            // Set of Survey points 2D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IfcGeometricRepresentationItem item = model.CreateCartesianPointList2D(points).AssignLayer(layer);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V2d[] points)
            => CreateShapeRepSurveyPoints(model, null, points);

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, IfcPresentationLayerWithStyle layer, params V3d[] points)
        {
            // Set of Survey points 3D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IfcGeometricRepresentationItem item = model.CreateCartesianPointList3D(points).AssignLayer(layer);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V3d[] points)
            => CreateShapeRepresentationSurveyPoints(model, null, points);

        #endregion

        #region Annotation - IfcShapeRepresentation
        // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/object-predefined-type.htm
        #region 3D Annotations - IfcShapeRepresentation
        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3d(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "GeometricSet";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dPoint(this IModel model, V3d point, IfcPresentationLayerWithStyle layer = null)
        {
            var content = model.CreatePoint(point);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dCurve(this IModel model, V3d[] points, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem content = model.CreatePolyLine(points);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dCross(this IModel model, V3d origin, V3d normal, double angleInDegree, double scale, IfcPresentationLayerWithStyle layer = null)
        {
            //var crossPoints = new V3d[] {
            //    origin, origin+(axis1.Normalized * scale),
            //    origin, origin-(axis1.Normalized * scale),
            //    origin, origin+(axis2.Normalized * scale),
            //    origin, origin-(axis2.Normalized * scale),
            //};
            var plane = new Plane3d(normal, 0.0);
            var d = new Rot2d(angleInDegree.RadiansFromDegrees()) * V2d.XAxis * scale;

            var crossPoints = new V3d[] {
                origin, origin+plane.Unproject( d),         //+dir.XYO,
                origin, origin+plane.Unproject(-d),         //-dir.XYO,
                origin, origin+plane.Unproject( d.YX * new V2d(-1,1)),      //+dir.YXO * new V3d(-1,1,0),
                origin, origin+plane.Unproject( d.YX * new V2d(1,-1)),      //+dir.YXO * new V3d(1,-1,0)
            };

            IfcGeometricRepresentationItem content = model.CreatePolyLine(crossPoints);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dSurface(this IModel model, Plane3d plane, Polygon2d poly, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem content = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }
        #endregion

        #region 2D Annotations - IfcShapeRepresentation
        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2D(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Annotation2D";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dPoint(this IModel model, V2d point, IfcPresentationLayerWithStyle layer = null)
        {
            var item = model.CreatePoint(point);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dCurve(this IModel model, V2d[] points, IEnumerable<int[]> indices = null, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateIndexedPolyCurve(points, indices);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dText(this IModel model, string label, V2d position, IfcPresentationLayerWithStyle layer = null)
        {
            // ONLY visible in "BIMVISION"
            IfcGeometricRepresentationItem item = model.New<IfcTextLiteral>(l =>
            {
                // https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD1/HTML/schema/ifcpresentationdefinitionresource/lexical/ifctextliteral.htm
                l.Path = IfcTextPath.RIGHT;
                l.Literal = label;
                //// Attributes for <IfcTextLiteralWithExtent>
                //l.BoxAlignment = new IfcBoxAlignment("center");
                //l.Extent = model.New<IfcPlanarExtent>(e =>
                //{
                //    e.SizeInX = new IfcLengthMeasure(300.0);
                //    e.SizeInY = new IfcLengthMeasure(200.0);
                //});
                l.Placement = model.CreateAxis2Placement2D(position);
            });
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dArea(this IModel model, Box2d rect, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem item = model.New<IfcAnnotationFillArea>(l =>
            {
                l.OuterBoundary = model.CreateIndexedPolyCurve(rect.ComputeCornersCCW());
                l.InnerBoundaries.Add(model.CreateIndexedPolyCurve(rect.ShrunkBy(new V2d(0.3)).ComputeCornersCCW()));
            });
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        #endregion

        #endregion

        public static IfcShapeRepresentation CreateShapeRepresentationLighting(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Lighting";
                r.RepresentationType = "LightSource";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null, IfcPresentationLayerAssignment layer = null)
        {

            IfcGeometricRepresentationItem item = model.CreateLightSourceAmbient(color, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateLightSourceDirectional(color, direction, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingGoniemtric(this IModel model, C3d color, V3d location, double colourTemperature, double luminousFlux, IfcLightIntensityDistribution distribution, IfcPresentationLayerAssignment layer = null)
        {
            var placement = model.CreateAxis2Placement3D(location);
            IfcGeometricRepresentationItem item = model.CreateLightSourceGoniometric(color, colourTemperature, luminousFlux, IfcLightEmissionSourceEnum.NOTDEFINED, distribution, placement);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationBoundingBox(this IModel model, Box3d box, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.New<IfcBoundingBox>(b => b.Set(box));
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Box";
                r.RepresentationType = "BoundingBox";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSolidBox(this IModel model, Box3d box, IfcPresentationLayerAssignment layer = null)
        {
            // Box creation by extruding box-base along Z-Axis
            var rectProf = model.New<IfcRectangleProfileDef>(p =>
            {
                p.ProfileName = "RectArea";
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.XDim = box.SizeX;
                p.YDim = box.SizeY;
                //p.Position = model.CreateAxis2Placement2D(V2d.Zero);
            });

            IfcGeometricRepresentationItem item = model.New<IfcExtrudedAreaSolid>(solid =>
            {
                solid.Position = model.CreateAxis2Placement3D(box.Min);
                solid.Depth = box.SizeZ;
                solid.ExtrudedDirection = model.CreateDirection(V3d.ZAxis);
                solid.SweptArea = rectProf;
            });
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSurface(this IModel model, Plane3d plane, Polygon2d poly, IfcPresentationLayerAssignment layer = null)
        {
            if (!poly.IsCcw())
            {
                poly.Reverse();
            }

            IfcGeometricRepresentationItem item = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(item);

            // Box creation by extruding box-base along Z-Axis
            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Surface";
                r.RepresentationType = "Surface3D";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        private static IfcTriangulatedFaceSet CreateTriangulatedFaceSet(IModel model, PolyMesh inputMesh)
        {
            var triangleMesh = inputMesh.TriangulatedCopy();

            return model.New<IfcTriangulatedFaceSet>(tfs =>
            {
                tfs.Closed = true;
                tfs.Coordinates = model.CreateCartesianPointList3D(triangleMesh.PositionArray);

                for (int i = 0; i < triangleMesh.FirstIndexArray.Length - 1; i++)
                {
                    var firstIndex = triangleMesh.FirstIndexArray[i];
                    var values = new long[3].SetByIndex(x => triangleMesh.VertexIndexArray[firstIndex + x]).Select(v => new IfcPositiveInteger(v + 1));   // CAUTION! Indices are 1 based in IFC!
                    tfs.CoordIndex.GetAt(i).AddRange(values);
                }
            });
        }

        private static IfcPolygonalFaceSet CreatePolygonalFaceSet(IModel model, PolyMesh inputMesh)
        {
            // only available in IFC4
            if (model.SchemaVersion != XbimSchemaVersion.Ifc4)
                return null;

            var faces = new List<IfcIndexedPolygonalFace>(inputMesh.Faces.Count());

            foreach (var face in inputMesh.Faces)
            {
                faces.Add(model.New<IfcIndexedPolygonalFace>(f => f.CoordIndex.AddRange(face.VertexIndices.Select(v => new IfcPositiveInteger(v + 1))))); // CAUTION! Indices are 1 based in IFC!
            }

            return model.New<IfcPolygonalFaceSet>(p =>
            {
                p.Closed = true;
                p.Coordinates = model.CreateCartesianPointList3D(inputMesh.PositionArray);
                p.Faces.AddRange(faces);
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationTessellation(this IModel model, PolyMesh mesh, IfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            IfcGeometricRepresentationItem item = triangulated ? CreateTriangulatedFaceSet(model, mesh) : CreatePolygonalFaceSet(model, mesh);
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "Tessellation";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }

        #endregion

        #region Element with PolyMesh

        public static T CreateElement<T>(this IModel model, string elementName, IfcObjectPlacement placement, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            // create a Definition shape to hold the geometry
            var shape = model.CreateShapeRepresentationTessellation(mesh, layer, triangulated);
            IfcRepresentationItem repItem = shape.Items.First();

            // apply specific surfaceStyle / otherwise use mesh color / apply surface style / fallback-color
            IfcSurfaceStyle surf;

            if (surfaceStyle != null)
            {
                surf = surfaceStyle;
            }
            else if (mesh.HasColors)
            {
                // TODO: add mesh-color cach (could have implications on other re-used objects)
                var col = ((C4b)mesh.VertexAttributes.Get(PolyMesh.Property.Colors).GetValue(0)).ToC4d();
                surf = model.CreateSurfaceStyle(col.RGB, (1.0 - col.A).Clamp(0, 1), "MeshColor");
            }
            else if (layer is IfcPresentationLayerWithStyle a && a.LayerStyles.OfType<IfcSurfaceStyle>().FirstOrDefault() != null)
            {
                surf = a.LayerStyles.OfType<IfcSurfaceStyle>().First();
            }
            else
            {
                // caching / re-using of default_surfaces
                var defaultSurface = model.Instances.OfType<IfcSurfaceStyle>().Where(x => x.Name == "Default_Surface").FirstOrDefault();
                surf = defaultSurface ?? model.CreateSurfaceStyle(C3d.Red, 0.0, "Default_Surface");
            }

            // create visual style (works with 3d-geometry - body)
            repItem.CreateStyleItem(surf);

            var proxy = model.New<T>(c => {
                c.Name = elementName;

                // create a Product Definition and add the model geometry to the cube
                c.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));

                // now place the object into the model
                c.ObjectPlacement = placement;
            });

            return proxy;
        }

        public static T CreateElement<T>(this IModel model, string elementName, V3d position, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var placement = model.CreateLocalPlacement(position);
            return model.CreateElement<T>(elementName, placement, mesh, surfaceStyle, layer, triangulated);
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, IfcObjectPlacement placement, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, placement, mesh, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, V3d position, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, position, mesh, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }
        #endregion
    }

    public static class IfcObjectExtensions
    {
        public static IIfcValue GetArea(this IfcObject o)
        {
            // short-cut
            var area = o.PhysicalSimpleQuantities.OfType<IIfcQuantityArea>().FirstOrDefault()?.AreaValue; //.TryGetSimpleValue(out double areaValue);

            ////try to get the value from quantities first
            //var area =
            //    //get all relations which can define property and quantity sets
            //    obj.IsDefinedBy

            //    //Search across all property and quantity sets. 
            //    //You might also want to search in a specific quantity set by name
            //    .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)

            //    //Only consider quantity sets in this case.
            //    .OfType<IIfcElementQuantity>()

            //    //Get all quantities from all quantity sets
            //    .SelectMany(qset => qset.Quantities)

            //    //We are only interested in areas 
            //    .OfType<IIfcQuantityArea>()

            //    //We will take the first one. There might obviously be more than one area properties
            //    //so you might want to check the name. But we will keep it simple for this example.
            //    .FirstOrDefault()?.AreaValue;

            if (area != null) return area;

            //try to get the value from properties
            return IFCHelper.GetProperty(o, "Area"); // .TryGetSimpleValue(out double areaValue2);
        }

        public static IfcSlab CreateAttachSlab(this IfcSpatialStructureElement parent, string elementName, IfcPresentationLayerAssignment layer, IfcMaterial material)
        {
            var model = parent.Model;

            var box = new Box3d(V3d.Zero, new V3d(100.0, 100.0, 300.0));

            var shape = model.New<IfcShapeRepresentation>(s => {

                var rectProf = model.New<IfcRectangleProfileDef>(p =>
                {
                    p.ProfileName = "RectArea";
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.XDim = box.SizeX;
                    p.YDim = box.SizeY;
                });

                IfcGeometricRepresentationItem item = model.New<IfcExtrudedAreaSolid>(solid =>
                {
                    solid.Position = parent.Model.CreateAxis2Placement3D(box.Min);
                    solid.Depth = box.SizeZ;    // CAUTION: this must be the layer-thickness
                    solid.ExtrudedDirection = parent.Model.CreateDirection(V3d.ZAxis); // CAUTION: this must be the layer-orientation
                    solid.SweptArea = rectProf;
                });
                layer?.AssignedItems.Add(item);

                s.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().First();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });

            var slab = model.New<IfcSlab>(c => {
                c.Name = elementName;
                c.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));
                c.ObjectPlacement = parent.Model.CreateLocalPlacement(new V3d(500, 500, 500));
            });
            parent.AddElement(slab);

            // Link Material via RelAssociatesMaterial
            model.New<IfcRelAssociatesMaterial>(mat =>
            {
                // Material Layer Set Usage (HAS TO BE MANUALLY SYNCHED!)
                IfcMaterialLayerSetUsage usage = model.New<IfcMaterialLayerSetUsage>(u =>
                {
                    u.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                    u.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS3;
                    u.OffsetFromReferenceLine = 0;
                    u.ForLayerSet = model.New<IfcMaterialLayerSet>(set =>
                    {
                        set.LayerSetName = "Concrete Layer Set";
                        set.MaterialLayers.Add(model.New<IfcMaterialLayer>(layer =>
                        {
                            layer.Name = "Layer1";
                            layer.Material = material;
                            layer.LayerThickness = box.SizeZ;
                            layer.IsVentilated = false;
                            layer.Category = "Core";
                        }));
                    });
                });

                mat.Name = "RelMat";
                mat.RelatingMaterial = usage;
                mat.RelatedObjects.Add(slab);
            });

            return slab;
        }

        public static IfcAnnotation CreateAnnotation(this IModel model, string text, IfcObjectPlacement placement, V3d position, IfcPresentationLayerWithStyle layer)
        {
            // Anotation-Experiments https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/IfcAnnotation.htm
            return model.New<IfcAnnotation>(a =>
            {
                var box = new Box3d(V3d.Zero, new V3d(200, 100, 500)); // mm

                a.Name = "Intersection of " + text;
                a.ObjectPlacement = placement;
                a.Representation = model.New<IfcProductDefinitionShape>(r => {
                    r.Representations.AddRange([
                        model.CreateShapeRepresentationAnnotation2dText(text, position.XY, layer),
                        model.CreateShapeRepresentationAnnotation2dCurve([position.XY, (position.XY + new V2d(500, 750.0)), (position.XY + new V2d(1000,1000))], [[1,2,3]], layer),
                        model.CreateShapeRepresentationAnnotation3dCurve([position, (position + new V3d(500, 750.0, 100)), (position + new V3d(1000,1000, 200))], layer),
                        model.CreateShapeRepresentationAnnotation3dSurface(Plane3d.ZPlane, new Polygon2d(box.XY.Translated(position.XY-box.XY.Center).ComputeCornersCCW()), layer),
                        model.CreateShapeRepresentationAnnotation3dCross(position, V3d.YAxis, 45, 1000.0, layer)
                        //// NOT-displayed in BIMVision
                        //model.CreateShapeRepresentationAnnotation2dPoint(position.XY, layer),
                        //model.CreateShapeRepresentationAnnotation3dPoint(position, layer),
                        //model.CreateShapeRepresentationAnnotation2dArea(new Box2d(V2d.Zero, V2d.One*1000.0), layer),

                        // broken
                        //model.CreateShapeRepresentationSurveyPoints(position.XY),
                        //model.CreateShapeRepresentationSurveyPoints(position),
                    ]);
                });
            });
        }

        public static IfcLightFixture CreateLightAmbient(this IModel model, string name, C3d color, IfcObjectPlacement placement, IfcPresentationLayerAssignment layer)
        {
            // box to visualize light dimensions
            var shape = model.CreateShapeRepresentationSolidBox(new Box3d(V3d.Zero, new V3d(200, 200, 300)), layer);

            //IfcRepresentationItem repItem = shape.Items.First();
            //repItem.CreateStyleItem(model.Instances.OfType<IfcSurfaceStyle>().First());

            // TODO: for regular grid matrix could be used...
            var distribution = model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, [
                // Main plane-angle and its secondary-plane-angles     
                new IFCHelper.LightIntensityDistributionData(0, [new IFCHelper.AngleAndIntensity(0.0, 100.0), new IFCHelper.AngleAndIntensity(90.0, 200.0), new IFCHelper.AngleAndIntensity(180.0, 100.0)]),
                new IFCHelper.LightIntensityDistributionData(180, [new IFCHelper.AngleAndIntensity(0.0, 10.0), new IFCHelper.AngleAndIntensity(45.0, 15.0), new IFCHelper.AngleAndIntensity(90.0, 20.0), new IFCHelper.AngleAndIntensity(135.0, 15.0), new IFCHelper.AngleAndIntensity(180.0, 10.0)])
            ]);

            return model.New<IfcLightFixture>(t =>
            {
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = model.New<IfcProductDefinitionShape>(r =>
                {
                    r.Representations.AddRange([
                        model.CreateShapeRepresentationLightingAmbient(color),
                        model.CreateShapeRepresentationLightingDirectional(color, V3d.ZAxis),
                        model.CreateShapeRepresentationLightingGoniemtric(color, V3d.Zero, 1000, 1000, distribution),
                        shape,
                    ]);
                });
            });

        }
    }

    public static class AardvarkExtensions
    {
        // Overrides values from Aardvark-Types
        public static void Set(this IfcCartesianPoint point, V2d vec)
        {
            point.SetXY(vec.X, vec.Y);
        }

        public static void Set(this IfcCartesianPoint point, V3d vec)
        {
            point.SetXYZ(vec.X, vec.Y, vec.Z);
        }

        public static void Set(this IfcDirection dir, V3d d)
        {
            dir.SetXYZ(d.X, d.Y, d.Z);
        }

        public static void Set(this IfcDirection dir, V2d d)
        {
            dir.SetXY(d.X, d.Y);
        }

        public static void Set(this IfcColourRgb c, C3d colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static void Set(this IfcColourRgb c, C3f colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static void Set(this IfcBoundingBox b, Box3d box)
        {
            b.Corner = b.Model.CreatePoint(box.Min);
            b.XDim = box.SizeX;
            b.YDim = box.SizeY;
            b.ZDim = box.SizeZ;
        }
    }
}