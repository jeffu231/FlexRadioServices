FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FlexRadioServices/FlexRadioServices.csproj", "FlexRadioServices/"]

RUN dotnet nuget locals all -c

ARG GITHUB_USERNAME
ARG GITHUB_TOKEN
RUN dotnet nuget add source "https://nuget.pkg.github.com/jeffu231/index.json" -n github -u ${GITHUB_USERNAME} -p ${GITHUB_TOKEN} --store-password-in-clear-text

RUN dotnet restore "FlexRadioServices/FlexRadioServices.csproj"
COPY . .
WORKDIR "/src/FlexRadioServices"
RUN dotnet build "FlexRadioServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexRadioServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexRadioServices.dll"]

EXPOSE 8080

LABEL org.opencontainers.image.source=https://github.com/jeffu231/FlexRadioServices
LABEL org.opencontainers.image.description="Flex Radio Services - An API to integrate with Flex 6xxx radios"
LABEL org.opencontainers.image.licenses=MIT
