# ===================================================================
# CRS - Khởi chạy Backend Microservices (PowerShell)
# ===================================================================
$ErrorActionPreference = "Continue"

$rootDir = $PSScriptRoot
$beDir = Join-Path $rootDir "be\crs-microservices"
if (-not (Test-Path $beDir)) {
    if (Test-Path (Join-Path $rootDir "crs-microservices")) {
        $beDir = Join-Path $rootDir "crs-microservices"
    } else {
        $beDir = $rootDir
    }
}

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "      CRS - COURSE REGISTRATION SYSTEM (MICROSERVICES)           " -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "[INFO] Backend Path: $beDir" -ForegroundColor Gray

# Kiểm tra các port trước khi chạy
$ports = @(8081, 8082, 8083, 8080)
$services = @(
    @{ Name = "auth-service"; Port = 8081; Dir = "auth-service"; Title = "CRS - Auth Service (8081)" },
    @{ Name = "course-service"; Port = 8082; Dir = "course-service"; Title = "CRS - Course Service (8082)" },
    @{ Name = "registration-service"; Port = 8083; Dir = "registration-service"; Title = "CRS - Registration Service (8083)" },
    @{ Name = "api-gateway"; Port = 8080; Dir = "api-gateway"; Title = "CRS - API Gateway (8080)" }
)

Write-Host "`nDang khoi dong 4 Microservices..." -ForegroundColor Yellow

$i = 1
foreach ($svc in $services) {
    $svcPath = Join-Path $beDir $svc.Dir
    Write-Host "[$i/4] Khoi chay $($svc.Name) tren Port $($svc.Port)..." -ForegroundColor Green
    
    Start-Process cmd.exe -ArgumentList "/k title $($svc.Title) && cd /d `"$svcPath`" && mvnw.cmd spring-boot:run"
    Start-Sleep -Seconds 3
    $i++
}

Write-Host "`n-------------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "[THÀNH CÔNG] Tất cả Microservices đã được khởi động trong các cửa sổ riêng!" -ForegroundColor Green
Write-Host "API Gateway: http://localhost:8080" -ForegroundColor White
Write-Host "Dừng các service bằng lệnh: .\stop-backend.ps1" -ForegroundColor Yellow
Write-Host "===================================================================" -ForegroundColor Cyan
