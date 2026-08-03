@echo off
chcp 65001 >nul
title CRS Microservices Stopper

echo ===================================================================
echo     CRS - DỪNG TẤT CẢ BACKEND MICROSERVICES (PORT 8080 - 8083)
echo ===================================================================
echo.

set "PORTS=8080 8081 8082 8083"

for %%p in (%PORTS%) do (
    echo Dang kiem tra Port %%p...
    for /f "tokens=5" %%a in ('netstat -ano -p tcp ^| findstr ":%%p " ^| findstr "LISTENING"') do (
        if not "%%a"=="" (
            echo   -> Tim thay tien trinh PID %%a dang chay tren Port %%p. Dang tat...
            taskkill /F /PID %%a >nul 2>&1
        )
    )
)

echo.
echo -------------------------------------------------------------------
echo [HOÀN TẤT] Tat ca cac port (8080, 8081, 8082, 8083) da duoc giai phong!
echo ===================================================================
pause
