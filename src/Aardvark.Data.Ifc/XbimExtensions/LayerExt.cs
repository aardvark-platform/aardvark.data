using System;
using System.Collections.Generic;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class LayerExt
    {
        public static IIfcPresentationLayerAssignment CreateLayer(this IModel model, string layerName, IEnumerable<IIfcLayeredItem> items = null)
        {
            // IfcPresentationLayerAssignment only allows: IFC4.IFCSHAPEREPRESENTATION", "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM
            return model.Factory().PresentationLayerAssignment(layer => {
                layer.Name = layerName;
                if (!items.IsEmptyOrNull()) layer.AssignedItems.AddRange(items);
            });
        }

        public static IIfcPresentationLayerAssignment CreateLayer(this IModel model, string layerName, IIfcLayeredItem items)
            => model.CreateLayer(layerName, [items]);

        public static IIfcPresentationLayerWithStyle CreateLayerWithStyle(this IModel model, string layerName, IEnumerable<IIfcPresentationStyle> styles, bool layerVisibility = true, bool layerFrozen = false, bool layerBlocked = false, IEnumerable<IIfcGeometricRepresentationItem> items = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationorganizationresource/lexical/ifcpresentationlayerwithstyle.htm
            // IfcPresentationLayerWithStyle only allows: "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM"

            return model.Factory().PresentationLayerWithStyle(layer => {
                layer.Name = layerName;

                // Visibility Control
                layer.LayerOn = layerVisibility; // visibility control allows to define a layer to be either 'on' or 'off', and/or 'frozen' or 'not frozen'
                layer.LayerFrozen = layerFrozen;

                // Access control
                layer.LayerBlocked = layerBlocked;    // access control allows to block graphical entities from manipulations

                // NOTE: ORDER seems to be important! BIM-Viewer tend to use only color information of first item!
                layer.LayerStyles.AddRange(styles);
                if (items != null && !items.IsEmpty()) layer.AssignedItems.AddRange(items);
            });
        }

        public static IIfcPresentationLayerWithStyle CreateLayerWithStyle(this IModel model, string layerName, IEnumerable<IIfcPresentationStyle> styles, IIfcGeometricRepresentationItem item, bool layerVisibility = true, bool layerFrozen = false, bool layerBlocked = false)
            => model.CreateLayerWithStyle(layerName, styles, layerVisibility, layerFrozen, layerBlocked, [item]);

        public static IIfcLayeredItem AssignLayer(this IIfcLayeredItem item, IIfcPresentationLayerAssignment layer)
        {
            if (layer is IIfcPresentationLayerWithStyle && item is IIfcShapeRepresentation)
            {
                // IfcPresentationLayerWithStyle only allows "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM"
                throw new ArgumentException("IfcShapeRepresentation cannot be assigened to IfcPresentationLayerWithStyle");
            }
            // IfcPresentationLayerAssignment only allows: IFC4.IFCSHAPEREPRESENTATION", "IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }

        public static IIfcGeometricRepresentationItem AssignLayer(this IIfcGeometricRepresentationItem item, IIfcPresentationLayerAssignment layer)
        {
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }

        public static IIfcMappedItem AssignLayer(this IIfcMappedItem item, IIfcPresentationLayerAssignment layer)
        {
            if (!layer.AssignedItems.Contains(item)) layer.AssignedItems.Add(item);
            return item;
        }
    }
}
