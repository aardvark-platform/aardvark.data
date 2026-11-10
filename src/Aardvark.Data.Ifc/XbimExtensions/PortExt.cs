using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    /// <summary>
    /// Mounting points with a full frame (IFC4+): an IfcDistributionPort is a product of its own, nested in its element and placed relative
    /// to it, so a port carries position and orientation; IfcRelConnectsPorts links the port of a mounted element to the port of its carrier.
    /// Next to the snap on a curve (AssemblyExt.ConnectAtPointOnCurve) this is the discrete, oriented way to attach lights, and it is the
    /// electrical connection MEP tools expect on a busbar.
    /// </summary>
    public static class PortExt
    {
        /// <summary>
        /// A port on the element with the given frame (position and orientation) in element coordinates; it is placed relative to the element, so it moves with it.
        /// A markerSize above zero adds an annotation showing the frame (see CreateFrameMarker) for visual debugging; ports have no geometry otherwise.
        /// </summary>
        public static IIfcDistributionPort CreatePort(this IIfcElement element, string name, Trafo3d frame, IfcFlowDirectionEnum flowDirection = IfcFlowDirectionEnum.SOURCEANDSINK,
            IfcDistributionPortTypeEnum predefinedType = IfcDistributionPortTypeEnum.CABLE, IfcDistributionSystemEnum systemType = IfcDistributionSystemEnum.LIGHTING, double markerSize = 0.0)
        {
            var model = element.Model;
            var port = model.Factory().DistributionPort(p =>
            {
                p.Name = name;
                p.FlowDirection = flowDirection;
                p.PredefinedType = predefinedType;
                p.SystemType = systemType;
                p.ObjectPlacement = model.CreateLocalPlacement(frame, element.ObjectPlacement);
            });

            // IFC4 nests ports in their element, IFC2x3 used IfcRelConnectsPortToElement instead and is not supported here
            var nesting = element.IsNestedBy.FirstOrDefault() ?? model.Factory().RelNests(r => r.RelatingObject = element);
            nesting.RelatedObjects.Add(port);

            if (markerSize > 0)
                port.CreateFrameMarker(markerSize);
            return port;
        }

        private static readonly C3d s_markerGreen = new(0, 1, 0);     // C3d.Green is the half-intensity web green

        /// <summary>
        /// A marker showing the frame of the port, for visual debugging: an IfcAnnotation (ObjectType "PortMarker") with three lines from the port origin
        /// along x (red), y (green) and z (blue), placed on the port and aggregated into the port's element, because viewers skip nested ports.
        /// BIMvision draws the lines of an annotation product with their colours; the import gives a line set with the colours in LineAttributes[Colors].
        /// </summary>
        public static IIfcAnnotation CreateFrameMarker(this IIfcDistributionPort port, double size, double lineWidth = 0)
        {
            var model = port.Model;
            var carrier = port.GetElement() ?? throw new InvalidOperationException($"port {port.Name} is not nested in an element");

            var marker = model.CreateAnnotation($"{port.Name} marker", model.CreateLocalPlacement(Trafo3d.Identity, port.ObjectPlacement), model.CreateFrameMarkerLines(size, lineWidth), "PortMarker");
            carrier.AddParts(marker);
            return marker;
        }

        /// <summary>
        /// Three lines from the origin along x (red), y (green) and z (blue) as 'Annotation' curve set with curve styles. A lineWidth of zero means a
        /// two-hundredth of the size; viewers draw the width as a solid, so keep it small (a fiftieth already reads as boxes in BIMvision).
        /// </summary>
        public static IIfcShapeRepresentation CreateFrameMarkerLines(this IModel model, double size, double lineWidth = 0)
        {
            var width = lineWidth > 0 ? lineWidth : size / 200;

            IIfcCurve Axis(V3d direction, C3d color)
            {
                var line = model.CreatePolyLine(V3d.Zero, direction * size);
                line.CreateStyleItem(model.CreateCurveStyle(color, width));
                return line;
            }
            return model.CreateShapeRepresentationAnnotation3dCurveSet([Axis(V3d.XAxis, C3d.Red), Axis(V3d.YAxis, s_markerGreen), Axis(V3d.ZAxis, C3d.Blue)]);
        }

        public static IEnumerable<IIfcDistributionPort> GetPorts(this IIfcElement element)
            => element.IsNestedBy.SelectMany(r => r.RelatedObjects).OfType<IIfcDistributionPort>();

        /// <summary>
        /// The element the port is nested in.
        /// </summary>
        public static IIfcElement GetElement(this IIfcPort port)
            => port.Nests.Select(r => r.RelatingObject).OfType<IIfcElement>().FirstOrDefault();

        /// <summary>
        /// The frame of the port in the coordinates of its element, i.e. what CreatePort was given.
        /// </summary>
        public static Trafo3d GetLocalFrame(this IIfcPort port)
        {
            if (port.ObjectPlacement is not IIfcLocalPlacement local)
                throw new NotSupportedException($"port {port.Name} has no IfcLocalPlacement");
            return local.RelativePlacement.ToTrafo3d();
        }

        public static IIfcRelConnectsPorts ConnectPorts(this IIfcDistributionPort relating, IIfcDistributionPort related, string name = null)
        {
            return relating.Model.Factory().RelConnectsPorts(rel =>
            {
                if (name != null)
                    rel.Name = name;
                rel.RelatingPort = relating;
                rel.RelatedPort = related;
            });
        }

        /// <summary>
        /// Ports connected to this one, whichever side of the relation they are on.
        /// </summary>
        public static IEnumerable<IIfcPort> GetConnectedPorts(this IIfcPort port)
            => port.ConnectedTo.Select(r => r.RelatedPort).Concat(port.ConnectedFrom.Select(r => r.RelatingPort));

        /// <summary>
        /// Mounts the element at the port: its placement is changed so that elementPort coincides with the port, orientation included, it becomes a
        /// part of the port's element, and the two ports are connected. Without an element port one is created at the element origin.
        /// The element's placement entity is kept, so its own parts and ports move along; mount elements that are not yet contained anywhere else.
        /// </summary>
        public static IIfcRelConnectsPorts MountAtPort(this IIfcDistributionPort port, IIfcElement element, IIfcDistributionPort elementPort = null, string name = null)
        {
            var carrier = port.GetElement() ?? throw new InvalidOperationException($"port {port.Name} is not nested in an element");
            var model = port.Model;

            // the element frame is the port frame moved back by the element port's own frame, so both ports fall together
            var offset = elementPort != null ? elementPort.GetLocalFrame().Inverse : Trafo3d.Identity;
            if (element.ObjectPlacement is IIfcLocalPlacement local)
            {
                local.PlacementRelTo = port.ObjectPlacement;
                local.RelativePlacement = model.CreateAxis2Placement3D(offset);
            }
            else if (element.ObjectPlacement == null)
                element.ObjectPlacement = model.CreateLocalPlacement(offset, port.ObjectPlacement);
            else
                throw new NotSupportedException($"{element.Name} has a {element.ObjectPlacement.GetType().Name}, only IfcLocalPlacement can be mounted");

            elementPort ??= element.CreatePort($"{element.Name} port", Trafo3d.Identity, IfcFlowDirectionEnum.SINK,
                port.PredefinedType ?? IfcDistributionPortTypeEnum.CABLE, port.SystemType ?? IfcDistributionSystemEnum.LIGHTING);

            carrier.AddParts(element);
            return port.ConnectPorts(elementPort, name);
        }
    }
}
