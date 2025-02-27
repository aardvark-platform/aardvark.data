
using System.Linq;
using System.Collections.Generic;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationAppearanceResource;

namespace Aardvark.Data.Ifc
{
    public static class StylingExt
    {
        #region Colors
        public static void Set(this IIfcColourRgb c, C3d colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static void Set(this IIfcColourRgb c, C3f colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static C3d ToC3d(this IIfcColourRgb col)
            => new(col.Red, col.Green, col.Blue);

        public static C3f ToC3f(this IIfcColourRgb col)
            => col.ToC3d().ToC3f();

        public static IIfcColourRgb CreateColor(this IModel model, C3f colour)
            => model.Factory().ColourRgb(x => x.Set(colour));

        public static IIfcColourRgb CreateColor(this IModel model, C3d colour)
            => model.Factory().ColourRgb(x => x.Set(colour));
        #endregion

        #region Text Styling
        public static IIfcTextStyleForDefinedFont CreateTextStyleForDefinedFont(this IModel model, C3f colour, C3f background, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstylefordefinedfont.htm

            return model.Factory().TextStyleForDefinedFont(f => {
                f.Colour = model.CreateColor(colour);
                // optional
                f.BackgroundColour = model.CreateColor(background);
            });
        }

        public enum TextDecoration { None, UnderLine, Overline, Linethrough }
        public enum TextTransform { Capitalize, Uppercase, Lowercase, None }
        public enum TextAlignment { Left, Right, Center, Justify }

        public static IIfcTextStyleTextModel CreateTextStyleTextModel(this IModel model, double textIndent, TextAlignment textAlign, TextDecoration textDecoration, TextTransform textTransform, double letterSpacing, double wordSpacing, double lineHeight)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstyletextmodel.htm

            string decoration = textDecoration switch
            {
                TextDecoration.UnderLine => "underLine",
                TextDecoration.Overline => "overline",
                TextDecoration.Linethrough => "line-through",
                _ => "none"
            };

            string transform = textTransform switch
            {
                TextTransform.Capitalize => "capitalize",
                TextTransform.Uppercase => "uppercase",
                TextTransform.Lowercase => "lowercase",
                _ => "none"
            };

            string alignment = textAlign switch
            {
                TextAlignment.Left => "left",
                TextAlignment.Right => "right",
                TextAlignment.Center => "center",
                _ => "justify"
            };

            return model.Factory().TextStyleTextModel(tm =>
            {
                // optional
                tm.TextIndent = new IfcLengthMeasure(textIndent);           // The property specifies the indentation that appears before the first formatted line.
                tm.TextAlign = new IfcTextAlignment(alignment);
                tm.TextDecoration = new IfcTextDecoration(decoration);
                tm.TextTransform = new IfcTextTransformation(transform);
                tm.LetterSpacing = new IfcLengthMeasure(letterSpacing);
                tm.WordSpacing = new IfcLengthMeasure(wordSpacing);
                tm.LineHeight = new IfcLengthMeasure(lineHeight);
            });
        }

        public enum FontStyle { Normal, Italic, Oblique }
        public enum FontWeight { Normal, Bold }
        public enum FontVariant { Normal, Smallcaps }
        public static IIfcTextStyleFontModel CreateTextStyleFontModel(this IModel model, double fontSize, string fontFamily, string fontModelName, FontStyle fontStyle = FontStyle.Normal, FontWeight fontWeight = FontWeight.Normal, FontVariant fontVariant = FontVariant.Normal)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstylefontmodel.htm

            string style = fontStyle switch
            {
                FontStyle.Normal => "normal",
                FontStyle.Italic => "italic",
                _ => "oblique"
            };

            string weight = fontWeight switch
            {
                FontWeight.Normal => "400",
                _ => "700",
            };

            string variant = fontVariant switch
            {
                FontVariant.Normal => "normal",
                _ => "small-caps",
            };

            return model.Factory().TextStyleFontModel(f =>
            {
                f.Name = fontModelName;
                f.FontSize = new IfcLengthMeasure(fontSize);
                f.FontFamily.Add(new IfcTextFontName(fontFamily)); // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextfontname.htm
                // optional
                f.FontStyle = new IfcFontStyle(style);
                f.FontWeight = new IfcFontWeight(weight);
                f.FontVariant = new IfcFontVariant(variant);
            });
        }

