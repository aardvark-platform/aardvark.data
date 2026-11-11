using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;

namespace Aardvark.Data.Ifc
{
    public class IFCData : IDisposable
    {
        private readonly IfcStore m_model;

        private IInverseCache m_inverseCache;

        private IEntityCache m_entityCache;

        public IfcStore IfcStore { get { return m_model; } }
        public Dict<IfcGloballyUniqueId, IFCContent> Content { get; private set; }
        public IFCNode Hierarchy { get; private set; }
        public Dictionary<string, IFCMaterial> Materials { get; private set; }
        public double UnitScale { get { return IfcStore.ModelFactors.LengthToMetresConversionFactor; } }
        public IIfcProject Project { get { return GeneralExt.GetProject(IfcStore); } }
        public string ProjectName { get { return Project.LongName; } }

        /// <summary>
        /// All geometries of a product, meshes and line sets alike (Content stacks entries under one GUID).
        /// </summary>
        public IEnumerable<IFCContent> GetContent(IfcGloballyUniqueId guid)
            => Content.ValuesWithKey(guid);

        /// <summary>
        /// The line sets of a product, one per curve representation, optionally only those of one representation identifier such as "Axis".
        /// </summary>
        public IEnumerable<PolyLineSet> GetLineSets(IfcGloballyUniqueId guid, string representationIdentifier = null)
            => GetContent(guid).Where(c => c.HasLineSet && ((representationIdentifier == null) || (c.RepresentationIdentifier == representationIdentifier))).Select(c => c.LineSet);

        public bool HasLineSet(IfcGloballyUniqueId guid)
            => GetContent(guid).Any(c => c.HasLineSet);

        /// <summary>
        /// True while xBIM's inverse and entity caches are alive.
        /// xBIM does not allow transactions while the inverse cache is active, see EndCaching.
        /// </summary>
        public bool IsCaching { get { return m_inverseCache != null; } }

        internal IFCData(IfcStore model, IInverseCache inverseCache, IEntityCache entityCache, Dict<IfcGloballyUniqueId, IFCContent> content, Dictionary<string, IFCMaterial> materials, IFCNode hierarchy)
        {
            m_inverseCache = inverseCache;
            m_entityCache = entityCache;
            m_model = model;
            Content = content;
            Hierarchy = hierarchy;
            Materials = materials;
        }

        /// <summary>
        /// Stops xBIM's inverse and entity caching so the store accepts IfcStore.BeginTransaction again.
        /// Queries get slower afterwards, so call this right before editing the model.
        /// </summary>
        public void EndCaching()
        {
            if (!IsCaching)
                return;

            DisposeCaches();

            // xBIM only keeps a weak reference to the inverse cache and checks it when a transaction is opened,
            // so the collection is forced to make the model forget the cache deterministically
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void DisposeCaches()
        {
            m_entityCache?.Dispose();
            m_inverseCache?.Dispose();
            m_entityCache = null;
            m_inverseCache = null;
        }

        public void Dispose()
        {
            DisposeCaches();
            m_model.Dispose();
        }
    }
}
