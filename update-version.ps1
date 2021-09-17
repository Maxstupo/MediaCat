#
# Update the build version using the last existing version tag (e.g. v1.0.0) + the build number
# Requires "APPVEYOR_TOKEN" secure variable for rest api
#

function Reset-BuildNumber() {
        $headers = @{
            "Authorization" = "Bearer $env:APPVEYOR_TOKEN"
            "Content-type" = "application/json"
            "Accept" = "application/json"
        }
        $json = @{
            nextBuildNumber = 1
        } | ConvertTo-Json
        
        Invoke-RestMethod -Method Put "https://ci.appveyor.com/api/projects/$env:APPVEYOR_ACCOUNT_NAME/$env:APPVEYOR_PROJECT_SLUG/settings/build-number" -Body $json -Headers $headers
        
}



$BUILD_NUMBER = $env:APPVEYOR_BUILD_NUMBER
$BUILD_SUFFIX = ""

if ($env:APPVEYOR_REPO_TAG -eq $true) { # Build has a tag

    $found = $env:APPVEYOR_REPO_TAG_NAME -match 'v?(\d+\.\d+\.\d+)(?:\-(.+))?'
    
    $env:BUILD_VERSION = $matches[1]
    $BUILD_SUFFIX = $matches[2]
    
    Write-Host "Setting version using commit tag: v$env:BUILD_VERSION"    
    Write-Host "Resetting build number, since tag is defined!"
    $BUILD_NUMBER = "0"
    Reset-BuildNumber
} else { # Use the last tag available
    $HASH = $(git rev-list --tags --max-count=1)
    
    if ($HASH) {
        $env:BUILD_VERSION = $(git describe --tags $HASH).TrimStart('v')
        Write-Host "Setting version using latest existing tag: v$env:BUILD_VERSION"
    } else {
        $env:BUILD_VERSION = "0.0.0"
        Write-Host "Setting version to zero, no previous tags: v$env:BUILD_VERSION"
    }
}


# "major.minor.patch .build"
$env:APP_VERSION = "$env:BUILD_VERSION.$BUILD_NUMBER"

# When building from tag, use only "major.minor.patch" else use "major.minor.patch.build"
if ($env:APPVEYOR_REPO_TAG -eq "true") {
    Write-Host "Release build detected, removing build number from version."
    $env:APP_VERSION =  $env:BUILD_VERSION
}

$env:APP_VERSION_INFORMATIONAL = "$env:APP_VERSION$BUILD_SUFFIX"

Write-Host "Branch: $env:APPVEYOR_REPO_BRANCH"
if($env:APPVEYOR_REPO_BRANCH  -notlike 'ma*') {
   $BRANCH = $env:APPVEYOR_REPO_BRANCH -replace "/","-"
   
   $env:APP_VERSION_INFORMATIONAL = "$env:APP_VERSION_INFORMATIONAL-$BRANCH"
}

Update-AppveyorBuild -Version "$env:APP_VERSION_INFORMATIONAL"
