FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# Copy everything
WORKDIR /root
COPY . ./Ceres

# Build Steps
WORKDIR /root/Ceres
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o ~/Ceres/Release

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /root/Ceres
COPY --from=build-env /root/Ceres ./

ENTRYPOINT ["/root/Ceres/Release/CeresDSP"]
