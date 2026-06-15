Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   Executando testes unitários do MouseKeyb         " -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

dotnet test
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERRO] Um ou mais testes falharam!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair..."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "   Todos os testes passaram com sucesso!            " -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green
Write-Host ""
Read-Host "Pressione Enter para finalizar..."
