# AriaSharp installer for Windows (x64).
#   irm https://raw.githubusercontent.com/migueljocode/AriaSharp/main/scripts/install.ps1 | iex
# Environment:
#   ARIASHARP_VERSION      release tag to install (default: latest)
#   ARIASHARP_INSTALL_DIR  target directory (default: %LOCALAPPDATA%\Programs\AriaSharp)

function Install-AriaSharp {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'

    $repo = 'migueljocode/AriaSharp'
    $version = if ($env:ARIASHARP_VERSION) { $env:ARIASHARP_VERSION } else { 'latest' }
    $dest = if ($env:ARIASHARP_INSTALL_DIR) { $env:ARIASHARP_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA 'Programs\AriaSharp' }

    if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne 'X64') {
        throw 'Unsupported CPU: only Windows x64 builds are published.'
    }

    $base = if ($version -eq 'latest') {
        "https://github.com/$repo/releases/latest/download"
    } else {
        "https://github.com/$repo/releases/download/$version"
    }
    $file = 'ariasharp-win-x64.zip'

    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('ariasharp-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    try {
        Write-Host "Installing AriaSharp ($version, win-x64) to $dest"
        Invoke-WebRequest -UseBasicParsing -Uri "$base/$file" -OutFile (Join-Path $tmp $file)
        Invoke-WebRequest -UseBasicParsing -Uri "$base/SHA256SUMS" -OutFile (Join-Path $tmp 'SHA256SUMS')

        $pattern = '\s\*?' + [regex]::Escape($file) + '$'
        $line = Get-Content (Join-Path $tmp 'SHA256SUMS') | Where-Object { $_ -match $pattern } | Select-Object -First 1
        if (-not $line) { throw "No checksum for $file in SHA256SUMS" }
        $expected = ($line -split '\s+')[0].ToLowerInvariant()
        $actual = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $tmp $file)).Hash.ToLowerInvariant()
        if ($expected -ne $actual) { throw "Checksum mismatch for $file" }

        Expand-Archive -Path (Join-Path $tmp $file) -DestinationPath $tmp -Force
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
        Copy-Item -Path (Join-Path $tmp 'ariasharp.exe') -Destination (Join-Path $dest 'ariasharp.exe') -Force
    }
    finally {
        Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
    }

    Write-Host "Installed $(Join-Path $dest 'ariasharp.exe')"

    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    if (($userPath -split ';') -notcontains $dest) {
        $newPath = (@($userPath, $dest) | Where-Object { $_ }) -join ';'
        [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
        $env:Path = "$env:Path;$dest"
        Write-Host "Added $dest to your user PATH (new terminals pick it up)."
    }

    Write-Host ''
    Write-Host 'Next: ariasharp --install-deps -y   # installs aria2 if it is missing'
}

Install-AriaSharp
