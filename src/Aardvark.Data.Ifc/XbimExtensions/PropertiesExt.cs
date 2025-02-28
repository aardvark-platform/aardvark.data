using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;

namespace Aardvark.Data.Ifc
{
    public static class PropertiesExt
    {
        private static bool EqualOrContainsName(this IIfcPropertySingleValue value, string queryString)
           => string.Equals(value.Name, queryString, StringComparison.OrdinalIgnoreCase) || value.Name.ToString().ToLower().Contains(queryString.ToLower());

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcObject o)
        {
            // TODO: misses to query for Ifc2x3 IsDefinedByProperties
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
            return GetProperty(o, "Area"); // .TryGetSimpleValue(out double areaValue2);
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
                catch (Exception ex)
                {
                    if (ex is NullReferenceException || ex is ArgumentNullException)
                    {

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

        public static bool TryGetSimpleValue<T>(this IIfcPropertySingleValue property, out T result) where T : struct
        {
            var isValid = property.NominalValue.TryGetSimpleValue(out T res);
            result = res;

            return isValid;
        }

        public static IIfcPropertySingleValue CreatePropertySingleValue<V>(this IModel model, string name, V value) where V : IIfcValue
        {
            return model.Factory().PropertySingleValue(p => {
                p.Name = name;
                p.NominalValue = value;
            });
        }

        public static IIfcPropertyEnumeratedValue CreatePropertyEnumeratedValue<V>(this IModel model, string name, V value) where V : IIfcValue
        {
            return model.Factory().PropertyEnumeratedValue(p =>
            {
                p.Name = name;
                p.EnumerationValues.Add(value);
            });
        }

        #endregion

        #region Quantities

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

        public static IIfcPhysicalSimpleQuantity CreatePhysicalSimpleQuantity(this IModel model, XbimQuantityTypeEnum quantityType, double value, string name = null)
        {
            var factory = new EntityCreator(model);
            return quantityType switch
            {
                XbimQuantityTypeEnum.Area => factory.QuantityArea(sq => { if (name != null) sq.Name = name; sq.AreaValue = (IfcAreaMeasure)value; }),
                XbimQuantityTypeEnum.Length => factory.QuantityLength(sq => { if (name != null) sq.Name = name; sq.LengthValue = (IfcLengthMeasure)value; }),
                XbimQuantityTypeEnum.Volume => factory.QuantityVolume(sq => { if (name != null) sq.Name = name; sq.VolumeValue = (IfcVolumeMeasure)value; }),
                XbimQuantityTypeEnum.Count => factory.QuantityCount(sq => { if (name != null) sq.Name = name; sq.CountValue = (IfcCountMeasure)value; }),
                XbimQuantityTypeEnum.Weight => factory.QuantityWeight(sq => { if (name != null) sq.Name = name; sq.WeightValue = (IfcMassMeasure)value; }),
                XbimQuantityTypeEnum.Time => factory.QuantityTime(sq => { if (name != null) sq.Name = name; sq.TimeValue = (IfcTimeMeasure)value; }),
                _ => default,
            }; 
        }

        #endregion

        #region Dictionary Helper
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
                .ToDictionaryDistinct(t => t.Item1, t => t.Item3, (x, w) => true);
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
        #endregion

        #region PropertySet
        
        public static IIfcPropertySet CreatePropertySet(this IModel model, string setName, IDictionary<string, object> parameters)
        {
            // supports the following types: https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcmeasureresource/lexical/ifcvalue.htm
            var factory = model.Factory();

            return factory.PropertySet(pset => {
                pset.Name = setName;
                pset.HasProperties.AddRange(
                    parameters.Select(x => factory.PropertySingleValue(p =>
                    {
                        p.Name = x.Key;
                        p.NominalValue = x.Value switch
                        {
                            IIfcValue v => v,
                            decimal d => (IfcReal) (double) d,
                            double d => (IfcReal)d,
                            float r => (IfcReal)r,
                            int i => (IfcInteger)i,
                            bool b => (IfcBoolean)b,
                            _ => (IfcText)x.Value.ToString(),
                        };
                    }))
                );
            });
        }

        public static IIfcProduct AttachPropertySet(this IIfcProduct o, IIfcPropertySet set)
        {
            o.AddPropertySet(set);
            return o;
        }

        public static IIfcPropertySet CreateAttachPropertySet(this IIfcObject o, string setName, IDictionary<string, object> parameters)
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
    }
}