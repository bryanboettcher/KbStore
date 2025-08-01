<#
.SYNOPSIS
    Generates a C# API client from an OpenAPI specification using Kiota.

.DESCRIPTION
    This script automates the process of generating strongly-typed C# API clients
    from OpenAPI (Swagger) specifications using Microsoft's Kiota tool. It handles
    both local files and remote URIs, manages Kiota installation, and provides
    comprehensive error handling and validation.

.PARAMETER OpenApiPath
    Path to the OpenAPI specification file (.yaml, .json) or URI to the specification.
    Default: "openapi.yaml"

.PARAMETER OutputPath
    Directory where the generated client code will be placed.
    Default: "GeneratedClient"

.PARAMETER ClientName
    Name for the generated client class and namespace. Must start with a letter and contain only alphanumeric characters.
    Default: "KbStore.ApiClient"

.PARAMETER Force
    If specified, will overwrite existing output directory without prompting.

.PARAMETER Language
    Target language for the generated client. Options: csharp, typescript, java, python, php, go, ruby, swift, shell
    Default: "csharp"

.PARAMETER Verbose
    Enable verbose output for debugging purposes.

.EXAMPLE
    .\Generate-KiotaClient.ps1
    Generates a C# client from "openapi.yaml" in the current directory.

.EXAMPLE
    .\Generate-KiotaClient.ps1 -OpenApiPath "https://api.github.com/openapi.yaml" -ClientName "GitHubClient" -Force
    Generates a GitHub API client from the remote specification, overwriting existing output.

.EXAMPLE
    .\Generate-KiotaClient.ps1 -OpenApiPath "swagger.json" -OutputPath "src/clients" -Language "typescript"
    Generates a TypeScript client from a local swagger.json file.
#>

param(
    [Parameter(Mandatory=$false, HelpMessage="Path to OpenAPI specification file or URI")]
    [ValidateNotNullOrEmpty()]
    [string]$OpenApiPath = "openapi.yaml",
    
    [Parameter(Mandatory=$false, HelpMessage="Output directory for generated client")]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath = "GeneratedClient",
    
    [Parameter(Mandatory=$false, HelpMessage="Name for the generated client class")]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9\.]*$')]
    [string]$ClientName = "KbStore.ApiClient",

    [Parameter(Mandatory=$false, HelpMessage="Overwrite existing output directory")]
    [switch]$Force,

    [Parameter(Mandatory=$false, HelpMessage="Target language for generated client")]
    [ValidateSet("csharp", "typescript", "java", "python", "php", "go", "ruby", "swift", "shell")]
    [string]$Language = "csharp",

    [Parameter(Mandatory=$false, HelpMessage="Enable verbose output")]
    [switch]$VerboseOutput
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to write colored output with timestamp
function Write-StatusMessage {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $colorMap = @{
        "Info" = "White"
        "Success" = "Green" 
        "Warning" = "Yellow"
        "Error" = "Red"
        "Progress" = "Cyan"
    }
    
    $color = $colorMap[$Type]
    Write-Host "[$timestamp] $Message" -ForegroundColor $color
}

