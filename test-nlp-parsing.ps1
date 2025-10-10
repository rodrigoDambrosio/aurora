# Test NLP Parsing Endpoint
# Este script prueba el nuevo endpoint de parsing de texto natural

$baseUrl = "http://localhost:5291/api"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "🧪 Probando el endpoint POST /api/events/from-text" -ForegroundColor Cyan
Write-Host ""

# Test 1: Reunión mañana
Write-Host "Test 1: 'reunión con cliente mañana a las 3pm'" -ForegroundColor Yellow
$body1 = @{
    text = "reunión con cliente mañana a las 3pm"
} | ConvertTo-Json

try {
    $response1 = Invoke-RestMethod -Uri "$baseUrl/events/from-text" -Method Post -Headers $headers -Body $body1
    Write-Host "✅ Éxito:" -ForegroundColor Green
    Write-Host "   Título: $($response1.event.title)" -ForegroundColor White
    Write-Host "   Fecha inicio: $($response1.event.startDate)" -ForegroundColor White
    Write-Host "   Fecha fin: $($response1.event.endDate)" -ForegroundColor White
    Write-Host "   Categoría ID: $($response1.event.eventCategoryId)" -ForegroundColor White
    if ($response1.validation) {
        Write-Host "   Validación: $($response1.validation.isApproved) - $($response1.validation.recommendationMessage)" -ForegroundColor Magenta
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# Test 2: Gimnasio hoy
Write-Host "Test 2: 'gimnasio hoy 6:30pm por 1 hora'" -ForegroundColor Yellow
$body2 = @{
    text = "gimnasio hoy 6:30pm por 1 hora"
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/events/from-text" -Method Post -Headers $headers -Body $body2
    Write-Host "✅ Éxito:" -ForegroundColor Green
    Write-Host "   Título: $($response2.event.title)" -ForegroundColor White
    Write-Host "   Fecha inicio: $($response2.event.startDate)" -ForegroundColor White
    Write-Host "   Fecha fin: $($response2.event.endDate)" -ForegroundColor White
    Write-Host "   Categoría ID: $($response2.event.eventCategoryId)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Estudiar el miércoles
Write-Host "Test 3: 'estudiar React el miércoles de 2 a 4pm'" -ForegroundColor Yellow
$body3 = @{
    text = "estudiar React el miércoles de 2 a 4pm"
} | ConvertTo-Json

try {
    $response3 = Invoke-RestMethod -Uri "$baseUrl/events/from-text" -Method Post -Headers $headers -Body $body3
    Write-Host "✅ Éxito:" -ForegroundColor Green
    Write-Host "   Título: $($response3.event.title)" -ForegroundColor White
    Write-Host "   Fecha inicio: $($response3.event.startDate)" -ForegroundColor White
    Write-Host "   Fecha fin: $($response3.event.endDate)" -ForegroundColor White
    Write-Host "   Categoría ID: $($response3.event.eventCategoryId)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "✨ Pruebas completadas" -ForegroundColor Cyan
