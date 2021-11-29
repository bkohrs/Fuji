@echo off
dotnet clean
dotnet pack Fuji.Core\Fuji.Core.csproj -o %LocalNugetPath%
dotnet pack Fuji.Generator\Fuji.Generator.csproj -o %LocalNugetPath%
rmdir /Q /S %USERPROFILE%\.nuget\packages\fuji.generator
rmdir /Q /S %USERPROFILE%\.nuget\packages\fuji.core
dotnet clean
dotnet test