# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# copy published output
COPY --from=build /app ./

# create writable folder for logs
RUN mkdir -p logs

# environment (optional override)
ENV ASPNETCORE_ENVIRONMENT=Production

# if you run Seq in another container named "seq", this makes that URL work:
ENV Seq__ServerUrl=http://seq:5341

ENTRYPOINT ["dotnet", "Darlin.dll"]
