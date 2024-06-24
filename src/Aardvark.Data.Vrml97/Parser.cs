using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aardvark.Data.Vrml97
{
    /// <summary>
    /// Symbol table.
    /// </summary>
    public static class Vrml97Sym
    {
#pragma warning disable 1591
        public static readonly Symbol Vrml97 = "Vrml97";
        public static readonly Symbol texture = "texture";
        public static readonly Symbol name = "name";
        public static readonly Symbol title = "title";
        public static readonly Symbol info = "info";
        public static readonly Symbol filename = "filename";
        public static readonly Symbol description = "description";
        public static readonly Symbol parameter = "parameter";
        public static readonly Symbol node = "node";
        public static readonly Symbol root = "root";
        public static readonly Symbol point = "point";
        public static readonly Symbol coord = "coord";
        public static readonly Symbol collide = "collide";
        public static readonly Symbol proxy = "proxy";
        public static readonly Symbol geometry = "geometry";
        public static readonly Symbol appearance = "appearance";
        public static readonly Symbol material = "material";
        public static readonly Symbol image = "image";
        public static readonly Symbol textureTransform = "textureTransform";
        public static readonly Symbol center = "center";
        public static readonly Symbol rotation = "rotation";
        public static readonly Symbol scale = "scale";
        public static readonly Symbol translation = "translation";
        public static readonly Symbol scaleOrientation = "scaleOrientation";
        public static readonly Symbol axisOfRotation = "axisOfRotation";

        public static readonly Symbol ccw = "ccw";
        public static readonly Symbol colorIndex = "colorIndex";
        public static readonly Symbol colorPerVertex = "colorPerVertex";
        public static readonly Symbol convex = "convex";
        public static readonly Symbol coordIndex = "coordIndex";
        public static readonly Symbol creaseAngle = "creaseAngle";
        public static readonly Symbol DEFname = "DEFname";
        public static readonly Symbol normal = "normal";
        public static readonly Symbol normalIndex = "normalIndex";
        public static readonly Symbol normalPerVertex = "normalPerVertex";
        public static readonly Symbol solid = "solid";
        public static readonly Symbol texCoord = "texCoord";
        public static readonly Symbol texCoordIndex = "texCoordIndex";
        public static readonly Symbol transform = "transform";
        public static readonly Symbol vector = "vector";

        public static readonly Symbol edgeSharpness = "edgeSharpness";
        public static readonly Symbol edgeSharpnessIndex = "edgeSharpnessIndex";
        public static readonly Symbol neighborMesh = "neighborMesh";
        public static readonly Symbol neighborIndex = "neighborIndex";
        public static readonly Symbol neighborSide = "neighborSide";
        public static readonly Symbol neighborFace = "neighborFace";
        public static readonly Symbol meshName = "meshName";
        public static readonly Symbol topologyHoles = "topologyHoles";

        public static readonly Symbol xDimension = "xDimension";
        public static readonly Symbol xSpacing = "xSpacing";
        public static readonly Symbol zDimension = "zDimension";
        public static readonly Symbol zSpacing = "zSpacing";

        public static readonly Symbol beginCap = "beginCap";
        public static readonly Symbol crossSection = "crossSection";
        public static readonly Symbol endCap = "endCap";
        public static readonly Symbol orientation = "orientation";
        public static readonly Symbol spine = "spine";

        public static readonly Symbol fogType = "fogType";
        public static readonly Symbol visibilityRange = "visibilityRange";

        public static readonly Symbol bboxCenter = "bboxCenter";
        public static readonly Symbol bboxSize = "bboxSize";

        public static readonly Symbol children = "children";

        public static readonly Symbol size = "size";
        public static readonly Symbol radius = "radius";
        public static readonly Symbol bottomRadius = "bottomRadius";
        public static readonly Symbol height = "height";
        public static readonly Symbol side = "side";
        public static readonly Symbol bottom = "bottom";
        public static readonly Symbol top = "top";

        public static readonly Symbol autoOffset = "autoOffset";
        public static readonly Symbol diskAngle = "diskAngle";
        public static readonly Symbol maxAngle = "maxAngle";
        public static readonly Symbol minAngle = "minAngle";
        public static readonly Symbol offset = "offset";

        public static readonly Symbol groundAngle = "groundAngle";
        public static readonly Symbol groundColor = "groundColor";
        public static readonly Symbol backUrl = "backUrl";
        public static readonly Symbol bottomUrl = "bottomUrl";
        public static readonly Symbol frontUrl = "frontUrl";
        public static readonly Symbol leftUrl = "leftUrl";
        public static readonly Symbol rightUrl = "rightUrl";
        public static readonly Symbol topUrl = "topUrl";
        public static readonly Symbol skyAngle = "skyAngle";
        public static readonly Symbol skyColor = "skyColor";

        public static readonly Symbol level = "level";
        public static readonly Symbol range = "range";

        public static readonly Symbol avatarSize = "avatarSize";
        public static readonly Symbol headlight = "headlight";
        public static readonly Symbol type = "type";
        public static readonly Symbol visibilityLimit = "visibilityLimit";
        
        public static readonly Symbol maxPosition = "maxPosition";
        public static readonly Symbol minPosition = "minPosition";
        public static readonly Symbol maxBack = "maxBack";
        public static readonly Symbol maxFront = "maxFront";
        public static readonly Symbol minBack = "minBack";
        public static readonly Symbol minFront = "minFront";
        public static readonly Symbol priority = "priority";
        public static readonly Symbol source = "source";
        public static readonly Symbol spatialize = "spatialize";
                
        public static readonly Symbol fieldOfView = "fieldOfView";
        public static readonly Symbol jump = "jump";
        public static readonly Symbol position = "position";

        public static readonly Symbol color = "color";
        public static readonly Symbol intensity = "intensity";
        public static readonly Symbol on = "on";
        public static readonly Symbol attenuation = "attenuation";
        public static readonly Symbol beamWidth = "beamWidth";
        public static readonly Symbol cutOffAngle = "cutOffAngle";
        public static readonly Symbol direction = "direction";
        public static readonly Symbol location = "location";

        public static readonly Symbol choice = "choice";
        public static readonly Symbol whichChoice = "whichChoice";

        public static readonly Symbol diffuseColor = "diffuseColor";
        public static readonly Symbol emissiveColor = "emissiveColor";
        public static readonly Symbol ambientIntensity = "ambientIntensity";
        public static readonly Symbol specularColor = "specularColor";
        public static readonly Symbol transparency = "transparency";
        public static readonly Symbol shininess = "shininess";

        public static readonly Symbol key = "key";
        public static readonly Symbol keyValue = "keyValue";

        public static readonly Symbol cycleInterval = "cycleInterval";
        public static readonly Symbol enabled = "enabled";
        public static readonly Symbol loop = "loop";
        public static readonly Symbol speed = "speed";
        public static readonly Symbol startTime = "startTime";
        public static readonly Symbol stopTime = "stopTime";

        public static readonly Symbol pitch = "pitch";

        public static readonly Symbol family = "family";
        public static readonly Symbol horizontal = "horizontal";
        public static readonly Symbol justify = "justify";
        public static readonly Symbol language = "language";
        public static readonly Symbol leftToRight = "leftToRight";
        public static readonly Symbol spacing = "spacing";
        public static readonly Symbol style = "style";
        public static readonly Symbol topToBottom = "topToBottom";

        public static readonly Symbol stringSym = "string";
        public static readonly Symbol fontStyle = "fontStyle";
        public static readonly Symbol length = "length";
        public static readonly Symbol maxExtent = "maxExtent";

        public static readonly Symbol inSym = "in";
        public static readonly Symbol outSym = "out";

        public static readonly Symbol url = "url";
        public static readonly Symbol path = "path";
        public static readonly Symbol repeatS = "repeatS";
        public static readonly Symbol repeatT = "repeatT";

        public static readonly Symbol DEF = "DEF";
        public static readonly Symbol USE = "USE";
        public static readonly Symbol ROUTE = "ROUTE";
        public static readonly Symbol NULL = "NULL";
#pragma warning restore 1591
    }

    /// <summary>
    /// Table of VRML97 node names
    /// </summary>
    public static class Vrml97NodeName
    {
#pragma warning disable 1591
        public static readonly Symbol Anchor = "Anchor";
        public static readonly Symbol Appearance = "Appearance";
        public static readonly Symbol AudioClip = "AudioClip";
        public static readonly Symbol Background = "Background";
        public static readonly Symbol Billboard = "Billboard";
        public static readonly Symbol Box = "Box";
        public static readonly Symbol Collision = "Collision";
        public static readonly Symbol Color = "Color";
        public static readonly Symbol ColorInterpolator = "ColorInterpolator";
        public static readonly Symbol Cone = "Cone";
        public static readonly Symbol Coordinate = "Coordinate";
        public static readonly Symbol CoordinateInterpolator = "CoordinateInterpolator";
        public static readonly Symbol Cylinder = "Cylinder";
        public static readonly Symbol CylinderSensor = "CylinderSensor";
        public static readonly Symbol DirectionalLight = "DirectionalLight";
        public static readonly Symbol ElevationGrid = "DirectionalLight";
        public static readonly Symbol Extrusion = "DirectionalLight";
        public static readonly Symbol Fog = "Fog";
        public static readonly Symbol FontStyle = "FontStyle";
        public static readonly Symbol Group = "Group";
        public static readonly Symbol ImageTexture = "ImageTexture";
        public static readonly Symbol IndexedFaceSet = "IndexedFaceSet";
        public static readonly Symbol IndexedLineSet = "IndexedLineSet";
        public static readonly Symbol Inline = "Inline";
        public static readonly Symbol LOD = "LOD";
        public static readonly Symbol Material = "Material";
        public static readonly Symbol MovieTexture = "MovieTexture";
        public static readonly Symbol NavigationInfo = "NavigationInfo";
        public static readonly Symbol Normal = "Normal";
        public static readonly Symbol NormalInterpolator = "NormalInterpolator";
        public static readonly Symbol OrientationInterpolator = "OrientationInterpolator";
        public static readonly Symbol PixelTexture = "PixelTexture";
        public static readonly Symbol PlaneSensor = "PlaneSensor";
        public static readonly Symbol PointLight = "PointLight";
        public static readonly Symbol PointSet = "PointSet";
        public static readonly Symbol PositionInterpolator = "PositionInterpolator";
        public static readonly Symbol ProximitySensor = "ProximitySensor";
        public static readonly Symbol ScalarInterpolator = "ScalarInterpolator";
        public static readonly Symbol Script = "Script";
        public static readonly Symbol Shape = "Shape";
        public static readonly Symbol Sound = "Sound";
        public static readonly Symbol Sphere = "Sphere";
        public static readonly Symbol SphereSensor = "SphereSensor";
        public static readonly Symbol SpotLight = "SpotLight";
        public static readonly Symbol Switch = "Switch";
        public static readonly Symbol Text = "Text";
        public static readonly Symbol TextureCoordinate = "TextureCoordinate";
        public static readonly Symbol TextureTransform = "TextureTransform";
        public static readonly Symbol TimeSensor = "TimeSensor";
        public static readonly Symbol TouchSensor = "TTouchSensorext";
        public static readonly Symbol Transform = "Transform";
        public static readonly Symbol Viewpoint = "Viewpoint";
        public static readonly Symbol VisibilitySensor = "VisibilitySensor";
        public static readonly Symbol WorldInfo = "WorldInfo";
#pragma warning restore 1591
    }

    /// <summary>
    /// Vrml97 parser.
    /// </summary>
    public static class Parser
    {
        internal class State
        {
            SymMapBase m_result = new SymMapBase();
            Tokenizer m_tokenizer;

            /// <summary>
            /// Constructs a Parser for the given input stream.
            /// In order to actually parse the data, call the
            /// Perform method, which returns a SymMapBase containing
            /// the parse tree.
            /// </summary>
            /// <param name="input">Input stream.</param>
            /// <param name="fileName">FileName</param>
            public State(Stream input, string fileName)
            {
                m_result.TypeName = Vrml97Sym.Vrml97;
                m_result[Vrml97Sym.filename] = fileName;
                m_tokenizer = new Tokenizer(input);
            }

            /// <summary>
            /// Parses the input data and returns a SymMapBase
            /// containing the parse tree.
            /// </summary>
            /// <returns>Parse tree.</returns>
            public Vrml97Scene Perform()
            {
                var root = new List<SymMapBase>();

                while (true)
                {
                    try
                    {
                        var node = ParseNode(m_tokenizer);
                        if (node == null) break;
                        root.Add(node);
                        Thread.Sleep(0);
                    }
                    catch (ParseException e)
                    {
                        Report.Warn("[Vrml97] Caught exception while parsing: {0}!", e.Message);
                        Report.Warn("[Vrml97] Result may contain partial, incorrect or invalid data!");
                        break;
                    }
                }

                m_result[Vrml97Sym.root] = root;
                return new Vrml97Scene(m_result);
            }
        }

        #region Node specs.

        private static SymbolDict<NodeParseInfo> s_parseInfoMap;

        private delegate SymMapBase NodeParser(Tokenizer t);

        public delegate object FieldParser(Tokenizer t);

        public static readonly FieldParser SFBool = new FieldParser(ParseSFBool);
        public static readonly FieldParser MFBool = new FieldParser(ParseMFBool);
        public static readonly FieldParser SFColor = new FieldParser(ParseSFColor);
        public static readonly FieldParser MFColor = new FieldParser(ParseMFColor);
        public static readonly FieldParser SFFloat = new FieldParser(ParseSFFloat);
        public static readonly FieldParser MFFloat = new FieldParser(ParseMFFloat);
        public static readonly FieldParser SFImage = new FieldParser(ParseSFImage);
        public static readonly FieldParser SFInt32 = new FieldParser(ParseSFInt32);
        public static readonly FieldParser MFInt32 = new FieldParser(ParseMFInt32);
        public static readonly FieldParser SFNode = new FieldParser(ParseSFNode);
        public static readonly FieldParser MFNode = new FieldParser(ParseMFNode);
        public static readonly FieldParser SFRotation = new FieldParser(ParseSFRotation);
        public static readonly FieldParser MFRotation = new FieldParser(ParseMFRotation);
        public static readonly FieldParser SFString = new FieldParser(ParseSFString);
        public static readonly FieldParser MFString = new FieldParser(ParseMFString);
        public static readonly FieldParser SFTime = new FieldParser(ParseSFFloat);
        public static readonly FieldParser MFTime = new FieldParser(ParseMFFloat);
        public static readonly FieldParser SFVec2f = new FieldParser(ParseSFVec2f);
        public static readonly FieldParser MFVec2f = new FieldParser(ParseMFVec2f);
        public static readonly FieldParser SFVec3f = new FieldParser(ParseSFVec3f);
        public static readonly FieldParser MFVec3f = new FieldParser(ParseMFVec3f);

        /// <summary>
        /// Registers a custom node attribute field parser.
        /// Usage: RegisterCustomNodeField(Vrml97NodeName.Material, "doubleSided", Parser.SFBool, false);
        /// </summary>
        public static void RegisterCustomNodeField(Symbol nodeName, Symbol fieldName, FieldParser parser, object defaultValue)
        {
            if (!s_parseInfoMap.TryGetValue(nodeName, out var npi))
                throw new ArgumentException($"Failed to register \"{fieldName}\" as custom \"{nodeName}\" node field: The node name is not registered!");
            if (npi.FieldDefs == null)
                throw new ArgumentException($"Failed to register \"{fieldName}\" as custom \"{nodeName}\" node field: The node does not have FieldDefs!");
            npi.FieldDefs.Add(fieldName, (parser, defaultValue));
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Parser()
        {
            // Lookup table for Vrml97 node types.
            // For each node type a NodeParseInfo entry specifies how
            // to handle this kind of node.
            s_parseInfoMap = new SymbolDict<NodeParseInfo>
            {
                // DEF
                [Vrml97Sym.DEF] = new NodeParseInfo(new NodeParser(ParseDEF)),

                // USE
                [Vrml97Sym.USE] = new NodeParseInfo(new NodeParser(ParseUSE)),

                // ROUTE
                [Vrml97Sym.ROUTE] = new NodeParseInfo(new NodeParser(ParseROUTE)),

                // NULL
                [Vrml97Sym.NULL] = new NodeParseInfo(new NodeParser(ParseNULL))
            };

            var defaultBBoxCenter = (SFVec3f, (object)V3f.Zero);
            var defaultBBoxSize = (SFVec3f, (object)new V3f(-1, -1, -1));

            (FieldParser, object) fd(FieldParser fp) => (fp, null); // helper to create (FieldParser, <defaultValue>) tuple with null as default value

            // Anchor
            s_parseInfoMap[Vrml97NodeName.Anchor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.children, fd(MFNode) },
                    { Vrml97Sym.description, fd(SFString) },
                    { Vrml97Sym.parameter, fd(MFString) },
                    { Vrml97Sym.url, fd(MFString) },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter},
                    { Vrml97Sym.bboxSize, defaultBBoxSize}
                });

            // Appearance
            s_parseInfoMap[Vrml97NodeName.Appearance] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.material, fd(SFNode) },
                    { Vrml97Sym.texture, fd(SFNode) },
                    { Vrml97Sym.textureTransform, fd(SFNode) }
                });

            // AudioClip
            s_parseInfoMap[Vrml97NodeName.AudioClip] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.description, fd(SFString) },
                    { Vrml97Sym.loop, (SFBool, false) },
                    { Vrml97Sym.pitch, (SFFloat, 1.0f) },
                    { Vrml97Sym.startTime, (SFTime, 0.0f)},
                    { Vrml97Sym.stopTime, (SFTime, 0.0f)},
                    { Vrml97Sym.url, fd(MFString)}
                });

            // Background
            s_parseInfoMap[Vrml97NodeName.Background] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.groundAngle, fd(MFFloat) },
                    { Vrml97Sym.groundColor, fd(MFColor) },
                    { Vrml97Sym.backUrl, fd(MFString) },
                    { Vrml97Sym.bottomUrl, fd(MFString) },
                    { Vrml97Sym.frontUrl, fd(MFString) },
                    { Vrml97Sym.leftUrl, fd(MFString) },
                    { Vrml97Sym.rightUrl, fd(MFString) },
                    { Vrml97Sym.topUrl, fd(MFString) },
                    { Vrml97Sym.skyAngle, fd(MFFloat) },
                    { Vrml97Sym.skyColor, (MFColor, C3f.Black) }
                });

            // Billboard
            s_parseInfoMap[Vrml97NodeName.Billboard] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.axisOfRotation, (SFVec3f, new V3f(0.0f, 1.0f, 0.0f)) },
                    { Vrml97Sym.children, fd(MFNode) },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter},
                    { Vrml97Sym.bboxSize, defaultBBoxSize}
                });
            
            // Box
            s_parseInfoMap[Vrml97NodeName.Box] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.size, (SFVec3f, new V3f(2.0f, 2.0f, 2.0f)) }
                });

            // Collision
            s_parseInfoMap[Vrml97NodeName.Collision] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.children, fd(MFNode) },
                    { Vrml97Sym.collide, (SFBool, true) },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter},
                    { Vrml97Sym.bboxSize, defaultBBoxSize},
                    { Vrml97Sym.proxy, fd(SFNode) }
                });

            // Color
            s_parseInfoMap[Vrml97NodeName.Color] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, fd(MFColor) }
                });

            // ColorInterpolator
            s_parseInfoMap[Vrml97NodeName.ColorInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFColor) }
                });

            // Cone
            s_parseInfoMap[Vrml97NodeName.Cone] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.bottomRadius, (SFFloat, 1.0f) },
                    { Vrml97Sym.height, (SFFloat, 2.0f) },
                    { Vrml97Sym.side, (SFBool, true) },
                    { Vrml97Sym.bottom, (SFBool, true) }
                });

            // Coordinate
            s_parseInfoMap[Vrml97NodeName.Coordinate] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.point, fd(MFVec3f) }
                });

            // CoordinateInterpolator
            s_parseInfoMap[Vrml97NodeName.CoordinateInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFVec3f) }
                });

            // Cylinder
            s_parseInfoMap[Vrml97NodeName.Cylinder] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.bottom, (SFBool, true) },
                    { Vrml97Sym.height, (SFFloat, 2.0f) },
                    { Vrml97Sym.radius, (SFFloat, 1.0f) },
                    { Vrml97Sym.side, (SFBool, true) },
                    { Vrml97Sym.top, (SFBool, true) }
                });

            // CylinderSensor
            s_parseInfoMap[Vrml97NodeName.CylinderSensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.autoOffset, (SFBool, true) },
                    { Vrml97Sym.diskAngle, (SFFloat, 0.262f) },
                    { Vrml97Sym.enabled, (SFBool, true) },
                    { Vrml97Sym.maxAngle, (SFFloat, -1.0f) },
                    { Vrml97Sym.minAngle, (SFFloat, 0.0f) },
                    { Vrml97Sym.offset, (SFFloat, 0.0f) }
                });

            // DirectionalLight
            s_parseInfoMap[Vrml97NodeName.DirectionalLight] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.ambientIntensity, (SFFloat, 0.0f) },
                    { Vrml97Sym.color, (SFColor, C3f.White) },
                    { Vrml97Sym.direction, (SFVec3f, new V3f(0.0f, 0.0f, -1.0f)) },
                    { Vrml97Sym.intensity, (SFFloat, 1.0f) },
                    { Vrml97Sym.on, (SFBool, true) }
                });

            // ElevationGrid
            s_parseInfoMap[Vrml97NodeName.ElevationGrid] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, fd(SFNode) },
                    { Vrml97Sym.normal, fd(SFNode) },
                    { Vrml97Sym.texCoord, fd(SFNode) },
                    { Vrml97Sym.height, fd(MFFloat) },
                    { Vrml97Sym.ccw, (SFBool, true) },
                    { Vrml97Sym.colorPerVertex, (SFBool, true) },
                    { Vrml97Sym.creaseAngle, (SFFloat, 0.0f) },
                    { Vrml97Sym.normalPerVertex, (SFBool, true) },
                    { Vrml97Sym.solid, (SFBool, true) },
                    { Vrml97Sym.xDimension, (SFInt32, 0) },
                    { Vrml97Sym.xSpacing, (SFFloat, 1.0f) },
                    { Vrml97Sym.zDimension, (SFInt32, 0) },
                    { Vrml97Sym.zSpacing, (SFFloat, 1.0f) }
                });
     
            // Extrusion
            s_parseInfoMap[Vrml97NodeName.Extrusion] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.beginCap, (SFBool, true) },
                    { Vrml97Sym.ccw, (SFBool, true) },
                    { Vrml97Sym.convex, (SFBool, true) },
                    { Vrml97Sym.creaseAngle, (SFFloat, 0.0f) },
                    { Vrml97Sym.crossSection, (MFVec2f, new List<V2f>() {new V2f(1.0f, 1.0f), new V2f(1.0f, -1.0f), new V2f(-1.0f, -1.0f), new V2f(-1.0f, 1.0f), new V2f(1.0f, 1.0f) }) },
                    { Vrml97Sym.endCap, (SFBool, true) },
                    { Vrml97Sym.orientation, (MFRotation, new V4f(0.0f, 0.0f, 1.0f, 0.0f)) },
                    { Vrml97Sym.scale, (MFVec2f, new V2f(1.0f, 1.0f)) },
                    { Vrml97Sym.solid, (SFBool, true) },
                    { Vrml97Sym.spine, (MFVec3f, new List<V3f>() { V3f.Zero, new V3f(0.0f, 1.0f, 0.0f) }) }
                });

            // Fog
            s_parseInfoMap[Vrml97NodeName.Fog] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, (SFColor, C3f.White) },
                    { Vrml97Sym.fogType, (SFString, "LINEAR") },
                    { Vrml97Sym.visibilityRange, (SFFloat, 0.0f) }
                });

            // FontStyle
            s_parseInfoMap[Vrml97NodeName.FontStyle] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.family, (MFString, "SERIF") },
                    { Vrml97Sym.horizontal, (SFBool, true) },
                    { Vrml97Sym.justify, (MFString, "BEGIN") },
                    { Vrml97Sym.language, fd(SFString) },
                    { Vrml97Sym.leftToRight, (SFBool, true) },
                    { Vrml97Sym.size, (SFFloat, 1.0f) },
                    { Vrml97Sym.spacing, (SFFloat, 1.0f) },
                    { Vrml97Sym.style, (SFString, "PLAIN") },
                    { Vrml97Sym.topToBottom, (SFBool, true) }
                });

            // Group
            s_parseInfoMap[Vrml97NodeName.Group] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.children, fd(MFNode) },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter },
                    { Vrml97Sym.bboxSize, defaultBBoxSize }
                });

            // ImageTexture
            s_parseInfoMap[Vrml97NodeName.ImageTexture] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.url, fd(MFString) },
                    { Vrml97Sym.repeatS, (SFBool, true) },
                    { Vrml97Sym.repeatT, (SFBool, true) }
                });

            // IndexedFaceSet
            s_parseInfoMap[Vrml97NodeName.IndexedFaceSet] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, fd(SFNode) },
                    { Vrml97Sym.coord, fd(SFNode) },
                    { Vrml97Sym.normal, fd(SFNode) },
                    { Vrml97Sym.texCoord, fd(SFNode) },
                    { Vrml97Sym.ccw, (SFBool, true) },
                    { Vrml97Sym.colorIndex, fd(MFInt32) },
                    { Vrml97Sym.colorPerVertex, (SFBool, true) },
                    { Vrml97Sym.convex, (SFBool, true) },
                    { Vrml97Sym.coordIndex, fd(MFInt32) },
                    { Vrml97Sym.creaseAngle, (SFFloat, 0.0f) },
                    { Vrml97Sym.normalIndex, fd(MFInt32) },
                    { Vrml97Sym.normalPerVertex, (SFBool, true) },
                    { Vrml97Sym.solid, (SFBool, true) },
                    { Vrml97Sym.texCoordIndex, fd(MFInt32) },  
                    // NOTE: the following attributes are not found in the spec ???
                    { Vrml97Sym.edgeSharpness, fd(MFFloat) },
                    { Vrml97Sym.edgeSharpnessIndex, fd(MFInt32) },
                    { Vrml97Sym.neighborMesh, fd(MFString) },
                    { Vrml97Sym.neighborIndex, fd(MFInt32) },
                    { Vrml97Sym.neighborSide, fd(MFInt32) },
                    { Vrml97Sym.neighborFace, fd(MFInt32) },
                    { Vrml97Sym.meshName, fd(SFString) },
                    { Vrml97Sym.topologyHoles, fd(SFInt32) }
                });

            // IndexedLineSet
            s_parseInfoMap[Vrml97NodeName.IndexedLineSet] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, fd(SFNode) },
                    { Vrml97Sym.coord, fd(SFNode) },
                    { Vrml97Sym.colorIndex, fd(MFInt32) },
                    { Vrml97Sym.colorPerVertex, (SFBool, true) },
                    { Vrml97Sym.coordIndex, fd(MFInt32) }
                });

            // Inline
            s_parseInfoMap[Vrml97NodeName.Inline] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.url, fd(MFString) },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter },
                    { Vrml97Sym.bboxSize, defaultBBoxSize }
                });

            // LOD
            s_parseInfoMap[Vrml97NodeName.LOD] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.level, fd(MFNode) },
                    { Vrml97Sym.center, defaultBBoxCenter },
                    { Vrml97Sym.range, fd(MFFloat) }
                });

            // Material
            s_parseInfoMap[Vrml97NodeName.Material] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.ambientIntensity, (SFFloat, 0.2f) },
                    { Vrml97Sym.diffuseColor, (SFColor, new C3f(0.8f, 0.8f, 0.8f)) },
                    { Vrml97Sym.emissiveColor, (SFColor, C3f.Black) },
                    { Vrml97Sym.shininess, (SFFloat, 0.2f) },
                    { Vrml97Sym.specularColor, (SFColor, C3f.Black) },
                    { Vrml97Sym.transparency, (SFFloat, 0.0f) }
                });

            // MovieTexture
            s_parseInfoMap[Vrml97NodeName.MovieTexture] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.loop, (SFBool, false) },
                    { Vrml97Sym.speed, (SFFloat, 1.0f) },
                    { Vrml97Sym.startTime, (SFTime, 1.0f) },
                    { Vrml97Sym.stopTime, (SFTime, 1.0f) },
                    { Vrml97Sym.url, fd(MFString) },
                    { Vrml97Sym.repeatS, (SFBool, true) },
                    { Vrml97Sym.repeatT, (SFBool, true) }
                });

            // NavigationInfo
            s_parseInfoMap[Vrml97NodeName.NavigationInfo] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.avatarSize, (MFFloat, new List<float>() {0.25f, 1.6f, 0.75f}) },
                    { Vrml97Sym.headlight, (SFBool, true) },
                    { Vrml97Sym.speed, (SFFloat, 1.0f) },
                    { Vrml97Sym.type, (MFString, new List<string>() {"WALK", "ANY"}) },
                    { Vrml97Sym.visibilityLimit, (SFFloat, 0.0f) }
                });

            // Normal
            s_parseInfoMap[Vrml97NodeName.Normal] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.vector, fd(MFVec3f) }
                });

            // NormalInterpolator
            s_parseInfoMap[Vrml97NodeName.NormalInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFVec3f) }
                });

            // OrientationInterpolator
            s_parseInfoMap[Vrml97NodeName.OrientationInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFRotation) }
                });

            // PixelTexture
            s_parseInfoMap[Vrml97NodeName.PixelTexture] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.image, (SFImage, new List<uint>() {0, 0, 0}) },
                    { Vrml97Sym.repeatS, (SFBool, true) },
                    { Vrml97Sym.repeatT, (SFBool, true) }
                });

            // PlaneSensor
            s_parseInfoMap[Vrml97NodeName.PlaneSensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.autoOffset, (SFBool, true) },
                    { Vrml97Sym.enabled, (SFBool, true) },
                    { Vrml97Sym.maxPosition, (SFVec2f, new V2f(-1.0f, -1.0f)) },
                    { Vrml97Sym.minPosition, (SFVec2f, V2f.Zero) },
                    { Vrml97Sym.offset, defaultBBoxCenter }
                });

            // PointLight
            s_parseInfoMap[Vrml97NodeName.PointLight] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.ambientIntensity, (SFFloat, 0.0f) },
                    { Vrml97Sym.attenuation, (SFVec3f, new V3f(1.0f, 0.0f, 0.0f)) },
                    { Vrml97Sym.color, (SFColor, C3f.White) },
                    { Vrml97Sym.intensity, (SFFloat, 1.0f) },
                    { Vrml97Sym.location, defaultBBoxCenter },
                    { Vrml97Sym.on, (SFBool, true) },
                    { Vrml97Sym.radius, (SFFloat, 100.0f) }
                });

            // PointSet
            s_parseInfoMap[Vrml97NodeName.PointSet] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.color, fd(SFNode) },
                    { Vrml97Sym.coord, fd(SFNode) }
                });

            // PositionInterpolator
            s_parseInfoMap[Vrml97NodeName.PositionInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFVec3f) }
                });

            // ProximitySensor
            s_parseInfoMap[Vrml97NodeName.ProximitySensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.center, defaultBBoxCenter },
                    { Vrml97Sym.size, defaultBBoxCenter },
                    { Vrml97Sym.enabled, (SFBool, true) }
                });

            // ScalarInterpolator
            s_parseInfoMap[Vrml97NodeName.ScalarInterpolator] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.key, fd(MFFloat) },
                    { Vrml97Sym.keyValue, fd(MFFloat) }
                });

            // Script
            // skipped

            // Shape
            s_parseInfoMap[Vrml97NodeName.Shape] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.appearance, fd(SFNode) },
                    { Vrml97Sym.geometry, fd(SFNode) },
                });

            // Sound
            s_parseInfoMap[Vrml97NodeName.Sound] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.direction, (SFVec3f, new V3f(0.0f, 0.0f, 1.0f)) },
                    { Vrml97Sym.intensity, (SFFloat, 1.0f) },
                    { Vrml97Sym.location, defaultBBoxCenter },
                    { Vrml97Sym.maxBack, (SFFloat, 10.0f) },
                    { Vrml97Sym.maxFront, (SFFloat, 10.0f) },
                    { Vrml97Sym.minBack, (SFFloat, 1.0f) },
                    { Vrml97Sym.minFront, (SFFloat, 1.0f) },
                    { Vrml97Sym.priority, (SFFloat, 0.0f) },
                    { Vrml97Sym.source, fd(SFNode) },
                    { Vrml97Sym.spatialize, (SFBool, true) }
                });

            // Sphere
            s_parseInfoMap[Vrml97NodeName.Sphere] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.radius, (SFFloat, 1.0f) }
                });

            // SphereSensor
            s_parseInfoMap[Vrml97NodeName.SphereSensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.autoOffset, (SFBool, true) },
                    { Vrml97Sym.enabled, (SFBool, true) },
                    { Vrml97Sym.offset, (SFRotation, new V4f(0.0f, 1.0f, 0.0f, 0.0f)) }
                });

            // SpotLight
            s_parseInfoMap[Vrml97NodeName.SpotLight] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.ambientIntensity, (SFFloat, 0.0f) },
                    { Vrml97Sym.attenuation, (SFVec3f, new V3f(1.0f, 0.0f, 0.0f)) },
                    { Vrml97Sym.beamWidth, (SFFloat, 1.570796f) },
                    { Vrml97Sym.color, (SFColor, C3f.White) },
                    { Vrml97Sym.cutOffAngle, (SFFloat, 0.785398f) },
                    { Vrml97Sym.direction, (SFVec3f, new V3f(0.0f, 0.0f, -1.0f)) },
                    { Vrml97Sym.intensity, (SFFloat, 1.0f) },
                    { Vrml97Sym.location, defaultBBoxCenter },
                    { Vrml97Sym.on, (SFBool, true) },
                    { Vrml97Sym.radius, (SFFloat, 100.0f) }
                });

            // Switch
            s_parseInfoMap[Vrml97NodeName.Switch] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.choice, fd(MFNode) },
                    { Vrml97Sym.whichChoice, (SFInt32, -1) }
                });

            // Text
            s_parseInfoMap[Vrml97NodeName.Text] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.stringSym, fd(MFString) },
                    { Vrml97Sym.fontStyle, fd(SFNode) },
                    { Vrml97Sym.length, fd(MFFloat) },
                    { Vrml97Sym.maxExtent, (SFFloat, 0.0f) }
                });

            // TextureCoordinate
            s_parseInfoMap[Vrml97NodeName.TextureCoordinate] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.point, fd(MFVec2f) }
                });

            // TextureTransform
            s_parseInfoMap[Vrml97NodeName.TextureTransform] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.center, (SFVec2f, V2f.Zero) },
                    { Vrml97Sym.rotation, (SFFloat, 0.0f) },
                    { Vrml97Sym.scale, (SFVec2f, new V2f(1.0f, 1.0f)) },
                    { Vrml97Sym.translation, (SFVec2f, V2f.Zero) }
                });

            // TimeSensor
            s_parseInfoMap[Vrml97NodeName.TimeSensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.cycleInterval, (SFTime, 1.0f) },
                    { Vrml97Sym.enabled, (SFBool, true) },
                    { Vrml97Sym.loop, (SFBool, false) },
                    { Vrml97Sym.startTime, (SFTime, 0.0f) },
                    { Vrml97Sym.stopTime, (SFTime, 0.0f) }
                });

            // TouchSensor
            s_parseInfoMap[Vrml97NodeName.TouchSensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.enabled, (SFBool, true) }
                });

            // Transform
            s_parseInfoMap[Vrml97NodeName.Transform] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.center, defaultBBoxCenter },
                    { Vrml97Sym.children, fd(MFNode) },
                    { Vrml97Sym.rotation, (SFRotation, new V4f(0.0f, 0.0f, 1.0f, 0.0f)) },
                    { Vrml97Sym.scale, (SFVec3f, new V3f(1.0f, 1.0f, 1.0f)) },
                    { Vrml97Sym.scaleOrientation, (SFRotation, new V4f(0.0f, 0.0f, 1.0f, 0.0f)) },
                    { Vrml97Sym.translation, defaultBBoxCenter },
                    { Vrml97Sym.bboxCenter, defaultBBoxCenter },
                    { Vrml97Sym.bboxSize, defaultBBoxSize }
                });

            // Viewpoint
            s_parseInfoMap[Vrml97NodeName.Viewpoint] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.fieldOfView, (SFFloat, 0.785398f) },
                    { Vrml97Sym.jump, (SFBool, true) },
                    { Vrml97Sym.orientation, (SFRotation, new V4f(0.0f, 0.0f, 1.0f, 0.0f)) },
                    { Vrml97Sym.position, (SFVec3f, new V3f(0.0f, 0.0f, 10.0f)) },
                    { Vrml97Sym.description, fd(SFString) }
                });

            // VisibilitySensor
            s_parseInfoMap[Vrml97NodeName.VisibilitySensor] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.center, defaultBBoxCenter },
                    { Vrml97Sym.enabled, (SFBool, true) },
                    { Vrml97Sym.size, defaultBBoxCenter }
                });

            // WorldInfo
            s_parseInfoMap[Vrml97NodeName.WorldInfo] = new NodeParseInfo(
                new SymbolDict<(FieldParser, object)>()
                {
                    { Vrml97Sym.title, fd(SFString) },
                    { Vrml97Sym.info, fd(MFString) }
                });
        }

        private static SymMapBase ParseDEF(Tokenizer t)
        {
            var result = new SymMapBase();
            result[Vrml97Sym.name] = t.NextNameToken().ToString();
            result[Vrml97Sym.node] = ParseNode(t);
            return result;
        }

        private static SymMapBase ParseUSE(Tokenizer t)
        {
            var result = new SymMapBase();
            result[Vrml97Sym.name] = t.NextNameToken().ToString();
            return result;
        }

        private static SymMapBase ParseROUTE(Tokenizer t)
        {
            var result = new SymMapBase();

            // nodeNameId.eventOutId
            result[Vrml97Sym.outSym] = t.NextNameToken().ToString();
            // "TO"
            t.NextToken();
            // nodeNameId.eventInId
            result[Vrml97Sym.inSym] = t.NextNameToken().ToString();

            return result;
        }

        private static SymMapBase ParseNULL(Tokenizer t) => null;

        #endregion

        #region Helper functions.

        private static object ParseSFBool(Tokenizer t) => t.NextToken().ToBool();

        private static List<bool> ParseMFBool(Tokenizer t)
        {
            var result = new List<bool>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    result.Add(token.ToBool());
                    token = t.NextToken();
                }
            }
            else
            {
                result.Add(token.ToBool());
            }

            return result;
        }

        private static object ParseSFFloat(Tokenizer t) => t.NextToken().ToFloat();

        private static List<float> ParseMFFloat(Tokenizer t)
        {
            var result = new List<float>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    result.Add(token.ToFloat());
                    token = t.NextToken();
                }
            }
            else
            {
                result.Add(token.ToFloat());
            }

            return result;
        }

        private static List<uint> ParseSFImage(Tokenizer t)
        {
            var result = new List<uint>
            {
                t.NextToken().ToUInt32(),   // width
                t.NextToken().ToUInt32(),   // height
                t.NextToken().ToUInt32()   // num components
            };

            uint imax = result[0] * result[1];
            for (uint i = 0; i < imax; i++)
            {
                result.Add(t.NextToken().ToUInt32());
            }

            return result;
        }

        private static object ParseSFInt32(Tokenizer t) => t.NextToken().ToInt32();

        private static List<int> ParseMFInt32(Tokenizer t)
        {
            var result = new List<int>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    result.Add(token.ToInt32());
                    token = t.NextToken();
                }
            }
            else
            {
                result.Add(token.ToInt32());
            }

            return result;
        }

        private static SymMapBase ParseSFNode(Tokenizer t) => ParseNode(t);

        private static List<SymMapBase> ParseMFNode(Tokenizer t)
        {
            var result = new List<SymMapBase>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    t.PushBack(token);
                    result.Add(ParseNode(t));
                    token = t.NextToken();
                }
            }
            else
            {
                t.PushBack(token);
                result.Add(ParseNode(t));
            }

            return result;
        }

        private static object ParseSFRotation(Tokenizer t)
        {
            var x = t.NextToken().ToFloat();
            var y = t.NextToken().ToFloat();
            var z = t.NextToken().ToFloat();
            var w = t.NextToken().ToFloat();
            return new V4f(x, y, z, w);
        }

        private static List<V4f> ParseMFRotation(Tokenizer t)
        {
            var result = new List<V4f>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    var x = token.ToFloat();
                    var y = t.NextToken().ToFloat();
                    var z = t.NextToken().ToFloat();
                    var w = t.NextToken().ToFloat();
                    result.Add(new V4f(x, y, z, w));

                    token = t.NextToken();
                }
            }
            else
            {
                var x = token.ToFloat();
                var y = t.NextToken().ToFloat();
                var z = t.NextToken().ToFloat();
                var w = t.NextToken().ToFloat();
                result.Add(new V4f(x, y, z, w));
            }

            return result;
        }

        private static string ParseSFString(Tokenizer t)
            => t.NextToken().GetCheckedUnquotedString();

        private static List<string> ParseMFString(Tokenizer t)
        {
            var result = new List<string>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    result.Add(token.GetCheckedUnquotedString());
                    token = t.NextToken();
                }
            }
            else
            {
                result.Add(token.GetCheckedUnquotedString());
            }

            return result;
        }

        private static object ParseSFVec2f(Tokenizer t)
        {
            var x = t.NextToken().ToFloat();
            var y = t.NextToken().ToFloat();
            return new V2f(x, y);
        }

        private static List<V2f> ParseMFVec2f(Tokenizer t)
        {
            var result = new List<V2f>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    float x = token.ToFloat();
                    float y = t.NextToken().ToFloat();
                    result.Add(new V2f(x, y));

                    token = t.NextToken();
                }
            }
            else
            {
                float x = token.ToFloat();
                float y = t.NextToken().ToFloat();
                result.Add(new V2f(x, y));
            }

            return result;
        }

        private static object ParseSFVec3f(Tokenizer t)
        {
            var x = t.NextToken().ToFloat();
            var y = t.NextToken().ToFloat();
            var z = t.NextToken().ToFloat();
            return new V3f(x, y, z);
        }

        private static List<V3f> ParseMFVec3f(Tokenizer t)
        {
            var result = new List<V3f>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    var x = token.ToFloat();
                    var y = t.NextToken().ToFloat();
                    var z = t.NextToken().ToFloat();
                    result.Add(new V3f(x, y, z));

                    token = t.NextToken();
                }
            }
            else
            {
                var x = token.ToFloat();
                var y = t.NextToken().ToFloat();
                var z = t.NextToken().ToFloat();
                result.Add(new V3f(x, y, z));
            }

            return result;
        }

        private static object ParseSFColor(Tokenizer t)
        {
            var r = t.NextToken().ToFloat();
            var g = t.NextToken().ToFloat();
            var b = t.NextToken().ToFloat();
            return new C3f(r, g, b);
        }

        private static List<C3f> ParseMFColor(Tokenizer t)
        {
            var result = new List<C3f>();

            var token = t.NextToken();
            if (token.IsBracketOpen)
            {
                token = t.NextToken();
                while (!token.IsBracketClose)
                {
                    var r = token.ToFloat();
                    var g = t.NextToken().ToFloat();
                    var b = t.NextToken().ToFloat();
                    result.Add(new C3f(r, g, b));

                    token = t.NextToken();
                }
            }
            else
            {
                var r = token.ToFloat();
                var g = t.NextToken().ToFloat();
                var b = t.NextToken().ToFloat();
                result.Add(new C3f(r, g, b));
            }

            return result;
        }

        private static void ExpectBraceOpen(Tokenizer t)
        {
            var token = t.NextToken();
            if (token.IsBraceOpen) return;

            throw new ParseException(
                "Token '{' expected. Found " + token.ToString() + " instead!"
                );
        }

        private static void ExpectBraceClose(Tokenizer t)
        {
            var token = t.NextToken();
            if (token.IsBraceClose) return;

            throw new ParseException(
                "Token '}' expected. Found " + token.ToString() + " instead!"
                );
        }

        #endregion

        #region Internal stuff.

        private static SymMapBase ParseNode(Tokenizer t)
        {
            // Next token is expected to be a Vrml97 node type.
            var nodeType = t.NextToken().ToString();
            if (nodeType == null) return null;

            SymMapBase node;

            // If a field description is available for this type,
            // then use the generic node parser, else use the custom
            // parse function.
            if (s_parseInfoMap.TryGetValue(nodeType, out var info))
            {
                node = (info.FieldDefs == null) ?
                    info.NodeParser(t) :
                    ParseGenericNode(t, info);
            }
            else
            {
                // unknown node type
                Report.Warn($"[Vrml97] ParseNode: \"{nodeType}\" unknown node type!");
                node = ParseUnknownNode(t);
            }

            if (node != null)
                node.TypeName = nodeType;

            return node;
        }

        /// <summary>
        /// Specifies how to parse a node.
        /// </summary>
        private struct NodeParseInfo
        {
            private NodeParser m_parseFunction;
            public readonly SymbolDict<(FieldParser, object)> FieldDefs;

            public NodeParseInfo(NodeParser parseFunction)
                : this(parseFunction, null)
            { }

            public NodeParseInfo(
                SymbolDict<(FieldParser, object)> fields)
                : this(null, fields)
            { }

            public NodeParseInfo(
                NodeParser parseFunction,
                SymbolDict<(FieldParser, object)> fields)
            {
                m_parseFunction = parseFunction;
                FieldDefs = fields;
            }

            public NodeParser NodeParser { get { return m_parseFunction; } }

            public FieldParser FieldParser(string fieldName)
            {
                if (fieldName == "ROUTE") return new FieldParser(ParseROUTE);
                return FieldDefs[fieldName].Item1;
            }

            public bool TryGetFieldParser(string fieldName, out FieldParser fieldParser)
            {
                if (fieldName == "ROUTE")
                {
                    fieldParser = new FieldParser(ParseROUTE);
                    return true;
                }
                if (FieldDefs.TryGetValue(fieldName, out var fpDef))
                {
                    fieldParser = fpDef.Item1;
                    return true;
                }
                fieldParser = null;
                return false;
            }

            public object DefaultValue(string fieldName)
            {
                return FieldDefs[fieldName].Item2;
            }
        }

        private static SymMapBase ParseGenericNode(Tokenizer t, NodeParseInfo info)
        {
            var result = new SymMapBase();
            ExpectBraceOpen(t);

            // populate fields with default values
            foreach (var kvp in info.FieldDefs)
            {
                if (kvp.Value.Item2 == null) continue;
                result[kvp.Key] = kvp.Value.Item2;
            }

            Tokenizer.Token token = t.NextToken();
            while (!token.IsBraceClose)
            {
                string fieldName = token.ToString();
                if (info.TryGetFieldParser(fieldName, out var fp))
                    result[fieldName] = fp(t);
                else
                    Report.Warn($"[Vrml97] FieldParser: \"{fieldName}\" unknown/unexpected token!");

                token = t.NextToken();
                Thread.Sleep(0);
            }

            return result;
        }

        private static SymMapBase ParseUnknownNode(Tokenizer t)
        {
            ExpectBraceOpen(t);
            var level = 1;

            var sb = new StringBuilder("{");

            do
            {
                var token = t.NextToken();
                sb.Append(" " + token);

                if (token.IsBraceOpen) level++;
                if (token.IsBraceClose) level--;
            }
            while (level > 0);

            var result = new SymMapBase();
            result["unknownNode"] = true;
            result["content"] = sb.ToString();
            return result;
        }

        #endregion
    }
}
