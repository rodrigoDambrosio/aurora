# Test Aurora API Endpoints
Write-Host "🧪 TESTING AURORA API ENDPOINTS" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Start backend in background
Write-Host "🚀 Starting backend..." -ForegroundColor Yellow
$job = Start-Job -ScriptBlock { 
    Set-Location "c:\Users\Camila\IA Aplicada\Aurora"
    dotnet run --project backend/Aurora.Api/Aurora.Api.csproj 
}

# Wait for backend to start
Write-Host "⏳ Waiting for backend to initialize..." -ForegroundColor Yellow
Start-Sleep 12

try {
    Write-Host "`n✅ Testing Health Endpoint:" -ForegroundColor Green
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5291/api/health" -Method Get
    $healthResponse | ConvertTo-Json -Depth 2

    Write-Host "`n✅ Testing Event Categories Endpoint:" -ForegroundColor Green
    $categoriesResponse = Invoke-RestMethod -Uri "http://localhost:5291/api/eventcategories" -Method Get
    Write-Host "Found $($categoriesResponse.Length) categories:"
    $categoriesResponse | ForEach-Object { Write-Host "  - $($_.name) ($($_.color))" }

    Write-Host "`n✅ Testing Weekly Events Endpoint:" -ForegroundColor Green
    $weeklyBody = @{
        weekStart = "2025-09-22"
    } | ConvertTo-Json
    $eventsResponse = Invoke-RestMethod -Uri "http://localhost:5291/api/events/weekly" -Method Post -Body $weeklyBody -ContentType "application/json"
    Write-Host "Found $($eventsResponse.events.Length) events for the week:"
    $eventsResponse.events | ForEach-Object { Write-Host "  - $($_.title) ($($_.startDate))" }

    Write-Host "`n🎉 ALL TESTS PASSED!" -ForegroundColor Green

} catch {
    Write-Host "❌ Error testing API: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Write-Host "`n🛑 Stopping backend..." -ForegroundColor Yellow
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -ErrorAction SilentlyContinue
}