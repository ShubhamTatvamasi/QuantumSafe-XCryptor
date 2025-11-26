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

# Install liboqs build dependencies and build liboqs 0.15.0 (shared)
RUN apt-get update && apt-get install -y --no-install-recommends \
	build-essential cmake ninja-build git ca-certificates libssl-dev pkg-config \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /tmp
RUN git clone --depth=1 --branch=0.15.0 https://github.com/open-quantum-safe/liboqs.git \
	&& cd liboqs \
	&& cmake -S . -B build -GNinja -DBUILD_SHARED_LIBS=ON -DOQS_USE_OPENSSL=ON -DCMAKE_BUILD_TYPE=Release \
	&& cmake --build build --config Release \
	&& cmake --install build
RUN ldconfig

WORKDIR /app

# Copy project file and restore dependencies first (for better layer caching)
# This layer will only rebuild if the .csproj file changes
COPY dotnet-service.csproj .
RUN dotnet restore

# Copy all source files
COPY . .

# Build and install shim library for liboqs interop
RUN gcc -fPIC -O2 -I/usr/local/include -L/usr/local/lib -loqs \
	-shared /app/oqs_shim.c -o /usr/local/lib/liboqs_shim.so \
	-Wl,-rpath,/usr/local/lib

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

# Copy liboqs shared libs and shim from build stage
COPY --from=build /usr/local/lib/liboqs.so* /usr/local/lib/
COPY --from=build /usr/local/lib/liboqs_shim.so /usr/local/lib/
ENV LD_LIBRARY_PATH=/usr/local/lib
ENV LD_PRELOAD=/usr/local/lib/liboqs.so

# Run the .NET application when container starts
CMD ["dotnet", "dotnet-service.dll"]
