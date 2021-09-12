$env:Path += ";C:\Program Files (x86)\Inno Setup 6"

$version = $env:APPVEYOR_BUILD_VERSION

# When building from tag, use only "major.minor.patch" else use "major.minor.patch.build"
if ($env:APPVEYOR_REPO_TAG -eq $true) {
    $version = $env:BUILD_VERSION
}
    
$portable_folder = "MediaCat-Portable_v$version"

New-Item -Path $portable_folder -ItemType Directory

# Copy all files needed for portable version
Write-Host "Building portable distro"
Copy-Item "MediaCat/bin/$env:CONFIGURATION/*" $portable_folder -Force -Recurse -Verbose

# Make the portable version
7z a "$portable_folder.zip" "$portable_folder"

# Make the installer
Write-Host "Building installer distro"
iscc mediacat-installer.iss /DConfiguration=$env:CONFIGURATION /DApplicationVersion=$version