@setlocal
@set shellDir=%~dp0
@IF %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

pushd %shellDir%\csharp && call Build.cmd & popd

pushd %shellDir%\scala && call Build.cmd & popd
