@echo off
chcp 65001 > nul
echo ===================================================
echo   Executando testes unitários do MouseKeyb         
echo ===================================================
echo.

dotnet test
if %errorlevel% neq 0 (
    echo.
    echo [ERRO] Um ou mais testes falharam!
    pause
    exit /b %errorlevel%
)

echo.
echo ===================================================
echo   Todos os testes passaram com sucesso!            
echo ===================================================
echo.
pause
