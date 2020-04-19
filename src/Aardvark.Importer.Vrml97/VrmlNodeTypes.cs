using Aardvark.Base;
using Aardvark.Base.Coder;
using Aardvark.Data.Vrml97;
using Aardvark.Geometry;
using Aardvark.VRVis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aardvark.Importer.Vrml97
{
    public class VrmlNode : IEquatable<VrmlNode>, IFieldCodeable
    {
        public string Name;

        public VrmlNode() { }

        internal virtual void Init(SymMapBase map)
        {
            Name = map.Get<string>((Symbol)"DEFname");
        }

        #region IEquatable<VrmlNode> Members

        public bool Equals(VrmlNode other)
        {
            return other == this;
        }

        #endregion
            
        #region IFieldCodeable Members

        public virtual IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            yield return new FieldCoder(0, "Name", (c, o) => c.CodeString(ref ((VrmlNode)o).Name));
        }

        #endregion
    }

    public class VrmlGroup : VrmlNode, INode, IEnumerable<VrmlNode>
    {
        protected List<VrmlNode> m_children;

        public VrmlGroup() { }

        public VrmlGroup(string name) { Name = name; }

        public List<VrmlNode> Children
        {
            get { return m_children; }
            set { m_children = value; }
        }

        /// <summary>
        /// Note: null entities are ignored
        /// </summary>
        public void Add(VrmlNode node)
        {
            if (node != null)
                Insert(node, int.MaxValue);
        }

        public void Insert(VrmlNode node, int pos = Int32.MaxValue)
        {
            if (m_children == null)
                m_children = new List<VrmlNode>();
            m_children.Insert(pos.Clamp(0, m_children.Count), node);
        }

        public void Remove(VrmlNode node)
        {
            if (m_children == null) throw new ArgumentNullException(nameof(node));
            m_children.Remove(node);
        }

        public void Clear()
        {
            m_children = null;
        }

        public int ChildCount
        {
            get { return m_children != null ? m_children.Count : 0; }
        }

        internal override void Init(SymMapBase map)
        {
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Children", (c,o) => c.CodeList_of_T_(ref ((VrmlGroup)o).m_children));
        }

        #endregion

        public IEnumerable<INode> SubNodes
        {
            get
            {
                return this.GetFrames();
            }
        }

        #region IEnumerable<VrmlNode> Members

        public IEnumerator<VrmlNode> GetEnumerator()
        {
            return (m_children ?? Enumerable.Empty<VrmlNode>()).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (m_children ?? Enumerable.Empty<VrmlNode>()).GetEnumerator();
        }

        #endregion
    }

    public class VrmlScene : VrmlGroup
    {
        public string[] Info;
        public string Title;

        public List<VrmlRoute> Routes = new List<VrmlRoute>();
        public List<VrmlPositionInterpolator> PositionInterpolators = new List<VrmlPositionInterpolator>();
        public List<VrmlOrientationInterpolator> OrientationInterpolators = new List<VrmlOrientationInterpolator>();
        public List<VrmlTimeSensor> TimeSensors = new List<VrmlTimeSensor>();  

        public VrmlScene() { }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "Info", (c, o) => c.CodeStringArray(ref ((VrmlScene)o).Info));
            yield return new FieldCoder(4, "Title", (c, o) => c.CodeString(ref ((VrmlScene)o).Title));
            yield return new FieldCoder(5, "Routes", (c, o) => c.CodeList_of_T_(ref ((VrmlScene)o).Routes));
            yield return new FieldCoder(6, "PositionInterpolators", (c, o) => c.CodeList_of_T_(ref ((VrmlScene)o).PositionInterpolators));
            yield return new FieldCoder(7, "OrientationInterpolators", (c, o) => c.CodeList_of_T_(ref ((VrmlScene)o).OrientationInterpolators));
            yield return new FieldCoder(8, "TimeSensors", (c, o) => c.CodeList_of_T_(ref ((VrmlScene)o).TimeSensors));
        }

        #endregion
    }

    public class VrmlShape : VrmlNode
    {
        public VrmlAppearance Appearance;
        public VrmlGeometry Geometry;

        public VrmlShape() { }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Appearance", (c, o) => c.CodeT(ref ((VrmlShape)o).Appearance));
            yield return new FieldCoder(3, "Geometry", (c, o) => c.CodeT(ref ((VrmlShape)o).Geometry));
        }

        #endregion
    }

    public abstract class VrmlGeometry : VrmlNode
    {
    }

    public class VrmlMesh : VrmlGeometry {
        
        public PolyMesh Mesh;

        public VrmlMesh() { }

        internal override void Init(SymMapBase map)
        {
            base.Init(map);
            Mesh = PolyMeshFromVrml97.CreateFromIfs(map, PolyMeshFromVrml97.Options.NoVertexColorsFromMaterial | PolyMeshFromVrml97.Options.TryFixSpecViolations);
            // NOTE: If a geometry is annotated with a crease angle normals should be generated according to the vrml specification. 
            //       Since geometry might be broken, it is better to first do some cleanup/repair, but this should be decided in the application.
            //if (Mesh.InstanceAttributes.Contains(PolyMesh.Property.CreaseAngle) && !Mesh.HasNormals)
            //{
            //    var ca = Mesh.InstanceAttributes.GetAs<float>(PolyMesh.Property.CreaseAngle);
            //    Mesh = Mesh.WithPerVertexIndexedNormals(ca);
            //}
            if (!this.IsNamed()) Mesh.InstanceAttributes.Remove(PolyMesh.Property.Name);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Mesh", (c, o) => c.CodeT(ref ((VrmlMesh)o).Mesh));
        }

        #endregion
    
    }

    public class VrmlBox : VrmlGeometry 
    {
        public V3f Size;

        internal override void Init(SymMapBase map)
        {
            Size = map.Get<V3f>((Symbol)"size", new V3f(2, 2, 2));
                
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "Size", (c, o) => c.CodeV3f(ref ((VrmlBox)o).Size));
        }

        #endregion

    }

    public class VrmlSphere : VrmlGeometry
    {
        public float Radius;

        internal override void Init(SymMapBase map)
        {
            Radius = map.Get<float>((Symbol)"radius", 1.0f);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "Radius", (c, o) => c.CodeFloat(ref ((VrmlSphere)o).Radius));
        }

        #endregion

    }

    public class VrmlCone : VrmlGeometry
    {
        public float BottomRadius;
        public float Height;
        public bool Side;
        public bool Bottom;

        internal override void Init(SymMapBase map)
        {
            BottomRadius = map.Get<float>((Symbol)"bottomRadius", 1);
            Height = map.Get<float>((Symbol)"height", 2);
            Side = map.Get<bool>((Symbol)"side", true);
            Bottom = map.Get<bool>((Symbol)"bottom", true);
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "BottomRadius", (c, o) => c.CodeFloat(ref ((VrmlCone)o).BottomRadius));
            yield return new FieldCoder(4, "Height", (c, o) => c.CodeFloat(ref ((VrmlCone)o).Height));
            yield return new FieldCoder(5, "Side", (c, o) => c.CodeBool(ref ((VrmlCone)o).Side));
            yield return new FieldCoder(6, "Bottom", (c, o) => c.CodeBool(ref ((VrmlCone)o).Bottom));
        }

        #endregion

    }

    public class VrmlCylinder : VrmlGeometry
    {
        public bool Bottom;
        public float Height;
        public float Radius;
        public bool Side;
        public bool Top;

        internal override void Init(SymMapBase map)
        {
            Bottom = map.Get<bool>((Symbol)"bottom", true);
            Height = map.Get<float>((Symbol)"height", 2);
            Radius = map.Get<float>((Symbol)"radius", 1);
            Side = map.Get<bool>((Symbol)"side", true);
            Top = map.Get<bool>((Symbol)"top", true);
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "Bottom", (c, o) => c.CodeBool(ref ((VrmlCylinder)o).Bottom));
            yield return new FieldCoder(4, "Height", (c, o) => c.CodeFloat(ref ((VrmlCylinder)o).Height));
            yield return new FieldCoder(5, "Radius", (c, o) => c.CodeFloat(ref ((VrmlCylinder)o).Radius));
            yield return new FieldCoder(6, "Side", (c, o) => c.CodeBool(ref ((VrmlCylinder)o).Side));
            yield return new FieldCoder(7, "Top", (c, o) => c.CodeBool(ref ((VrmlCylinder)o).Top));
        }

        #endregion

    }

    public abstract class VrmlLight : VrmlNode
    {
        public float AmbientIntensity;
        public C3f Color;
        public float Intensity;
        public bool IsOn;

        internal override void Init(SymMapBase map)
        {
            AmbientIntensity = map.Get<float>((Symbol)"ambientIntensity", 0.0f);
            Color = map.Get<C3f>((Symbol)"color", C3f.White);
            Intensity = map.Get<float>((Symbol)"intensity", 1);
            IsOn = map.Get<bool>((Symbol)"on", true);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(3, "AmbientIntensity", (c, o) => c.CodeFloat(ref ((VrmlLight)o).AmbientIntensity));
            yield return new FieldCoder(4, "Color", (c, o) => c.CodeC3f(ref ((VrmlLight)o).Color));
            yield return new FieldCoder(5, "Intensity", (c, o) => c.CodeFloat(ref ((VrmlLight)o).Intensity));
            yield return new FieldCoder(6, "IsOn", (c, o) => c.CodeBool(ref ((VrmlLight)o).IsOn));
        }

        #endregion
    }

    public class VrmlSpotLight : VrmlLight
    {
        public V3f Attenuation;
        public float BeamWidth;
        public float CutOffAngle;
        public V3f Direction;
        public V3f Location;
        public float Radius;

        public VrmlSpotLight() { }

        internal override void Init(SymMapBase map)
        {
            Attenuation = map.Get<V3f>((Symbol)"attenuation", V3f.IOO);
            BeamWidth = map.Get<float>((Symbol)"beamWidth", (float)Constant.PiHalf);
            CutOffAngle = map.Get<float>((Symbol)"cutOffAngle", (float)Constant.PiQuarter);
            Direction = map.Get<V3f>((Symbol)"direction", -V3f.OOI);
            Location = map.Get<V3f>((Symbol)"location", V3f.OOO);
            Radius = map.Get<float>((Symbol)"radius", 1);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(7, "Attenuation", (c, o) => c.CodeV3f(ref ((VrmlSpotLight)o).Attenuation));
            yield return new FieldCoder(8, "BeamWidth", (c, o) => c.CodeFloat(ref ((VrmlSpotLight)o).BeamWidth));
            yield return new FieldCoder(9, "CutOffAngle", (c, o) => c.CodeFloat(ref ((VrmlSpotLight)o).CutOffAngle));
            yield return new FieldCoder(10, "Direction", (c, o) => c.CodeV3f(ref ((VrmlSpotLight)o).Direction));
            yield return new FieldCoder(11, "Location", (c, o) => c.CodeV3f(ref ((VrmlSpotLight)o).Location));
            yield return new FieldCoder(12, "Radius", (c, o) => c.CodeFloat(ref ((VrmlSpotLight)o).Radius));
        }

        #endregion
    }

    public class VrmlDirectionalLight : VrmlLight
    {
        public V3f Direction;

        public VrmlDirectionalLight() { }

        internal override void Init(SymMapBase map)
        {
            Direction = map.Get((Symbol)"direction", -V3f.OOI);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(7, "Direction", (c, o) => c.CodeV3f(ref ((VrmlDirectionalLight)o).Direction));
        }

        #endregion
    }

    public class VrmlPointLight : VrmlLight
    {
        public V3f Location;
        public float Radius;
        public V3f Attenuation;

        public VrmlPointLight() { }

        internal override void Init(SymMapBase map)
        {
            Attenuation = map.Get((Symbol)"attenuation", V3f.IOO);
            Location = map.Get((Symbol)"location", V3f.OOO);
            Radius = map.Get((Symbol)"radius", 1f);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(7, "Attenuation", (c, o) => c.CodeV3f(ref ((VrmlPointLight)o).Attenuation));
            yield return new FieldCoder(8, "Location", (c, o) => c.CodeV3f(ref ((VrmlPointLight)o).Location));
            yield return new FieldCoder(9, "Radius", (c, o) => c.CodeFloat(ref ((VrmlPointLight)o).Radius));
        }

        #endregion
    }

    public class VrmlTransform : VrmlGroup
    {
        public V3f Center;
        public V4f Rotation;
        public V4f ScaleRotation;
        public V3f Scale;
        public V3f Translation;

        public Trafo3d GetTrafo()
        {
            return VrmlHelpers.BuildVrmlGeometryTrafo((V3d)Center, (V4d)Rotation, (V3d)Scale, (V4d)ScaleRotation, (V3d)Translation);
        }

        public VrmlTransform() { }

        internal override void Init(SymMapBase map)
        {
            Center = map.Get<V3f>((Symbol)"center");
            Rotation = map.Get<V4f>((Symbol)"rotation");
            ScaleRotation = map.Get<V4f>((Symbol)"scaleOrientation");
            Scale = map.Get<V3f>((Symbol)"scale");
            Translation = map.Get<V3f>((Symbol)"translation");

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;

            yield return new FieldCoder(3, "Center", (c, o) => c.CodeV3f(ref ((VrmlTransform)o).Center));
            yield return new FieldCoder(4, "Rotation", (c, o) => c.CodeV4f(ref ((VrmlTransform)o).Rotation));
            yield return new FieldCoder(5, "ScaleRotation", (c, o) => c.CodeV4f(ref ((VrmlTransform)o).ScaleRotation));
            yield return new FieldCoder(6, "Scale", (c, o) => c.CodeV3f(ref ((VrmlTransform)o).Scale));
            yield return new FieldCoder(7, "Translation", (c, o) => c.CodeV3f(ref ((VrmlTransform)o).Translation));
        }

        #endregion
    }

    public class VrmlSwitch : VrmlGroup
    {
        public int SelectionIndex;

        public VrmlSwitch() { }

        internal override void Init(SymMapBase map)
        {
            SelectionIndex = map.Get<int>((Symbol)"whichChoice");
            base.Init(map);
        }

        public VrmlNode SelectedNode
        {
            get 
            {
                return SelectionIndex < 0 || m_children == null || m_children.Count <= SelectionIndex ? 
                            null : m_children[SelectionIndex]; 
            }
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Trafo", (c, o) => c.CodeT(ref ((VrmlSwitch)o).SelectionIndex));
        }

        #endregion
    }

    public class VrmlAppearance : VrmlNode
    {
        public VrmlMaterial Material;
        public VrmlTexture Textures;
        public VrmlTextureTransform TextureTrafo;

        public VrmlAppearance() { }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Material", (c, o) => c.CodeT(ref ((VrmlAppearance)o).Material));
            yield return new FieldCoder(3, "Textures", (c, o) => c.CodeT(ref ((VrmlAppearance)o).Textures));
            yield return new FieldCoder(4, "TextureTrafo", (c, o) => c.CodeT(ref ((VrmlAppearance)o).TextureTrafo));
        }

        #endregion
    }

    public class VrmlMaterial : VrmlNode
    {
        public C3f DiffuseColor;
        public C3f EmissiveColor;
        public C3f SpecularColor;
        public float AmbientIntensity;
        public float Opacity;
        public float Shininess;

        public VrmlMaterial() { }

        internal override void Init(SymMapBase map)
        {
            DiffuseColor = map.Get<C3f>((Symbol)"diffuseColor");
            EmissiveColor = map.Get<C3f>((Symbol)"emissiveColor");
            SpecularColor = map.Get<C3f>((Symbol)"specularColor");
            AmbientIntensity = map.Get<float>((Symbol)"ambientIntensity");
            Opacity = 1 - map.Get<float>((Symbol)"transparency");
            Shininess = map.Get<float>((Symbol)"shininess");

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "DiffuseColor", (c, o) => c.CodeC3f(ref ((VrmlMaterial)o).DiffuseColor));
            yield return new FieldCoder(3, "EmissiveColor", (c, o) => c.CodeC3f(ref ((VrmlMaterial)o).EmissiveColor));
            yield return new FieldCoder(4, "SpecularColor", (c, o) => c.CodeC3f(ref ((VrmlMaterial)o).SpecularColor));
            yield return new FieldCoder(5, "AmbientIntensity", (c, o) => c.CodeFloat(ref ((VrmlMaterial)o).AmbientIntensity));
            yield return new FieldCoder(6, "Opacity", (c, o) => c.CodeFloat(ref ((VrmlMaterial)o).Opacity));
            yield return new FieldCoder(7, "Shininess", (c, o) => c.CodeFloat(ref ((VrmlMaterial)o).Shininess));
        }

        #endregion
    }

    public class VrmlPositionInterpolator : VrmlNode
    {
        public List<float> Key;
        public List<V3f> KeyValue;

        public VrmlPositionInterpolator() { }

        internal override void Init(SymMapBase map)
        {
            Key = map.Get<List<float>>((Symbol) "key");
            KeyValue = map.Get<List<V3f>>((Symbol)"keyValue");

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;

            yield return new FieldCoder(2, "Key", (c, o) => c.CodeList_of_Float_(ref ((VrmlPositionInterpolator) o).Key));
            yield return new FieldCoder(3, "KeyValue", (c, o) => c.CodeList_of_V3f_(ref ((VrmlPositionInterpolator)o).KeyValue));
        }

        #endregion
    }

    public class VrmlOrientationInterpolator : VrmlNode
    {
        public List<float> Key;
        public List<V4f> KeyValue;

        public VrmlOrientationInterpolator() { }

        internal override void Init(SymMapBase map)
        {
            Key = map.Get<List<float>>((Symbol)"key");
            KeyValue = map.Get<List<V4f>>((Symbol)"keyValue");

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;

            yield return new FieldCoder(2, "Key", (c, o) => c.CodeList_of_Float_(ref ((VrmlOrientationInterpolator)o).Key));
            yield return new FieldCoder(3, "KeyValue", (c, o) => c.CodeList_of_V4f_(ref ((VrmlOrientationInterpolator)o).KeyValue));
        }

        #endregion
    }

    public class VrmlTimeSensor : VrmlNode
    {
        public float CycleInterval;
        public bool Enabled;
        public bool Loop;
        public float StartTime;
        public float StopTime;

        public VrmlTimeSensor() { }

        internal override void Init(SymMapBase map)
        {
            CycleInterval = map.Get<float>((Symbol)"cycleInterval");
            Enabled = map.Get<bool>((Symbol)"enabled");
            Loop = map.Get<bool>((Symbol)"loop");
            StartTime = map.Get<float>((Symbol)"startTime");
            StopTime = map.Get<float>((Symbol)"stopTime");

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;

            yield return new FieldCoder(2, "CycleInterval", (c, o) => c.CodeFloat(ref ((VrmlTimeSensor)o).CycleInterval));
            yield return new FieldCoder(3, "Enabled", (c, o) => c.CodeBool(ref ((VrmlTimeSensor)o).Enabled));
            yield return new FieldCoder(4, "Loop", (c, o) => c.CodeBool(ref ((VrmlTimeSensor)o).Loop));
            yield return new FieldCoder(5, "StartTime", (c, o) => c.CodeFloat(ref ((VrmlTimeSensor)o).StartTime));
            yield return new FieldCoder(6, "StopTime", (c, o) => c.CodeFloat(ref ((VrmlTimeSensor)o).StopTime));
        }

        #endregion
    }

    public class VrmlRoute : VrmlNode
    {
        private const string VrmlGetPrefix = "get_";
        private const string VrmlSetPrefix = "set_";

        public string SourceNode;
        public string SinkNode;

        public string SourceBinding;
        public string SinkBinding;

        public VrmlRoute() { }

        internal override void Init(SymMapBase map)
        {
            var inputIdentifiers = map.Get<string>((Symbol)"in").Split('.');
            var outputIdentifiers = map.Get<string>((Symbol)"out").Split('.');

            SourceNode = outputIdentifiers[0];
            SinkNode = inputIdentifiers[0];
            SourceBinding = RemovePrefix(outputIdentifiers[1], VrmlGetPrefix);
            SinkBinding = RemovePrefix(inputIdentifiers[1], VrmlSetPrefix);

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;

            yield return new FieldCoder(2, "SourceNode", (c, o) => c.CodeString(ref ((VrmlRoute)o).SourceNode));
            yield return new FieldCoder(3, "SinkNode", (c, o) => c.CodeString(ref ((VrmlRoute)o).SinkNode));
            yield return new FieldCoder(4, "SourceBinding", (c, o) => c.CodeString(ref ((VrmlRoute)o).SourceBinding));
            yield return new FieldCoder(5, "SinkBinding", (c, o) => c.CodeString(ref ((VrmlRoute)o).SinkBinding));
        }

        #endregion
        private static string RemovePrefix(string identifier, string vrmlPropertyIdentifier)
        {
            return identifier.StartsWith(vrmlPropertyIdentifier) ?
                identifier.Substring(vrmlPropertyIdentifier.Length, identifier.Length - vrmlPropertyIdentifier.Length) :
                identifier;
        }
    }

    public class VrmlTexture : VrmlNode
    {
        List<string> m_filenames;

        public VrmlTexture() { }

        public bool RepeatS;
        public bool RepeatT;

        public IEnumerable<string> Filenames
        {
            get { return m_filenames != null ? m_filenames : Enumerable.Empty<string>(); }
            set { m_filenames = new List<string>(value); }
        }

        internal override void Init(SymMapBase map)
        {
            var value = map.Get<object>((Symbol)"url");
            if (value != null)
            {
                IEnumerable<string> filenames;

                if (value is string)
                    filenames = (value as string).IntoIEnumerable();
                else if (value is IEnumerable<string>)
                    filenames = value as IEnumerable<string>;
                else
                    throw new Exception("vrml texture node: invalid url type");

                var path = map.Get<string>((Symbol)"path");
                if (!path.IsNullOrEmpty())
                    filenames = filenames.Select(fp => Path.Combine(path, fp));
                
                m_filenames = new List<string>(filenames);
            }

            RepeatS = map.Get<bool>((Symbol)"repeatS");
            RepeatT = map.Get<bool>((Symbol)"repeatT");
            
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Filenames", (c, o) => c.CodeList_of_String_(ref ((VrmlTexture)o).m_filenames));
        }

        #endregion
    }

    public class VrmlInline : VrmlNode
    {
        public string Url;

        public VrmlInline() { }

        internal override void Init(SymMapBase map)
        {
            var x = map.Get<List<string>>((Symbol)"url");
            Url = x != null ? x.FirstOrDefault() : null;

            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "url", (c, o) => c.CodeString(ref ((VrmlInline)o).Url));
        }

        #endregion
    }

    public class VrmlTextureTransform : VrmlNode
    {
        public V2f Center;
        public float Rotation;
        public V2f Scale;
        public V2f Translation;

        public Trafo2d GetTextureTrafo()
        {
            return VrmlHelpers.BuildVrmlTextureTrafo((V2d)Center, Rotation, (V2d)Scale, (V2d)Translation);
        }

        public VrmlTextureTransform() { }

        internal override void Init(SymMapBase map)
        {
            Center = map.Get<V2f>(Vrml97Sym.center, V2f.Zero);
            Rotation = map.Get<float>(Vrml97Sym.rotation, 0.0f);
            Scale = map.Get<V2f>(Vrml97Sym.scale, V2f.II);
            Translation = map.Get<V2f>(Vrml97Sym.translation, V2f.Zero);
            base.Init(map);
        }

        #region IFieldCodeable Members

        public override IEnumerable<FieldCoder> GetFieldCoders(int coderVersion)
        {
            foreach (var fc in base.GetFieldCoders(coderVersion))
                yield return fc;
            yield return new FieldCoder(2, "Center", (c, o) => c.CodeV2f(ref ((VrmlTextureTransform)o).Center));
            yield return new FieldCoder(3, "Rotation", (c, o) => c.CodeFloat(ref ((VrmlTextureTransform)o).Rotation));
            yield return new FieldCoder(4, "Scale", (c, o) => c.CodeV2f(ref ((VrmlTextureTransform)o).Scale));
            yield return new FieldCoder(5, "Translation", (c, o) => c.CodeV2f(ref ((VrmlTextureTransform)o).Translation));
        }

        #endregion
    }

    public static class VrmlExtensions
    {
        static VrmlNodeComparer s_nodeComparer = new VrmlNodeComparer();

        public class VrmlNodeComparer : IEqualityComparer<VrmlNode>
        {
            #region IEqualityComparer<Shape> Members

            public bool Equals(VrmlNode x, VrmlNode y)
            {
                if (x is VrmlShape && y is VrmlShape)
                    return ((VrmlShape)x).Geometry == ((VrmlShape)y).Geometry;
                return x == y;
            }

            public int GetHashCode(VrmlNode obj)
            {
                if (obj is VrmlShape && ((VrmlShape)obj).Geometry != null)
                    return ((VrmlShape)obj).Geometry.GetHashCode();
                return obj.GetHashCode();
            }

            #endregion
        }

        static public void RemoveDuplicatedChildren(this VrmlGroup self)
        {
            if (self.ChildCount > 1)
            {
                // also check if geometries are duplicated but wraped in non-instanced shapes (materials are ignored)
                var filtered = self.Distinct(s_nodeComparer).ToList();
                var diff = self.Children.Count - filtered.Count;
                if (diff != 0)
                {
                    self.Children = filtered;
                    Report.Warn("removed {0} duplicated child{1} in {2}", diff, diff > 1 ? "ren" : "", self.Name);
                }
            }
        }

        public static void ReplaceChildNodes(this VrmlGroup node, Dictionary<string, VrmlNode> replacements)
        {
            node.DepthFirst(x => x.GetFrames()).ForEach(g => g.Children = g.Select(n =>
            {
                VrmlNode e;
                if (replacements.TryGetValue(n.Name, out e))
                    return e;
                return n;
            }).ToList());
        }

        static public void RemoveUselessGroups(this VrmlGroup self)
        {
            self.Children = self.Select(child =>
                {
                    if (child.GetType() != typeof(VrmlGroup) || child.IsNamed()) return child;
                    var group = (VrmlGroup)child;
                    if (group.ChildCount == 0) return null;
                    return group.ChildCount == 1 ? group.Children.First() : group;
                }).WhereNotNull().ToList();
        }

        static public IEnumerable<VrmlGroup> GetFrames(this VrmlGroup self)
        {
            return self.OfType<VrmlGroup>();
        }

        static public IEnumerable<VrmlShape> GetGeometries(this VrmlGroup self)
        {
            return self.OfType<VrmlShape>();
        }

        static public IEnumerable<VrmlLight> GetLights(this VrmlGroup self)
        {
            return self.OfType<VrmlLight>();
        }

        static public bool IsNamed(this VrmlNode self)
        {
            return !self.Name.IsNullOrEmpty() && !self.Name.StartsWith(self.GetType().Name);
        }
        
        static public VrmlGroup Baked(this VrmlSwitch switchNode)
        {
            var selection = switchNode.SelectedNode;
            var result = new VrmlGroup(switchNode.Name);
            if (selection != null)
                result.Add(selection);

            return result;
        }

        /// <summary>
        /// Performs the switches and places the selected nodes in a group.
        /// </summary>
        static public T WithBakedSwitches<T>(this T vrmlNode, bool removeEmpty = true) where T: VrmlGroup
        {
            VrmlGroup resultGroup = null;

            if (vrmlNode is VrmlSwitch)
            {
                var switchNode = vrmlNode as VrmlSwitch;
                var selection = switchNode.SelectedNode;
                if (!removeEmpty || selection != null)
                {
                    resultGroup = new VrmlGroup(switchNode.Name);
                    resultGroup.Add(selection);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                resultGroup = vrmlNode;
            }

            for (int i = resultGroup.ChildCount - 1; i >= 0; i--)
            {
                var child = resultGroup.Children[i];
                if (child is VrmlGroup)
                    child = ((VrmlGroup)child).WithBakedSwitches();

                if (child == null)
                    resultGroup.Children.RemoveAt(i);
                else
                    resultGroup.Children[i] = child;
            }

            return (resultGroup as T);
        }

        /// <summary>
        /// resolves inline files, uses path to build absolute filenames
        /// </summary>
        public static VrmlNode ResolveInlines(this VrmlNode vrmlNode, string path)
        {
            if (vrmlNode is VrmlGroup)
            {
                var g = ((VrmlGroup)vrmlNode);
                if (g.Children != null)
                {
                    for (int i = 0; i < g.Children.Count; i++)
                        g.Children[i] = g.Children[i].ResolveInlines(path);
                }
            }
            else if (vrmlNode is VrmlInline)
            {
                var file = ((VrmlInline)vrmlNode).Url; // relative filename -> use path build build absolute path

                try
                {
                    var filePath = Path.Combine(path, file);
                    if (File.Exists(filePath))
                        return SceneLoader.Load(filePath);
                    else
                        Report.Warn("could not resolve inline file: \"{0}\"", file);
                }
                catch (Exception e)
                {
                    Report.Warn(e.ToString());
                }

                return new VrmlScene() { Name = file.IsEmptyOrNull() ? vrmlNode.Name : Path.GetFileName(file) };
            }

            return vrmlNode;
        }

        public static void PrimitivesToMeshes(this VrmlNode vrmlNode) {

            if (vrmlNode is VrmlGroup)
            {
                var g = ((VrmlGroup)vrmlNode);
                if (g.ChildCount > 0)
                    g.Children.ForEach(x => x.PrimitivesToMeshes());
            }

            if (vrmlNode is VrmlShape) 
            {
                VrmlShape shape = (VrmlShape)vrmlNode;
                
                if (shape.Geometry is VrmlMesh) return;

                VrmlMesh mesh = new VrmlMesh();
                mesh.Name = shape.Geometry.TrySelect(x => x.Name);

                if (shape.Geometry is VrmlBox)
                {
                    var box = (VrmlBox)shape.Geometry;
                    mesh.Mesh = PolyMeshPrimitives.Box(Box3d.FromCenterAndSize(V3d.Zero, box.Size.XZY), C4b.White);
                }
                else if (shape.Geometry is VrmlSphere)
                {
                    var sphere = (VrmlSphere)shape.Geometry;
                    mesh.Mesh = PolyMeshPrimitives.Sphere(20, sphere.Radius, C4b.White);
                }
                else if (shape.Geometry is VrmlCone)
                {
                    var cone = (VrmlCone)shape.Geometry;
                    mesh.Mesh = PolyMeshPrimitives.Cone(20, cone.Height, cone.BottomRadius, C4b.White);
                }
                else if (shape.Geometry is VrmlCylinder)
                {
                    var cylinder = (VrmlCylinder)shape.Geometry;
                    mesh.Mesh = PolyMeshPrimitives.Cylinder2(20, cylinder.Height, cylinder.Radius, C4b.White, Geometry.PolyMesh.Property.DiffuseColorCoordinates);
                }
                else
                {
                    mesh.Mesh = new Geometry.PolyMesh();
                }

                mesh.Mesh = mesh.Mesh.Transformed(Trafo3d.RotationXInDegrees(-90));

                shape.Geometry = mesh;
            }
        }
    }
}
