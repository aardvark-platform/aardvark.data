# Aardvark.Data.Ifc
Simple IFC loader for the Aardvark Platform.
## Installing Xbim 6.X packages
Currently, there are no official packages for Xbim 6.X. You will have to add the Xbim development and aardvark-platform feeds to your `paket.dependencies` file:
```
source https://www.myget.org/F/xbim-develop/api/v3/index.json
source https://nuget.pkg.github.com/aardvark-platform/index.json
```
For the aardvark-platform feed you need an access token with read access and add it to paket:
```
dotnet tool restore
dotnet paket config add-token https://nuget.pkg.github.com/aardvark-platform/index.json <token>
```
