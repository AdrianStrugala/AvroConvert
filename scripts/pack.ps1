$nextVersion = '3.2.1'



$csProjName = 'AvroConvert.csproj';

cd scripts
$currentVersion = Get-Content .\version.txt -Raw 

cd ../
cd src/AvroConvert

((Get-Content -Path $csProjName -Raw) -replace $currentVersion, $nextVersion) | Set-Content -Path $csProjName
dotnet build -c Release
dotnet pack -c Release

cd ../../
Get-ChildItem .\src\AvroConvert\bin\Release -Filter *nupkg | Copy-Item -Destination . -Force -PassThru

cd scripts
Set-Content -Path .\version.txt -Value $nextVersion;