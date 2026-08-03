# ===================================================================
# CRS - Dừng các Backend Microservices (PowerShell)
# ===================================================================
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "   CRS - DỪNG TẤT CẢ BACKEND MICROSERVICES (PORT 8080 - 8083)     " -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan

$ports = @(8080, 8081, 8082, 8083)

foreach ($port in $ports) {
    Write-Host "Dang kiem tra Port $port..." -ForegroundColor Yellow
    try {
        $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($connections) {
            foreach ($conn in $connections) {
                $pidToKill = $conn.OwningProcess
                Write-Host "  -> Tim thay PID $pidToKill tren Port $port. Dang tat..." -ForegroundColor Red
                Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
            }
        } else {
            Write-Host "  -> Port $port dang trong." -ForegroundColor Gray
        }
    } catch {
        # Fallback netstat
        $lines = netstat -ano | Select-String ":$port\s+.*LISTENING"
        foreach ($line in $lines) {
            $parts = $line.ToString().Trim() -split '\s+'
            $pId = $parts[-1]
            if ($pId -match '^\d+$') {
                Write-Host "  -> Fallback: Tat PID $pId tren Port $port" -ForegroundColor Red
                Stop-Process -Id $pId -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

Write-Host "`n-------------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "[HOÀN TẤT] Tất cả các port microservices đã được giải phóng!" -ForegroundColor Green
Write-Host "===================================================================" -ForegroundColor Cyan
