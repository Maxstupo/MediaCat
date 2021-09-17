param($config='Release',$build='true',$version='0.0.0.0',$title='MediaCat',$project_name='MediaCat',$iss='mediacat-installer.iss')

$env:Path += ";C:\Program Files (x86)\Inno Setup 6;C:\Program Files\7-Zip"

$appversion = $env:APP_VERSION

# Setup script if running outside of AppVeyor. Use 0.0.0.0 for version since msbuild doesn't update the version anyway
if (!$env:APPVEYOR) {
    $env:TITLE = $title
    $env:PROJECT_NAME = $project_name
    $env:ISS = $iss

    $appversion = $version
    $env:CONFIGURATION = $config
    
    if($build -eq 'true') {
        Write-Host "################# Restoring Dependencies #################"  
        msbuild "$env:PROJECT_NAME.sln" /t:Restore
   
        Write-Host "################# Building #################"  
        msbuild "$env:PROJECT_NAME.sln" /P:Configuration=$config
    }
}



# Copy and archive all files needed for portable version 
Write-Host "################# Packaging Portable Edition #################"   
$portable_folder = "$env:TITLE-Portable_v$appversion"

New-Item -Path $portable_folder -ItemType Directory
Copy-Item -Exclude '*.pdb', '*.sqlite3' "$env:PROJECT_NAME/bin/$env:CONFIGURATION/*" $portable_folder -Force -Recurse -Verbose

# Make the portable version
7z a "$portable_folder.zip" "$portable_folder"

# Remove temp folder
Get-ChildItem -Path $portable_folder -Recurse | Remove-Item -Force -Recurse
Remove-Item $portable_folder -Force


# Make the installer
Write-Host "################# Building Installer #################"
iscc $env:ISS /DConfiguration=$env:CONFIGURATION /DApplicationVersion=$appversion


# Pause if running outside of AppVeyor
if (!$env:APPVEYOR) { pause }