using Aardvark.Base;
using System;
using System.IO;
using System.Text;

#pragma warning disable 1591 // missing XML comments

namespace Aardvark.Data.Photometry
{
    public class LDTLampData
    {
        /// <summary>
        /// Number of lamps:
        /// For absolute photometry, number is negative
        /// </summary>
        public int Number;

        /// <summary>
        /// Type of lamps
        /// </summary>
        public string Type;

        /// <summary>
        /// Total luminous flux of lamps (lm):
        /// For absolute photometry, this field is Total Luminous Flux of Luminaire
        /// </summary>
        public double TotalFlux;

        /// <summary>
        /// Color appearance / color temperature of lamps
        /// </summary>
        public string Color;

        /// <summary>
        /// Color rendering group / color rendering index
        /// </summary>
        public string ColorRendering;

        /// <summary>
        /// Wattage including ballast (W)
        /// </summary>
        public double Wattage;
    }

    /// <summary>
    /// ELUMDAT Symmetry indicator - Isym
    /// Specifies how the luminaire has been measured and how the data needs to be interpreted
    /// </summary>
    public enum LDTSymmetry
    {
        None     = 0, // No symmetry
        Vertical = 1, // Full symmetrically
        C0       = 2, // Measurement data from 0 - 180
        C1       = 3, // Measurement data from 270 - 90
        Quarter  = 4  // Measurement data from 0 - 90
    }

    /// <summary>
    /// ELUMDAT Type indicator - Ityp
    /// Indicates the luminaire type and describes its symmetry character. It does not necessarily mean that the measurement data 
    /// is also perfectly symmetrical according to this (e.g. Ityp = 1 does not force Isym = 1)
    /// </summary>
    public enum LDTItype
    {
        PointSource = 0,           // point source with no symmetry
        PointVerticalSymmetry = 1, // symmetry about the vertical axis
        Linear = 2,                // linear luminaire / can be subdivided in longitudinal and transverse directions
        PointWithOtherSymmetry = 3 // point source with any other symmetry
    }

    /// <summary>
    /// Holds the data represented in an EULUMDATA luminaire data file
    /// http://www.helios32.com/Eulumdat.htm
    /// </summary>
    public class LDTData
    {
        public static LDTData FromFile(String filename)
        {
            return LDTParser.Parse(filename);
        }

        public static LDTData FromStream(Stream stream)
        {
            return new LDTParser().Parse(stream);
        }

        public static LDTData FromString(string data)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            return FromStream(stream);
        }

        public static LDTData ParseMeta(string filename)
        {
            return new LDTParser().ParseMeta(filename);
        }

        public LDTData() { }

        /// <summary>
        /// Company identification/databank/version/format identification (Max 78)
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Ityp - Type indicator
        /// 
        /// 0 - point source with no symmetry
        /// 1 - symmetry  about the vertical axis
        /// 2 - linear luminaire
        /// 3 - point source with any other symmetry. 
        /// 
        /// Note: only linear luminaires, Ityp = 2, are being subdivided in longitudinal and transverse directions
        /// </summary>
        public LDTItype Itype { get; set; }

        /// <summary>
        /// lsym - Symmetry indicator 
        /// 
        /// 0 ... no symmetry
        /// 1 - symmetry about the vertical axis
        /// 2 - symmetry to plane C0-C180
        /// 3 - symmetry to plane C90-C270
        /// 4 - symmetry to plane C0-C180 and to plane C90-C270
        /// </summary>
        public LDTSymmetry Symmetry { get; set; }

        /// <summary>
        /// Mc - Number of C-planes between 0 and 360 degrees (usually 24 for interior, 36 for road lighting luminaires)
        /// </summary>
        public int PlaneCount { get; set; }

        /// <summary>
        /// Dc - Distance between C-planes (Dc = 0 for non-equidistantly available C-planes)
        /// </summary>
        public double HorAngleStep { get; set; }

        /// <summary>
        /// Ng - Number of luminous intensities in each C-plane (usually 19 or 37)
        /// </summary>
        public int ValuesPerPlane { get; set; }

