FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
COPY bin/Release/netcoreapp3.1/publish/ App/
FROM mcr.microsoft.com/powershell
COPY --from=0  App/ .
WORKDIR /App
ENTRYPOINT ["dotnet", "Tasklist.Web.dll"]