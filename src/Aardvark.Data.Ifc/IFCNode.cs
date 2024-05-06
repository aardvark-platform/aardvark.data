using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public interface IIFCNode
    {
        List<IIFCNode> SubNodes { get; }
        IIfcObjectDefinition MetaData { get; }
    }

    public class IFCNode : IIFCNode
    {
        public IIfcObjectDefinition MetaData { get; private set; }

        public List<IIFCNode> SubNodes { get; private set; }

        public IFCNode(IIfcObjectDefinition metaData, List<IIFCNode> subNodes)
        {
            MetaData = metaData;
            SubNodes = subNodes;
        }
    }
}
