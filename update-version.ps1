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

if ($env:APPVEYOR_REPO_TAG -eq $true) { # Build has a tag
    $env:BUILD_VERSION = $env:APPVEYOR_REPO_TAG_NAME.TrimStart('v')        
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

Update-AppveyorBuild -Version "$env:BUILD_VERSION.$BUILD_NUMBER"

$env:BUILD_VERSION = $env:BUILD_VERSION.split(".")[0..2] -join '.'