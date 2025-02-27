using System;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class EntityCreatorExtensions
    {
        public static @IIfcLightFixture LightFixtureExt(this IModel model, Action<@IIfcLightFixture> init = null)
        {
            return model.SchemaVersion switch
            {
                // IfcLightFixture Not supported in 2x3
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.ElectricalDomain.IfcLightFixture>(init),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.ElectricalDomain.IfcLightFixture>(init),
                _ => throw new NotSupportedException($"Type IfcLightFixture is not supported in schema {model.SchemaVersion}")
            };
        }

        public static @IIfcFlowTerminal LightFixtureExtIFC2x3(this IModel model, Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureTypeEnum? lightType = null)
        {
            return model.SchemaVersion switch
            {
                XbimSchemaVersion.Ifc2X3 => 
                    // NOTE: Ifc2X3 requires a LightFixtureType to type FlowTerminal as Light
            
                    // CAUTION: Instantiation-Routine links LightFixtureType to Flowterminal! (via CreateAttachInstancedRepresentation) => lightType = null
                    // CAUTION: Direct Creation of Lights require for IFC2x3 a IfcLightFixtureTypeEnum!
                    model.Instances.New<Xbim.Ifc2x3.SharedBldgServiceElements.IfcFlowTerminal>(o => {
                        if (lightType != null)
                        {
                            o.AddDefiningType(model.Instances.New<Xbim.Ifc2x3.ElectricalDomain.IfcLightFixtureType>(lt => {
                                lt.PredefinedType = lightType.Value;
                                lt.Name = "LightType";
                            }));    
                        }
                    }),
                XbimSchemaVersion.Ifc4 => model.Instances.New<Xbim.Ifc4.ElectricalDomain.IfcLightFixture>(),
                XbimSchemaVersion.Ifc4x3 => model.Instances.New<Xbim.Ifc4x3.ElectricalDomain.IfcLightFixture>(),
                _ => throw new NotSupportedException($"Type IfcLightFixture is not supported in schema {model.SchemaVersion}")
            };
        }
    }
}