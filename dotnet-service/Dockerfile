# .NET Service Dockerfile (Multi-stage build)
#
# This Dockerfile uses a multi-stage build to create a lightweight container
# for the .NET AES-256-GCM decryption service. The build stage compiles the
# application, and the runtime stage runs it with minimal overhead.

# ============================================================================
# Build Stage: Compile the .NET application
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies first (for better layer caching)
# This layer will only rebuild if the .csproj file changes
COPY dotnet-service.csproj .
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish the application in Release mode
# Output goes to the /app/out directory
RUN dotnet publish -c Release -o out

# ============================================================================
# Runtime Stage: Run the compiled application
# ============================================================================
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy only the compiled binaries from the build stage
# This keeps the final image small (no SDK, only runtime)
COPY --from=build /app/out .

# Run the .NET application when container starts
CMD ["dotnet", "dotnet-service.dll"]
