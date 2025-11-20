# Python Service Dockerfile
# 
# This Dockerfile builds a containerized Python service for AES-256-GCM
# encryption and decryption, with optional post-quantum key generation support.

# Use Python 3.11 slim image as base for a smaller footprint
FROM python:3.11-slim

# Set working directory inside the container
WORKDIR /app

# Copy and install Python dependencies first (for better layer caching)
# This layer will only rebuild if requirements.txt changes
COPY requirements.txt .
RUN pip install -r requirements.txt

# Copy the application source files
COPY main.py .

# Run the main encryption/decryption script when container starts
CMD ["python", "main.py"]
