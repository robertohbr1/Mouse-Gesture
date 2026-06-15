@echo off
chcp 65001 > nul
echo ===================================================
echo   Compilando MouseKeyb (Debug e Release)           
echo ===================================================
echo.

echo [1/2] Compilando em modo Debug...
dotnet build -c Debug
if %errorlevel% neq 0 (
    echo.
    echo [ERRO] Falha na compilação em modo Debug!
    pause
    exit /b %errorlevel%
)

echo.
echo [2/2] Compilando em modo Release...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo.
    echo [ERRO] Falha na compilação em modo Release!
    pause
    exit /b %errorlevel%
)

echo.
echo ===================================================
echo   Compilação finalizada com sucesso!               
echo ===================================================
echo.
pause