        /// <summary>
        /// Dg - Distance between luminous intensities per C-plane (Dg = 0 for non-equidistantly available luminous intensities in C-planes)
        /// </summary>
        public double VertAngleStep { get; set; }

        /// <summary>
        /// Measurement report number (Max 78)
        /// </summary>
        public string MeasurementReportNumber { get; set; }

        /// <summary>
        /// Luminaire name (Max 78)
        /// </summary>
        public string LuminaireName { get; set; }

        /// <summary>
        /// Luminaire number (Max 78)
        /// </summary>
        public string LuminaireNumber { get; set; }

        /// <summary>
        /// File name (8)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Date/user (Max 78)
        /// </summary>
        public string DateUser { get; set; }

        /// <summary>
        /// Length/diameter of luminaire (mm)
        /// </summary>
        public int LengthLuminaire { get; set; }

        /// <summary>
        /// b - Width of luminaire (mm) (b = 0 for circular luminaire)
        /// </summary>
        public int WidthLuminaire { get; set; }

        /// <summary>
        /// Height of luminaire (mm)
        /// </summary>
        public int HeightLuminare { get; set; }

        /// <summary>
        /// Length/diameter of luminous area (mm)
        /// </summary>
        public int LengthLuminousArea { get; set; }

        /// <summary>
        /// b1 - Width of luminous area (mm) (b1 = 0 for circular luminous area of luminaire)
        /// </summary>
        public int WidthLuminousArea { get; set; }

        /// <summary>
        /// Height of luminous area C0-plane(mm)
        /// </summary>
        public int HeightLuminousAreaC0 { get; set; }

        /// <summary>
        /// Height of luminous area C90-plane (mm)
        /// </summary>
        public int HeightLuminousAreaC90 { get; set; }

        /// <summary>
        /// Height of luminous area C180-plane (mm)
        /// </summary>
        public int HeightLuminousAreaC180 { get; set; }

        /// <summary>
        /// Height of luminous area C270-plane (mm)
        /// </summary>
        public int HeightLuminousAreaC270 { get; set; }

        /// <summary>
        /// DFF - Downward flux fraction(%)
        /// </summary>
        public double DownwardFluxFraction { get; set; }

        /// <summary>
        /// LORL - Light output ratio luminaire (%)
        /// </summary>
        public double LightOutputRatioLuminaire { get; set; }

        /// <summary>
        /// Conversion factor for luminous intensities(depending on measurement)
        /// </summary>
        public double ConversionIntensity { get; set; }

        /// <summary>
        /// Standard sets of lamps (optional, also extendable on company-specific basis)
        /// For absolute photometry, there is only one entry
        /// Lamp attributes:
        /// a: Number of lamps: For absolute photometry, number is negative
        /// b: Type of lamps
        /// c: Total luminous flux of lamps (lm): For absolute photometry, this field is Total Luminous Flux of Luminaire
        /// d: Color appearance / color temperature of lamps
        /// e: Color rendering group / color rendering index
        /// f: Wattage including ballast (W)
        /// </summary>
        public LDTLampData[] LampSets { get; set; }

        /// <summary>
        /// Tilt of luminaire during measurement(road lighting luminaires)
        /// </summary>
        public double Tilt { get; set; }

        /// <summary>
        /// DR - Direct ratios for room indices k = 0.6 ... 5 (for determination of luminaire numbers according to utilization factor method)
        /// </summary>
        public double[] DirectRatios { get; set; }

        /// <summary>
        /// Angles C (beginning with 0 degrees)
        /// </summary>
        public double[] HorizontalAngles { get; set; }

        /// <summary>
        /// Angles G (beginning with 0 degrees)
        ///
        /// </summary>
        public double[] VerticleAngles { get; set; }

        /// <summary>
        /// Luminous intensity distribution normalized to cd per 1000 lumen (cd/1000 lumens)
        /// </summary>
        public Matrix<double> Data { get; set; }
    }
}
