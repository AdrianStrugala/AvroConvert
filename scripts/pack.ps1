$csProjName = 'AvroConvert.csproj';

cd E:\workspace\AvroConvert\scripts

cd ../src/AvroConvert

((Get-Content -Path $csProjName -Raw) -replace '3.1.5', '21.37') | Set-Content -Path $csProjName
dotnet build -c Release
dotnet pack -c Release

cd ../../
Get-ChildItem .\src\AvroConvert\obj\Release -Filter *.nuspec | Copy-Item -Destination . -Force -PassThru