Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   Compilando MouseKeyb (Debug e Release)           " -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/2] Compilando em modo Debug..." -ForegroundColor Yellow
dotnet build -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERRO] Falha na compilação em modo Debug!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair..."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/2] Compilando em modo Release..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERRO] Falha na compilação em modo Release!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair..."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "   Compilação finalizada com sucesso!               " -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green
Write-Host ""
Read-Host "Pressione Enter para finalizar..."
