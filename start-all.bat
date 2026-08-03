@echo off
chcp 65001 >nul
title CRS Fullstack Launcher (BE + FE)

echo ===================================================================
echo   CRS - COURSE REGISTRATION SYSTEM (FULLSTACK LAUNCHER)
echo ===================================================================
echo.

set "ROOT_DIR=%~dp0"
set "BE_DIR=%ROOT_DIR%be\crs-microservices"
set "FE_DIR=%ROOT_DIR%fe\crs-frontend"

if not exist "%BE_DIR%" (
    if exist "%ROOT_DIR%crs-microservices" (
        set "BE_DIR=%ROOT_DIR%crs-microservices"
    ) else (
        set "BE_DIR=%ROOT_DIR%"
    )
)

if not exist "%FE_DIR%" (
    if exist "%ROOT_DIR%crs-frontend" (
        set "FE_DIR=%ROOT_DIR%crs-frontend"
    )
)

echo [INFO] BE Path: %BE_DIR%
echo [INFO] FE Path: %FE_DIR%
echo.
echo Dang khoi dong toan bo he thong...
echo -------------------------------------------------------------------

:: 1. Auth Service
echo [1/5] Dang khoi chay [auth-service] (Port 8081)...
start "CRS [Port 8081] - Auth Service" cmd /k "cd /d "%BE_DIR%\auth-service" && title Auth Service [8081] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 2. Course Service
echo [2/5] Dang khoi chay [course-service] (Port 8082)...
start "CRS [Port 8082] - Course Service" cmd /k "cd /d "%BE_DIR%\course-service" && title Course Service [8082] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 3. Registration Service
echo [3/5] Dang khoi chay [registration-service] (Port 8083)...
start "CRS [Port 8083] - Registration Service" cmd /k "cd /d "%BE_DIR%\registration-service" && title Registration Service [8083] && mvnw.cmd spring-boot:run"
timeout /t 3 /nobreak >nul

:: 4. API Gateway
echo [4/5] Dang khoi chay [api-gateway] (Port 8080)...
start "CRS [Port 8080] - API Gateway" cmd /k "cd /d "%BE_DIR%\api-gateway" && title API Gateway [8080] && mvnw.cmd spring-boot:run"
timeout /t 2 /nobreak >nul

:: 5. Frontend Vite
echo [5/5] Dang khoi chay [crs-frontend] (Port 5173)...
start "CRS [Port 5173] - Frontend UI" cmd /k "cd /d "%FE_DIR%" && title Frontend React [5173] && npm run dev"

echo -------------------------------------------------------------------
echo [HOÀN TẤT] Toan bo he thong da duoc khoi chay!
echo  - Frontend Web UI:  http://localhost:5173
echo  - Backend Gateway:  http://localhost:8080
echo.
echo De dung toan bo backend, chay file: stop-backend.bat
echo ===================================================================
pause
