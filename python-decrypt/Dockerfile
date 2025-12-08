FROM python:3.11-slim

WORKDIR /app

# Install liboqs for ML-KEM-1024
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    build-essential \
    cmake \
    git \
    libssl-dev \
    && rm -rf /var/lib/apt/lists/*

# Install liboqs (pinned to 0.14.0 to match liboqs-python 0.14.1)
RUN git clone --depth 1 --branch 0.14.0 https://github.com/open-quantum-safe/liboqs.git && \
    cd liboqs && \
    mkdir build && cd build && \
    cmake -DBUILD_SHARED_LIBS=ON .. && \
    make -j$(nproc) && \
    make install && \
    ldconfig && \
    cd ../.. && rm -rf liboqs

# Install Python dependencies
RUN pip install --no-cache-dir liboqs-python cryptography

COPY main.py .

CMD ["python", "main.py"]
