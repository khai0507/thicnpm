@echo off
chcp 65001 >nul
title CRS Microservices Launcher (BE)

echo ===================================================================
echo       CRS - KHỞI CHẠY CÁC BACKEND MICROSERVICES
echo ===================================================================
echo.

set "BE_DIR=%~dp0"
echo [INFO] BE Directory: %BE_DIR%
echo.

:: 1. Auth Service
echo [1/4] Dang khoi chay [auth-service] (Port 8081)...
start "CRS [Port 8081] - Auth Service" cmd /k "cd /d "%BE_DIR%auth-service" && title Auth Service [8081] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 2. Course Service
echo [2/4] Dang khoi chay [course-service] (Port 8082)...
start "CRS [Port 8082] - Course Service" cmd /k "cd /d "%BE_DIR%course-service" && title Course Service [8082] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 3. Registration Service
echo [3/4] Dang khoi chay [registration-service] (Port 8083)...
start "CRS [Port 8083] - Registration Service" cmd /k "cd /d "%BE_DIR%registration-service" && title Registration Service [8083] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 4. API Gateway
echo [4/4] Dang khoi chay [api-gateway] (Port 8080)...
start "CRS [Port 8080] - API Gateway" cmd /k "cd /d "%BE_DIR%api-gateway" && title API Gateway [8080] && mvnw.cmd spring-boot:run"

echo -------------------------------------------------------------------
echo [THANH CONG] Tat ca 4 microservices da duoc bat!
echo API Gateway: http://localhost:8080
echo ===================================================================
pause
