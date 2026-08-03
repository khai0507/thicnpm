@echo off
chcp 65001 >nul
title CRS Microservices Launcher

echo ===================================================================
echo       CRS - COURSE REGISTRATION SYSTEM (MICROSERVICES)
echo ===================================================================
echo.

:: Xac dinh thu muc Backend
set "ROOT_DIR=%~dp0"
if exist "%ROOT_DIR%be\crs-microservices\api-gateway" (
    set "BE_DIR=%ROOT_DIR%be\crs-microservices"
) else if exist "%ROOT_DIR%crs-microservices\api-gateway" (
    set "BE_DIR=%ROOT_DIR%crs-microservices"
) else if exist "%ROOT_DIR%api-gateway" (
    set "BE_DIR=%ROOT_DIR%"
) else (
    echo [LOI] Khong tim thay thu muc chua cac microservices!
    echo Vui long kiem tra lai duong dan.
    pause
    exit /b 1
)

echo [INFO] Thu muc Backend: %BE_DIR%
echo.
echo Dang khoi dong cac Microservices theo thu tu:
echo  1. Auth Service         - Port 8081 (auth_db)
echo  2. Course Service       - Port 8082 (course_db)
echo  3. Registration Service - Port 8083 (registration_db)
echo  4. API Gateway          - Port 8080
echo -------------------------------------------------------------------

:: 1. Auth Service
echo [1/4] Dang khoi chay [auth-service] (Port 8081)...
start "CRS [Port 8081] - Auth Service" cmd /k "cd /d "%BE_DIR%\auth-service" && title Auth Service [8081] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 2. Course Service
echo [2/4] Dang khoi chay [course-service] (Port 8082)...
start "CRS [Port 8082] - Course Service" cmd /k "cd /d "%BE_DIR%\course-service" && title Course Service [8082] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 3. Registration Service
echo [3/4] Dang khoi chay [registration-service] (Port 8083)...
start "CRS [Port 8083] - Registration Service" cmd /k "cd /d "%BE_DIR%\registration-service" && title Registration Service [8083] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 4. API Gateway
echo [4/4] Dang khoi chay [api-gateway] (Port 8080)...
start "CRS [Port 8080] - API Gateway" cmd /k "cd /d "%BE_DIR%\api-gateway" && title API Gateway [8080] && mvnw.cmd spring-boot:run"

echo -------------------------------------------------------------------
echo [THANH CONG] Tat ca 4 microservices da duoc mo trong cac cua so rieng!
echo API Gateway se lang nghe tai: http://localhost:8080
echo.
echo De dung tat ca services, hay chay file: stop-backend.bat
echo ===================================================================
pause
