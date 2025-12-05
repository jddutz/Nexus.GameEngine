# Build, test, and publish Nexus.GameEngine to local repository

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\packages",
    [string]$LocalRepoPath = "$env:USERPROFILE\source\LocalNuGetRepo",
    [string]$SourceName = "NuGet-Local",
    #[string]$VersionSuffix = "dev", # Not needed for semver build number
    [switch]$SkipTests,
    [switch]$SkipPublish,
    [switch]$SkipSetup,
    [switch]$Clean
)


# Extract base version from csproj
$csprojPath = "src\GameEngine\GameEngine.csproj"
$baseVersion = Select-String -Path $csprojPath -Pattern "<Version>(.*?)</Version>" | ForEach-Object { $_.Matches[0].Groups[1].Value }
if (-not $baseVersion) { $baseVersion = "1.0.0" }
# Generate build number (date-based)
$buildNumber = Get-Date -Format "yyMMddHHmm"
# Compose semver version (NuGet prerelease label)
$semverVersion = "$baseVersion-$buildNumber"

# Resolve workspace root 
$WorkspaceRoot = $PSScriptRoot

Set-Location $WorkspaceRoot

try {
    # Setup local NuGet repository
    if (-not $SkipSetup) {
        Write-Host "Setting up local NuGet repository..." -ForegroundColor Green
        
        if (!(Test-Path $LocalRepoPath)) {
            New-Item -ItemType Directory -Path $LocalRepoPath -Force | Out-Null
            Write-Host "Created local repository: $LocalRepoPath" -ForegroundColor Yellow
        }
        
        # Update NuGet.config
        $nugetConfigPath = ".\NuGet.config"
        if (Test-Path $nugetConfigPath) {
            $nugetConfig = Get-Content $nugetConfigPath -Raw
            $updatedConfig = $nugetConfig -replace "PLACEHOLDER_LOCAL_REPO_PATH", $LocalRepoPath
            Set-Content $nugetConfigPath -Value $updatedConfig -Encoding UTF8
        }
        else {
            $existingSources = dotnet nuget list source
            if (-not ($existingSources -match $SourceName)) {
                dotnet nuget add source $LocalRepoPath --name $SourceName
            }
        }
        
        dotnet nuget locals all --clear
    }
    
    # Clean if requested
    if ($Clean) {
        Write-Host "Cleaning previous builds..." -ForegroundColor Green
        dotnet clean Nexus.GameEngine.sln --configuration $Configuration
        if (Test-Path $OutputPath) {
            Remove-Item $OutputPath -Recurse -Force
        }
    }
    
    # Restore dependencies
    Write-Host "Restoring dependencies..." -ForegroundColor Green
    dotnet restore Nexus.GameEngine.sln
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
    
    # Build solution
    Write-Host "Building solution ($Configuration)..." -ForegroundColor Green
    dotnet build Nexus.GameEngine.sln --configuration $Configuration --no-restore /p:Version=$semverVersion
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    
    # Run tests
    if (-not $SkipTests) {
        Write-Host "Running tests..." -ForegroundColor Green
        dotnet test Nexus.GameEngine.sln --configuration $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    }
    
    # Pack NuGet packages
    Write-Host "Creating NuGet packages with version: $semverVersion..." -ForegroundColor Green
    
    # Clean packages folder before creating new packages
    if (Test-Path $OutputPath) {
        Write-Host "Cleaning packages folder..." -ForegroundColor Yellow
        Remove-Item $OutputPath -Recurse -Force
    }
    
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    dotnet pack $csprojPath --configuration $Configuration --no-build --output $OutputPath /p:Version=$semverVersion
    if ($LASTEXITCODE -ne 0) { throw "Packaging failed" }
    
    Write-Host "Packages created:" -ForegroundColor Cyan
    Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | ForEach-Object {
        Write-Host "  $($_.Name)" -ForegroundColor White
    }
    
    # Publish to local repository
    if (-not $SkipPublish) {
        Write-Host "Publishing to local repository..." -ForegroundColor Green
        
        if (!(Test-Path $LocalRepoPath)) {
            throw "Local repository not found: $LocalRepoPath"
        }
        
        $packages = Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | Where-Object { !$_.Name.EndsWith(".symbols.nupkg") }
        
        foreach ($package in $packages) {
            Write-Host "Publishing $($package.Name)..." -ForegroundColor Yellow
            
            dotnet nuget push $package.FullName --source $SourceName
            if ($LASTEXITCODE -ne 0) {
                dotnet nuget push $package.FullName --source $LocalRepoPath
                if ($LASTEXITCODE -ne 0) {
                    Copy-Item $package.FullName $LocalRepoPath -Force
                }
            }
            
            # Publish symbol package if it exists
            $symbolPackage = $package.FullName -replace "\.nupkg$", ".symbols.nupkg"
            if (Test-Path $symbolPackage) {
                dotnet nuget push $symbolPackage --source $SourceName
                if ($LASTEXITCODE -ne 0) {
                    dotnet nuget push $symbolPackage --source $LocalRepoPath
                    if ($LASTEXITCODE -ne 0) {
                        Copy-Item $symbolPackage $LocalRepoPath -Force
                    }
                }
            }
        }
    }
    
    Write-Host "Build, test, and publish complete!" -ForegroundColor Green
    Write-Host "Version: $semverVersion" -ForegroundColor Cyan
    Write-Host "Packages: $((Resolve-Path $OutputPath).Path)" -ForegroundColor Cyan
    if (-not $SkipPublish) {
        Write-Host "Local repo: $LocalRepoPath" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
