
using System.Collections.Generic;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationAppearanceResource;

namespace Aardvark.Data.Ifc
{
    public static class StylingExt
    {
        public static IfcColourRgb CreateColor(this IModel model, C3f colour)
            => model.New<IfcColourRgb>(x => x.Set(colour));

        public static IfcColourRgb CreateColor(this IModel model, C3d colour)
            => model.New<IfcColourRgb>(x => x.Set(colour));

        #region Text Styling
        public static IfcTextStyleForDefinedFont CreateTextStyleForDefinedFont(this IModel model, C3f colour, C3f background, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstylefordefinedfont.htm

            return model.New<IfcTextStyleForDefinedFont>(f => {
                f.Colour = model.CreateColor(colour);
                // optional
                f.BackgroundColour = model.CreateColor(background);
            });
        }

        public enum TextDecoration { None, UnderLine, Overline, Linethrough }
        public enum TextTransform { Capitalize, Uppercase, Lowercase, None }
        public enum TextAlignment { Left, Right, Center, Justify }

        public static IfcTextStyleTextModel CreateTextStyleTextModel(this IModel model, double textIndent, TextAlignment textAlign, TextDecoration textDecoration, TextTransform textTransform, double letterSpacing, double wordSpacing, double lineHeight)
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

            return model.New<IfcTextStyleTextModel>(tm =>
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
        public static IfcTextStyleFontModel CreateTextStyleFontModel(this IModel model, double fontSize, string fontFamily, string fontModelName, FontStyle fontStyle = FontStyle.Normal, FontWeight fontWeight = FontWeight.Normal, FontVariant fontVariant = FontVariant.Normal)
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

            return model.New<IfcTextStyleFontModel>(f =>
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

        public static IfcTextStyle CreateTextStyle(this IModel model, double fontSize, C3f colour, C3f background, string fontModelName, string fontFamily = "serif", bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifctextstyle.htm

            return model.New<IfcTextStyle>(ts =>
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
        public static IfcSurfaceStyleShading CreateSurfaceStyleShading(this IModel model, C3d surface, double transparency = 0.0)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestyleshading.htm

            return model.New<IfcSurfaceStyleShading>(l =>
            {
                l.SurfaceColour = model.CreateColor(surface);
                l.Transparency = new IfcNormalisedRatioMeasure(transparency); // [0 = opaque .. 1 = transparent]
            });
        }

        public static IfcSurfaceStyleRendering CreateSurfaceStyleRendering(this IModel model, C3d surface, double transparency, C3d diffuse, C3d diffuseTransmission, C3d transmission, C3d specular, double specularHighlight, C3d reflection, IfcReflectanceMethodEnum reflectionType)
        {
            // https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylerendering.htm

            return model.New<IfcSurfaceStyleRendering>(l =>
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

        public static IfcSurfaceStyleLighting CreateSurfaceStyleLighting(this IModel model, C3d diffuseTransmission, C3d diffuseReflection, C3d transmission, C3d reflectance)
        {
            // https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2/HTML/schema/ifcpresentationappearanceresource/lexical/ifcsurfacestylelighting.htm

            return model.New<IfcSurfaceStyleLighting>(l =>
            {
                l.DiffuseTransmissionColour = model.CreateColor(diffuseTransmission);
                l.DiffuseReflectionColour = model.CreateColor(diffuseReflection);
                l.TransmissionColour = model.CreateColor(transmission);
                l.ReflectanceColour = model.CreateColor(reflectance);
            });
        }

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, IEnumerable<IfcSurfaceStyleElementSelect> styles, string name = null)
        {
            // IfcSurfaceStyle is an assignment of one or many surface style elements to a surface, defined by subtypes of
            //     IfcSurface, IfcFaceBasedSurfaceModel, IfcShellBasedSurfaceModel, or by subtypes of IfcSolidModel. 
            // The positive direction of the surface normal relates to the positive side. In case of solids the outside of the solid is to be taken as positive side.

            return model.New<IfcSurfaceStyle>(style =>
            {
                if (name != null) style.Name = name;
                style.Side = IfcSurfaceSide.BOTH;
                style.Styles.AddRange(styles); // [1:5] [IfcSurfaceStyleShading -> IfcSurfaceStyleRendering | IfcSurfaceStyleLighting | IfcSurfaceStyleWithTextures | IfcExternallyDefinedSurfaceStyle | IfcSurfaceStyleRefraction
            });
        }

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, IfcSurfaceStyleElementSelect style, string name = null)
            => CreateSurfaceStyle(model, [style], name);

        public static IfcSurfaceStyle CreateSurfaceStyle(this IModel model, C3d surface, double transparency = 0.0, string name = null)
            => CreateSurfaceStyle(model, model.CreateSurfaceStyleShading(surface, transparency), name);

        #endregion

        #region Curve Styling
        public static IfcCurveStyle CreateCurveStyle(this IModel model, C3d color, double width, double visibleLengh = 0, double invisibleLength = 0, bool modelOrDraughting = true)
        {
            return model.New<IfcCurveStyle>(c =>
            {
                c.ModelOrDraughting = modelOrDraughting;
                c.CurveColour = model.CreateColor(color);
                c.CurveWidth = new IfcPositiveLengthMeasure(width);
                if (visibleLengh > 0)
                {
                    c.CurveFont = model.New<IfcCurveStyleFont>(f =>
                        f.PatternList.Add(model.New<IfcCurveStyleFontPattern>(p =>
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
        public static IfcFillAreaStyleHatching CreateFillAreaStyleHatching(this IModel model, double angle, double startOfNextHatchLine, IfcCurveStyle curveStyle)
        {
            return model.New<IfcFillAreaStyleHatching>(h =>
            {
                h.HatchLineAppearance = curveStyle;
                h.HatchLineAngle = new IfcPlaneAngleMeasure(angle);
                h.StartOfNextHatchLine = new IfcPositiveLengthMeasure(startOfNextHatchLine);
            });
        }

        public static IfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d backgroundColor, bool modelOrDrauting = true, string name = null)
        {
            // NOTE: Color information of surfaces for rendering is assigned by using IfcSurfaceStyle, not by using IfcFillAreaStyle. 
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            return model.New<IfcFillAreaStyle>(a =>
            {
                if (name != null) a.Name = name;
                a.ModelorDraughting = modelOrDrauting;
                // Solid fill for areas and surfaces by only assigning IfcColour to the set of FillStyles. It then provides the background colour for the filled area or surface.
                a.FillStyles.Add(model.CreateColor(backgroundColor));
            });
        }

        public static IfcFillAreaStyle CreateFillAreaStyle(this IModel model, C3d hatchingColour, double angle, double startOfNextHatchLine, IfcCurveStyle curveStyle, bool modelOrDrauting = true, string name = null)
        {
            // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/schema/ifcpresentationappearanceresource/lexical/ifcfillareastyle.htm
            // 
            return model.New<IfcFillAreaStyle>(a =>
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
        public static IfcStyledItem CreateStyleItem(this IfcRepresentationItem item, IEnumerable<IfcPresentationStyle> styles)
        {
            // Each subtype of IfcPresentationStyle is assigned to the IfcGeometricRepresentationItem's through an intermediate IfcStyledItem.
            return item.Model.New<IfcStyledItem>(styleItem => {
                styleItem.Styles.AddRange(styles);
                if (item != null) styleItem.Item = item;
            });
        }

        public static IfcStyledItem CreateStyleItem(this IfcRepresentationItem item, IfcPresentationStyle style)
            => CreateStyleItem(item, [style]);

        #endregion
    }
}
