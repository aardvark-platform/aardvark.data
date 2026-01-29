using Aardvark.Base;
using System;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;

namespace Aardvark.Data.Ifc
{
    public class IFCData : IDisposable
    {
        private readonly IfcStore m_model;

        private readonly IInverseCache m_inversCache;

        private readonly IEntityCache m_entityCache;

        public IfcStore IfcStore { get { return m_model; } }
        public Dict<IfcGloballyUniqueId, IFCContent> Content { get; private set; }
        public IFCNode Hierarchy { get; private set; }
        public Dictionary<string, IFCMaterial> Materials { get; private set; }
        public double UnitScale { get { return IfcStore.ModelFactors.LengthToMetresConversionFactor; } }
        public IIfcProject Project { get { return GeneralExt.GetProject(IfcStore); } }
        public string ProjectName { get { return Project.LongName; } }

        internal IFCData(IfcStore model, IInverseCache inversCache, IEntityCache entityCache, Dict<IfcGloballyUniqueId, IFCContent> content, Dictionary<string, IFCMaterial> materials, IFCNode hierarchy)
        {
            m_inversCache = inversCache;
            m_entityCache = entityCache;
            m_model = model;
            Content = content;
            Hierarchy = hierarchy;
            Materials = materials;
        }

        public void Dispose()
        {
            m_entityCache.Dispose();
            m_inversCache.Dispose();
            m_model.Dispose();
        }
    }
}