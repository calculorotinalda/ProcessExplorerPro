# PowerShell script to build Process Explorer Pro and generate the installer

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Building Process Explorer Pro..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# 1. Clean previous builds
Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow
Remove-Item -Path "bin", "obj" -Recurse -ErrorAction SilentlyContinue

# 2. Publish the WPF application as a self-contained single-file exe
Write-Host "Publishing self-contained single-file executable..." -ForegroundColor Yellow
dotnet publish -c Release --self-contained true -r win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Dotnet publish failed!" -ForegroundColor Red
    Exit $LASTEXITCODE
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Executable generated at: bin\Release\net9.0-windows\win-x64\publish\ProcessExplorerPro.exe`n" -ForegroundColor Green

# 3. Compile Inno Setup Installer if ISCC is installed
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $isccPath) {
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Compiling Inno Setup Installer..." -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    
    & $isccPath setup.iss
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nInstaller created successfully at: setup-output\ProcessExplorerPro_Setup.exe" -ForegroundColor Green
    } else {
        Write-Host "`nError: Inno Setup compilation failed!" -ForegroundColor Red
    }
} else {
    Write-Host "Inno Setup compiler (ISCC.exe) not found at: $isccPath" -ForegroundColor Yellow
    Write-Host "Skipping installer generation. You can install Inno Setup 6 to compile 'setup.iss'." -ForegroundColor Yellow
}