        public static IIfcTextStyle CreateTextStyle(this IModel model, double fontSize, C3f colour, C3f background, string fontModelName, string fontFamily = "serif", bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstyle.htm

            return model.Factory().TextStyle(ts =>
            {
                ts.ModelOrDraughting = modelOrDrauting;

                if (name != null) ts.Name = name;

                ts.TextFontStyle = model.CreateTextStyleFontModel(fontSize, fontFamily, fontModelName);

                // optional
                ts.TextCharacterAppearance = model.CreateTextStyleForDefinedFont(colour, background);
                ts.TextStyle = model.CreateTextStyleTextModel(10, TextAlignment.Right, TextDecoration.None, TextTransform.None, 10, 10, 20);
            });
        }

        #endregion

        #region Surface Styling

        #region Shading
        public static IIfcSurfaceStyleShading CreateSurfaceStyleShading(this IModel model, C3d surface, double transparency = 0.0)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestyleshading.htm

            return model.Factory().SurfaceStyleShading(l =>
            {
                l.SurfaceColour = model.CreateColor(surface);
                l.Transparency = new IfcNormalisedRatioMeasure(transparency); // [0 = opaque .. 1 = transparent]
            });
        }

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IIfcSurfaceStyleShading shading)
            => shading.Model.CreateSurfaceStyle(shading);

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IModel model, C3d surface, double transparency = 0.0, string name = null)
            => CreateSurfaceStyle(model, model.CreateSurfaceStyleShading(surface, transparency), name);

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IModel model, C4d surface, string name = null)
            => model.CreateSurfaceStyle(surface.RGB, (1.0 - surface.A).Clamp(0, 1), name);
        #endregion

        #region Rendering
        public static IIfcSurfaceStyleRendering CreateSurfaceStyleRendering(this IModel model, C3d surface, double transparency, C3d diffuse, C3d diffuseTransmission, C3d transmission, C3d specular, double specularHighlight, C3d reflection, IfcReflectanceMethodEnum reflectionType)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylerendering.htm

            return model.Factory().SurfaceStyleRendering(l =>
            {
                l.SurfaceColour = model.CreateColor(surface);
                l.Transparency = new IfcNormalisedRatioMeasure(transparency); // [0 = opaque .. 1 = transparent]
                l.DiffuseColour = model.CreateColor(diffuse);

                l.TransmissionColour = model.CreateColor(transmission);
                l.DiffuseTransmissionColour = model.CreateColor(diffuseTransmission);

                l.ReflectionColour = model.CreateColor(reflection);
                l.ReflectanceMethod = reflectionType;

                // The IfcSpecularExponent defines the datatype for exponent determining the sharpness of the 'reflection'.
                // The reflection is made sharper with large values of the exponent, such as 10.0.
                // Small values, such as 1.0, decrease the specular fall - off.
                // IfcSpecularExponent is of type REAL.
                l.SpecularHighlight = new IfcSpecularExponent(specularHighlight);
                l.SpecularColour = model.CreateColor(specular);
            });
        }

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IIfcSurfaceStyleRendering rendering)
            => rendering.Model.CreateSurfaceStyle(rendering);
        #endregion

        #region Lighting
        public static IIfcSurfaceStyleLighting CreateSurfaceStyleLighting(this IModel model, C3d diffuseTransmission, C3d diffuseReflection, C3d transmission, C3d reflectance)
        {
            // https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylelighting.htm

            return model.Factory().SurfaceStyleLighting(l =>
            {
                l.DiffuseTransmissionColour = model.CreateColor(diffuseTransmission);
                l.DiffuseReflectionColour = model.CreateColor(diffuseReflection);
                l.TransmissionColour = model.CreateColor(transmission);
                l.ReflectanceColour = model.CreateColor(reflectance);
            });
        }

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IIfcSurfaceStyleLighting lighting)
            => lighting.Model.CreateSurfaceStyle(lighting);
        #endregion

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IModel model, IEnumerable<IIfcSurfaceStyleElementSelect> styles, string name = null)
        {
            // IfcSurfaceStyle is an assignment of one or many surface style elements to a surface, defined by subtypes of
            //     IfcSurface, IfcFaceBasedSurfaceModel, IfcShellBasedSurfaceModel, or by subtypes of IfcSolidModel. 
            // The positive direction of the surface normal relates to the positive side. In case of solids the outside of the solid is to be taken as positive side.

            return model.Factory().SurfaceStyle(style =>
            {
                if (name != null) style.Name = name;
                style.Side = IfcSurfaceSide.BOTH;
                style.Styles.AddRange(styles); // [1:5] [IfcSurfaceStyleShading -> IfcSurfaceStyleRendering | IfcSurfaceStyleLighting | IfcSurfaceStyleWithTextures | IfcExternallyDefinedSurfaceStyle | IfcSurfaceStyleRefraction
            });
        }

        public static IIfcSurfaceStyle CreateSurfaceStyle(this IModel model, IIfcSurfaceStyleElementSelect style, string name = null)
            => CreateSurfaceStyle(model, [style], name);

        public static bool TryCreateSurfaceStyle(this IModel model, PolyMesh mesh, out IIfcSurfaceStyle style)
        {
            if (mesh.HasColors)
            {
                var col = ((C4b)mesh.VertexAttributes.Get(PolyMesh.Property.Colors).GetValue(0)).ToC4d();
                style = model.CreateSurfaceStyle(col, "MeshColor");
                return true;
            }
            else
            {
                style = null;
                return false;
            }
        }

        public static bool TryCreateSurfaceStyle(this IIfcPresentationLayerAssignment layer, out IIfcSurfaceStyle style)
        {

            if (layer is IIfcPresentationLayerWithStyle a && a.LayerStyles.OfType<IIfcSurfaceStyle>().FirstOrDefault() != null)
            {
                style = a.LayerStyles.OfType<IIfcSurfaceStyle>().First();
                return true;
            }
            else
            {
                style = null;
                return false;
            }
        }

        #endregion

        #region Curve Styling
        public static IIfcCurveStyle CreateCurveStyle(this IModel model, C3d color, double width, double visibleLengh = 0, double invisibleLength = 0, bool modelOrDraughting = true)
        {
            var factory = model.Factory();
            return factory.CurveStyle(c =>
            {
                c.ModelOrDraughting = modelOrDraughting;
                c.CurveColour = model.CreateColor(color);
                c.CurveWidth = (IfcPositiveLengthMeasure) width;
                if (visibleLengh > 0)
                {
                    c.CurveFont = factory.CurveStyleFont(f =>
                        f.PatternList.Add(factory.CurveStyleFontPattern(p =>
                        {
                            p.VisibleSegmentLength = visibleLengh;
                            if (invisibleLength > 0) p.InvisibleSegmentLength = invisibleLength;
                        })
                    ));
                }
            });
        }
        #endregion

        #region Area Styling
        public static IIfcFillAreaStyleHatching CreateFillAreaStyleHatching(this IModel model, double angle, double startOfNextHatchLine, IIfcCurveStyle curveStyle)
        {
            return model.Factory().FillAreaStyleHatching(h =>
            {
                h.HatchLineAppearance = curveStyle;
                h.HatchLineAngle = new IfcPlaneAngleMeasure(angle);
                h.StartOfNextHatchLine = new IfcPositiveLengthMeasure(startOfNextHatchLine);
            });
        }

        public static IIfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d backgroundColor, bool modelOrDrauting = true, string name = null)
        {
            // NOTE: Color information of surfaces for rendering is assigned by using IfcSurfaceStyle, not by using IfcFillAreaStyle. 
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            return model.Factory().FillAreaStyle(a =>
            {
                if (name != null) a.Name = name;
                a.ModelorDraughting = modelOrDrauting;
                // Solid fill for areas and surfaces by only assigning IfcColour to the set of FillStyles. It then provides the background colour for the filled area or surface.
                a.FillStyles.Add(model.CreateColor(backgroundColor));
            });
        }

        public static IIfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d hatchingColour, double angle, double startOfNextHatchLine, IIfcCurveStyle curveStyle, bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            // 
            return model.Factory().FillAreaStyle(a =>
            {
                if (name != null) a.Name = name;
                a.ModelorDraughting = modelOrDrauting;
                // Solid fill for areas and surfaces by only assigning IfcColour to the set of FillStyles. It then provides the background colour for the filled area or surface.
                a.FillStyles.AddRange([
                    model.CreateColor(hatchingColour),
                    model.CreateFillAreaStyleHatching(angle, startOfNextHatchLine, curveStyle)
                ]);
            });
        }
        #endregion

        #region Style Item
        public static IIfcStyledItem CreateStyleItem(this IIfcRepresentationItem item, IIfcPresentationStyle[] styles)
        {
            // Each subtype of IfcPresentationStyle is assigned to the IfcGeometricRepresentationItem's through an intermediate IfcStyledItem.
            return item.Model.Factory().StyledItem(styleItem => {
                styleItem.Styles.AddRange(styles);
                if (item != null) styleItem.Item = item;
            });
        }

        public static IIfcStyledItem CreateStyleItem(this IIfcRepresentationItem item, IIfcPresentationStyle style)
            => CreateStyleItem(item, [style]);

        #endregion
    }
}
