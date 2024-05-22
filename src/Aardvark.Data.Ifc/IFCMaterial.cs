using Xbim.Ifc;

namespace Aardvark.Data.Ifc
{
    public class IFCMaterial
    {
        public XbimTexture Texture { get; private set; }
        public string Name { get; private set; }
        public double ThermalConductivity { get; private set; }
        public double SpecificHeatCapacity { get; private set; }
        public double MassDensity { get; private set; }

        public IFCMaterial(string name, XbimTexture texture)
        {
            Name = name;
            Texture = texture;
            MassDensity = 0.0;
            SpecificHeatCapacity = 0.0;
            ThermalConductivity = 0.0;
        }

        public IFCMaterial(string name, XbimTexture texture, double thermalConductivity, double specificHeatCapacity, double massDensity)
        {
            Name = name;
            Texture = texture;
            ThermalConductivity = thermalConductivity;
            SpecificHeatCapacity = specificHeatCapacity;
            MassDensity = massDensity;
        }
    }
}
