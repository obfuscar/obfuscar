md .nuget
cd .nuget
call nuget update /self
rmdir /S /Q ..\local
for %%f in (..\*.nupkg) do call nuget add %%f -source ..\local
cd ..