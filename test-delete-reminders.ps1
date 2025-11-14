# Script para gestionar reminders

# Primero, obtener todos los reminders para ver el estado actual
Write-Host "=== OBTENIENDO TODOS LOS REMINDERS ==="
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5291/api/reminders/pending" -Method GET -ContentType "application/json"
    Write-Host "Reminders encontrados: $($response.Length)"
    $response | ForEach-Object {
        Write-Host "- ID: $($_.id), Evento: $($_.eventTitle), Trigger: $($_.triggerDateTime)"
    }
} catch {
    Write-Host "Error al obtener reminders o no hay reminders pendientes: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== ELIMINANDO TODOS LOS REMINDERS ==="
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5291/api/reminders/all" -Method DELETE -ContentType "application/json"
    Write-Host "Todos los reminders han sido eliminados exitosamente"
} catch {
    Write-Host "Error al eliminar reminders: $($_.Exception.Message)"
    Write-Host "Respuesta: $($_.Exception.Response)"
}

Write-Host ""
Write-Host "=== VERIFICANDO ELIMINACION ==="
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5291/api/reminders/pending" -Method GET -ContentType "application/json"
    Write-Host "Reminders restantes: $($response.Length)"
    if ($response.Length -eq 0) {
        Write-Host "Confirmado: No quedan reminders en el sistema"
    } else {
        Write-Host "Aun quedan $($response.Length) reminders"
    }
} catch {
    Write-Host "Error al verificar o no hay reminders: $($_.Exception.Message)"
}