# Build, test, and publish Nexus.GameEngine to local repository

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\packages",
    [string]$LocalRepoPath = "$env:USERPROFILE\source\LocalNuGetRepo",
    [string]$SourceName = "Nexus-GameEngine-Local",
    [string]$VersionSuffix = "dev",
    [switch]$SkipTests,
    [switch]$SkipPublish,
    [switch]$SkipSetup,
    [switch]$Clean
)

# Generate pre-release version with timestamp for tracking
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$preReleaseVersion = "$VersionSuffix.$timestamp"

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
    dotnet build Nexus.GameEngine.sln --configuration $Configuration --no-restore --version-suffix $preReleaseVersion
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    
    # Run tests
    if (-not $SkipTests) {
        Write-Host "Running tests..." -ForegroundColor Green
        dotnet test Nexus.GameEngine.sln --configuration $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    }
    
    # Pack NuGet packages
    Write-Host "Creating NuGet packages with version suffix: $preReleaseVersion..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    dotnet pack "GameEngine\GameEngine.csproj" --configuration $Configuration --no-build --output $OutputPath --version-suffix $preReleaseVersion
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
            
            dotnet nuget push $package.FullName --source $SourceName --skip-duplicate
            if ($LASTEXITCODE -ne 0) {
                dotnet nuget push $package.FullName --source $LocalRepoPath --skip-duplicate
                if ($LASTEXITCODE -ne 0) {
                    Copy-Item $package.FullName $LocalRepoPath -Force
                }
            }
            
            # Publish symbol package if it exists
            $symbolPackage = $package.FullName -replace "\.nupkg$", ".symbols.nupkg"
            if (Test-Path $symbolPackage) {
                dotnet nuget push $symbolPackage --source $SourceName --skip-duplicate
                if ($LASTEXITCODE -ne 0) {
                    dotnet nuget push $symbolPackage --source $LocalRepoPath --skip-duplicate
                    if ($LASTEXITCODE -ne 0) {
                        Copy-Item $symbolPackage $LocalRepoPath -Force
                    }
                }
            }
        }
    }
    
    Write-Host "Build, test, and publish complete!" -ForegroundColor Green
    Write-Host "Version: $preReleaseVersion" -ForegroundColor Cyan
    Write-Host "Packages: $((Resolve-Path $OutputPath).Path)" -ForegroundColor Cyan
    if (-not $SkipPublish) {
        Write-Host "Local repo: $LocalRepoPath" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
