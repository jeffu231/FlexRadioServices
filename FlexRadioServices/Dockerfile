# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["FlexRadioServices/FlexRadioServices.csproj", "FlexRadioServices/"]

RUN dotnet nuget locals all -c

ARG GITHUB_USERNAME
RUN --mount=type=secret,id=github_token \
    token="$(cat /run/secrets/github_token)" \
    && dotnet nuget add source "https://nuget.pkg.github.com/jeffu231/index.json" -n github -u "$GITHUB_USERNAME" -p "$token" --store-password-in-clear-text \
    && dotnet restore "FlexRadioServices/FlexRadioServices.csproj" \
    && dotnet nuget remove source github
COPY . .
WORKDIR "/src/FlexRadioServices"
RUN dotnet build "FlexRadioServices.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "FlexRadioServices.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "FlexRadioServices.dll"]