# Function to check if dotnet tool is installed
function Test-KiotaInstalled {
    if ($VerboseOutput) {
        Write-StatusMessage "Checking if Kiota is installed..." -Type "Progress"
    }
    
    try {
        $null = Get-Command kiota -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# Function to validate URI format
function Test-ValidUri {
    param([string]$Uri)
    
    return [System.Uri]::IsWellFormedUriString($Uri, [System.UriKind]::Absolute) -and 
           $Uri -match '^https?://'
}

# Function to test network connectivity to URI
function Test-UriAccessible {
    param([string]$Uri)
    
    try {
        $response = Invoke-WebRequest -Uri $Uri -Method Get -TimeoutSec 30 -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch {
        if ($VerboseOutput) {
            Write-StatusMessage "URI test failed: $($_.Exception.Message)" -Type "Error"
        }
        return $false
    }
}

# Function to install Kiota
function Install-Kiota {
    Write-StatusMessage "Installing Microsoft Kiota..." -Type "Progress"
    Write-Progress -Activity "Setting up Kiota" -Status "Installing tool..." -PercentComplete 25
    
    try {
        $installResult = dotnet tool install --global Microsoft.OpenApi.Kiota 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-StatusMessage "Kiota installed successfully!" -Type "Success"
            return $true
        } else {
            Write-StatusMessage "Failed to install Kiota. Exit code: $LASTEXITCODE" -Type "Error"
            if ($VerboseOutput) {
                Write-StatusMessage "Install output: $installResult" -Type "Error"
            }
            return $false
        }
    }
    catch {
        Write-StatusMessage "Failed to install Kiota: $($_.Exception.Message)" -Type "Error"
        return $false
    }
    finally {
        Write-Progress -Activity "Setting up Kiota" -Completed
    }
}

# Main script execution
try {
    Write-StatusMessage "Starting Kiota client generation process" -Type "Info"
    Write-StatusMessage "OpenAPI Path: $OpenApiPath" -Type "Info"
    Write-StatusMessage "Output Path: $OutputPath" -Type "Info"
    Write-StatusMessage "Client Name: $ClientName" -Type "Info"
    Write-StatusMessage "Language: $Language" -Type "Info"

    # Step 1: Check and install Kiota if needed
    Write-Progress -Activity "Generating API Client" -Status "Checking dependencies..." -PercentComplete 10

    if (-not (Test-KiotaInstalled)) {
        Write-StatusMessage "Kiota not found. Installing..." -Type "Warning"
        
        if (-not (Install-Kiota)) {
            Write-StatusMessage "Cannot proceed without Kiota installation" -Type "Error"
            exit 1
        }
    } else {
        Write-StatusMessage "Kiota is already installed" -Type "Success"
    }

    # Step 2: Validate OpenAPI source
    Write-Progress -Activity "Generating API Client" -Status "Validating OpenAPI source..." -PercentComplete 25

    $isOpenApiUri = Test-ValidUri $OpenApiPath

    if ($isOpenApiUri) {
        Write-StatusMessage "Detected URI format: $OpenApiPath" -Type "Info"
        
        if (-not (Test-UriAccessible $OpenApiPath)) {
            Write-StatusMessage "Cannot access OpenAPI URI: $OpenApiPath" -Type "Error"
            Write-StatusMessage "Please check the URL and your network connection" -Type "Error"
            exit 1
        }
        
        Write-StatusMessage "OpenAPI URI is accessible" -Type "Success"
    } else {
        Write-StatusMessage "Detected local file path: $OpenApiPath" -Type "Info"
        
        if (-not (Test-Path $OpenApiPath)) {
            Write-StatusMessage "OpenAPI file not found: $OpenApiPath" -Type "Error"
            Write-StatusMessage "Please check the file path and ensure the file exists" -Type "Error"
            exit 1
        }
        
        $fileInfo = Get-Item $OpenApiPath
        Write-StatusMessage "Using local OpenAPI file: $($fileInfo.FullName) (Size: $($fileInfo.Length) bytes)" -Type "Success"
    }

    # Step 3: Handle output directory
    Write-Progress -Activity "Generating API Client" -Status "Preparing output directory..." -PercentComplete 40

    if (Test-Path $OutputPath) {
        if ($Force) {
            Write-StatusMessage "Removing existing output directory (Force specified)" -Type "Warning"
            Remove-Item $OutputPath -Recurse -Force
        } else {
            Write-StatusMessage "Output directory already exists: $OutputPath" -Type "Warning"
            Write-StatusMessage "Use -Force parameter to overwrite existing directory" -Type "Warning"
        }
    }

    if (-not (Test-Path $OutputPath)) {
        $null = New-Item -ItemType Directory -Path $OutputPath -Force
        Write-StatusMessage "Created output directory: $OutputPath" -Type "Success"
    }

    # Step 4: Generate client
    Write-Progress -Activity "Generating API Client" -Status "Generating client code..." -PercentComplete 60

    Write-StatusMessage "Generating $Language client from $OpenApiPath to $OutputPath..." -Type "Progress"

    $kiotaArgs = @(
        "generate",
        "-l", $Language,
        "-n", $ClientName,
        "-c", $ClientName,
        "-o", $OutputPath,
        "--openapi", $OpenApiPath
    )

    if ($VerboseOutput) {
        Write-StatusMessage "Kiota command: kiota $($kiotaArgs -join ' ')" -Type "Info"
    }

    $generateResult = & kiota @kiotaArgs 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Progress -Activity "Generating API Client" -Status "Completed successfully!" -PercentComplete 100
        Write-StatusMessage "Client generated successfully!" -Type "Success"
        
        # Display summary information
        $outputInfo = Get-ChildItem $OutputPath -Recurse -File
        Write-StatusMessage "Generated $($outputInfo.Count) files in $OutputPath" -Type "Info"
        
        if ($VerboseOutput) {
            Write-StatusMessage "Generated files:" -Type "Info" 
            $outputInfo | ForEach-Object { 
                Write-StatusMessage "  - $($_.Name) ($($_.Length) bytes)" -Type "Info"
            }
        }
    } else {
        Write-StatusMessage "Failed to generate client. Exit code: $LASTEXITCODE" -Type "Error"
        if ($VerboseOutput -and $generateResult) {
            Write-StatusMessage "Kiota output: $generateResult" -Type "Error"
        }
        exit 1
    }

    Write-Progress -Activity "Generating API Client" -Completed
    Write-StatusMessage "Process completed successfully!" -Type "Success"

} catch {
    Write-StatusMessage "An unexpected error occurred: $($_.Exception.Message)" -Type "Error"
    
    if ($VerboseOutput) {
        Write-StatusMessage "Stack trace: $($_.ScriptStackTrace)" -Type "Error"
    }
    
    Write-StatusMessage "Please check your parameters and try again" -Type "Error"
    exit 1
} finally {
    # Cleanup progress indicators
    Write-Progress -Activity "Generating API Client" -Completed
}