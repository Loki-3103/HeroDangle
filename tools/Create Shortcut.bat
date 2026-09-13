@echo off
setlocal
title HeroDangle - Create Desktop Shortcut
set "EXE=%~dp0HeroDangle.exe"
if not exist "%EXE%" (
    echo [ERROR] HeroDangle.exe not found next to this script.
    echo Put this file in the same folder as HeroDangle.exe.
    echo.
    pause
    exit /b 1
)
set "HD_EXE=%EXE%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$exe = $env:HD_EXE; $desktop = [Environment]::GetFolderPath('Desktop'); $lnk = Join-Path $desktop 'HeroDangle.lnk'; $sh = New-Object -ComObject WScript.Shell; $s = $sh.CreateShortcut($lnk); $s.TargetPath = $exe; $s.WorkingDirectory = (Split-Path -Parent $exe); $s.Description = 'HeroDangle'; $s.Save(); Write-Host ('Shortcut created: ' + $lnk)"
if errorlevel 1 (
    echo [ERROR] Could not create shortcut.
    pause
    exit /b 1
)
echo.
echo Done! Look for "HeroDangle" on your desktop.
pause
endlocal