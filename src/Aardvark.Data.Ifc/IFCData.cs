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
        private IfcStore m_model;

        private IInverseCache m_inversCache;

        private IEntityCache m_entityCache;

        public Dict<IfcGloballyUniqueId, IFCContent> Content { get; private set; }
        public Dictionary<string, IFCMaterial> Materials { get; private set; }
        public double UnitScale { get; private set; }
        public IFCNode Hierarchy { get; private set; }
        public IIfcProject Project { get; }
        public string ProjectName { get; private set; }

        public IFCData(IfcStore model, IInverseCache inversCache, IEntityCache entityCache, Dict<IfcGloballyUniqueId, IFCContent> content, Dictionary<string, IFCMaterial> materials, double scale, IFCNode hierarchy)
        {
            m_inversCache = inversCache;
            m_entityCache = entityCache;
            m_model = model;
            Content = content;
            UnitScale = scale;
            Hierarchy = hierarchy;
            Project = IFCHelper.GetProject(m_model);
            ProjectName = Project.LongName;
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