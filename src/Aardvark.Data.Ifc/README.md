# Aardvark.Data.Ifc

[![NuGet](https://badgen.net/nuget/v/Aardvark.Data.Ifc)](https://www.nuget.org/packages/Aardvark.Data.Ifc/)
[![NuGet](https://badgen.net/nuget/dt/Aardvark.Data.Ifc)](https://www.nuget.org/packages/Aardvark.Data.Ifc/)

IFC wrapper based on the [xBIM toolkit](https://docs.xbim.net/index.html) for the Aardvark platform.

The xBIM library is made available under the CDDL Open Source licence. See the licenses https://docs.xbim.net/license/third-party-licenses.html

## What is it?
The xBIM Tookit (eXtensible Building Information Modelling) is an open-source, software development BIM toolkit that supports the BuildingSmart Data Model (aka the [Industry Foundation Classes IFC](https://www.buildingsmart.org/standards/bsi-standards/industry-foundation-classes/)).

xBIM allows developers to read, create and view Building Information (BIM) Models in the IFC format. There is full support for geometric, topological operations and visualizations.
Check the [quick start guide](https://docs.xbim.net/quick-start.html) to gain an overview how xBIM allows IFC manipulation and generation.

## Get started

1. Add *Aardvark.Data.Ifc* nuget to your project.
2. Load your IFC file via ```IFCParser.PreprocessIFC (filepath)```.
	- The herby generated IFCData holds a cache-optimized version of the xBIM-Model to enable efficient query operations. 
	- Geometries are wrapped to our [PolyMesh](https://github.com/aardvark-platform/aardvark.algodat) data structure. 
	- Materials are retrieved to enable quick rendering.
	- Hierarchy provides a node based project description to build your scene graph.
3. Edit existing Ifc-Object or attach new Ifc-Object
	- Use the xBIM toolkit or any of our [extensions functions](https://github.com/aardvark-platform/aardvark.data/blob/master/src/Aardvark.Data.Ifc/IFCHelper.cs).
	- Don't forget to use *Transactions* while manipulating the IfcStore.
4. Export your enriched file via ```IfcStore.SaveAs(string fieName)```.