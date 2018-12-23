mkdir .nuget
cd .nuget
nuget update /self
for %%f in (..\*.nupkg) do nuget push %%f -Source https://www.nuget.org/api/v2/package
cd ..