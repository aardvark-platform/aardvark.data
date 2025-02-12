using System;
using System.Collections.Generic;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.RepresentationResource;

namespace Aardvark.Data.Ifc
{
    public static class LayerExt
    {
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
    }
}
