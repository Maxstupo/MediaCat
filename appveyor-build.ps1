param ($config='Release')

$env:Path += ";C:\Program Files (x86)\Inno Setup 6;C:\Program Files\7-Zip"

$appversion = $env:APP_VERSION

# Setup script if running outside of AppVeyor. Use 0.0.0.0 for version since msbuild doesn't update the version anyway
if (!$env:APPVEYOR) {
    $appversion = "0.0.0.0"
    $env:CONFIGURATION = $config
    
    Write-Host "################# Restoring Dependencies #################"  
    msbuild MediaCat.sln /t:Restore
   
    Write-Host "################# Building #################"  
    msbuild MediaCat.sln /P:Configuration=$config
}



# Copy and archive all files needed for portable version 
Write-Host "################# Packaging Portable Edition #################"   
$portable_folder = "MediaCat-Portable_v$appversion"

New-Item -Path $portable_folder -ItemType Directory
Copy-Item -Exclude '*.pdb', '*.sqlite3' "MediaCat/bin/$env:CONFIGURATION/*" $portable_folder -Force -Recurse -Verbose

# Make the portable version
7z a "$portable_folder.zip" "$portable_folder"

# Remove temp folder
Get-ChildItem -Path $portable_folder -Recurse | Remove-Item -Force -Recurse
Remove-Item $portable_folder -Force


# Make the installer
Write-Host "################# Building Installer #################"
iscc mediacat-installer.iss /DConfiguration=$env:CONFIGURATION /DApplicationVersion=$appversion


# Pause if running outside of AppVeyor
if (!$env:APPVEYOR) { pause }