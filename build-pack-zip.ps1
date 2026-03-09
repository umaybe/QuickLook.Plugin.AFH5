dotnet build -c Release

Remove-Item QuickLook.Plugin.AFH5.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path bin\Release\ -Exclude *.pdb,*.xml
Compress-Archive $files QuickLook.Plugin.AFH5.zip
Move-Item QuickLook.Plugin.AFH5.zip QuickLook.Plugin.AFH5.qlplugin
