FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY .nuget/NuGet/NuGet.Config /root/.nuget/NuGet
COPY .nuget/packages /root/packages

RUN ls -lA /root/.nuget
WORKDIR /src
COPY ["FlexRadioServices/FlexRadioServices.csproj", "FlexRadioServices/"]
RUN dotnet restore --ignore-failed-sources "FlexRadioServices/FlexRadioServices.csproj"
COPY . .
WORKDIR "/src/FlexRadioServices"
RUN dotnet build "FlexRadioServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexRadioServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexRadioServices.dll"]
