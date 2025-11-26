# Python Service Dockerfile
# 
# This Dockerfile builds a containerized Python service for post-quantum
# hybrid encryption using Kyber1024 (via liboqs) and AES-256-GCM.

# Use Python 3.11 slim image as base
FROM python:3.11-slim

# Install system dependencies required for liboqs
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    git \
    libssl-dev \
    ninja-build \
    && rm -rf /var/lib/apt/lists/*

# Build and install liboqs from source
WORKDIR /tmp
RUN git clone --depth=1 --branch=0.15.0 https://github.com/open-quantum-safe/liboqs.git && \
    cd liboqs && \
    mkdir build && cd build && \
    cmake -GNinja -DCMAKE_INSTALL_PREFIX=/usr/local -DBUILD_SHARED_LIBS=ON .. && \
    ninja && \
    ninja install && \
    cd / && rm -rf /tmp/liboqs

# Update library cache
RUN ldconfig

# Ensure Python can locate the liboqs shared library
ENV LD_LIBRARY_PATH=/usr/local/lib:${LD_LIBRARY_PATH}
ENV OQS_LIB=/usr/local/lib/liboqs.so

# Set working directory inside the container
WORKDIR /app

# Copy and install Python dependencies
COPY requirements.txt .
RUN pip install -r requirements.txt

# Copy the application source files
COPY main.py .

# Run the main encryption/decryption script when container starts
CMD ["python", "main.py"]
