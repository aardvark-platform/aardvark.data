using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    /// <summary>
    /// Lighting tracks as products of their own (IFC4+): an IfcCableSegment with PredefinedType BUSBARSEGMENT, electrically what a track is,
    /// listed by every BIM tool as its own product kind. An IfcCableSegmentType shares body geometry and makes counting per type work like for lights.
    /// The snap curve is the 'Axis' representation of the track; lights and sub-tracks become parts (IfcRelAggregates) connected at a parameter on it.
    /// </summary>
    public static class TrackExt
    {
        /// <summary>
        /// Type of a track with shared body geometry; elementType names the article, e.g. "LightTrack 2m", and becomes the ObjectType of the instances.
        /// </summary>
        public static IIfcCableSegmentType CreateTrackType(this IModel model, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties = null, string name = "", string elementType = null)
        {
            var type = model.CableSegmentTypeFactory(t =>
            {
                t.Name = (name == "") ? "TrackType_BUSBARSEGMENT" : name;
                t.PredefinedType = IfcCableSegmentTypeEnum.BUSBARSEGMENT;
                if (elementType != null)
                    t.ElementType = elementType;
            });
            type.AttachGeometriesAndProperties(repMaps, properties);
            return type;
        }

        /// <summary>
        /// A track as instance of its type with the body of the type; the snap curve, in track coordinates, is attached as 'Axis' representation.
        /// </summary>
        public static IIfcCableSegment Instantiate(this IIfcCableSegmentType type, string name, IIfcObjectPlacement placement, Trafo3d trafo, IIfcCurve snapCurve = null)
        {
            var track = type.Model.CableSegmentFactory(t => t.Name = name);
            track.CreateAttachInstancedRepresentation(type, placement, trafo);
            track.PredefinedType = type.PredefinedType;
            if (type.ElementType.HasValue)
                track.ObjectType = type.ElementType;
            if (snapCurve != null)
                track.AddRepresentation(snapCurve.CreateShapeRepresentationAxis3d());
            return track;
        }

        /// <summary>
        /// An untyped track from a snap curve and an optional body, both in track coordinates; objectType names the kind of track.
        /// </summary>
        public static IIfcCableSegment CreateTrack(this IModel model, string name, IIfcObjectPlacement placement, IIfcCurve snapCurve, IIfcShapeRepresentation body = null, string objectType = null)
        {
            var track = model.CableSegmentFactory(t =>
            {
                t.Name = name;
                t.PredefinedType = IfcCableSegmentTypeEnum.BUSBARSEGMENT;
                if (objectType != null)
                    t.ObjectType = objectType;
                t.ObjectPlacement = placement;
            });
            if (body != null)
                track.AddRepresentation(body);      // body first, viewers take the first representation as the shape
            track.AddRepresentation(snapCurve.CreateShapeRepresentationAxis3d());
            return track;
        }

        /// <summary>
        /// The curve things snap to: the first curve of the 'Axis' representation, null without one.
        /// </summary>
        public static IIfcCurve GetSnapCurve(this IIfcProduct product)
        {
            return product.Representation?.Representations
                .OfType<IIfcShapeRepresentation>()
                .Where(r => r.RepresentationIdentifier == "Axis")
                .SelectMany(r => r.Items.OfType<IIfcCurve>())
                .FirstOrDefault();
        }

        /// <summary>
        /// Makes the element a part of the track and snaps it to the parameter on the snap curve (0..1 per segment of an IfcIndexedPolyCurve).
        /// Works for lights and sub-tracks alike; the element's placement is not changed, use track.CreateRelativePlacement for it.
        /// </summary>
        public static IIfcRelConnectsElements AttachToTrack(this IIfcCableSegment track, IIfcElement element, double parameter, string name = null)
        {
            var curve = track.GetSnapCurve() ?? throw new InvalidOperationException($"track {track.Name} has no 'Axis' representation to snap to");
            track.AddParts(element);
            return track.ConnectAtPointOnCurve(element, curve, parameter, name);
        }

        /// <summary>
        /// All tracks of the model, i.e. cable segments that are busbar segments themselves or through their type.
        /// </summary>
        public static IEnumerable<IIfcCableSegment> GetTracks(this IModel model)
            => model.Instances.OfType<IIfcCableSegment>().Where(IsTrack);

        public static bool IsTrack(this IIfcObject obj)
        {
            if (obj is not IIfcCableSegment segment)
                return false;
            if (segment.PredefinedType == IfcCableSegmentTypeEnum.BUSBARSEGMENT)
                return true;
            return segment.IsTypedBy.Any(r => (r.RelatingType is IIfcCableSegmentType type) && (type.PredefinedType == IfcCableSegmentTypeEnum.BUSBARSEGMENT));
        }
    }
}
