@echo off
dotnet clean
dotnet pack Fuji.sln -o %LocalNugetPath%
rmdir /Q /S %USERPROFILE%\.nuget\packages\fuji.generator
rmdir /Q /S %USERPROFILE%\.nuget\packages\fuji.core
dotnet clean
dotnet test