function CopyLibraries  {
    param(
        [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
        [String] $vsExtensionsDirectory,
        [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
        [String] $copyDirectory
    )
    process{
        $item = Get-ChildItem -Path "$vsExtensionsDirectory" -Include "Microsoft.VisualStudio.TeamFoundation.dll" -Recurse
        Write-Host $item.FullName
        if (Test-Path $item.FullName) {
            Copy-Item -Path $item.FullName -Destination $copyDirectory
        }
        $item = Get-ChildItem -Path "$vsExtensionsDirectory" -Include "Microsoft.VisualStudio.TeamFoundation.VersionControl.dll" -Recurse
        if (Test-Path $item.FullName) {
            Copy-Item -Path $item.FullName -Destination $copyDirectory
        }
    }   
}

function GetVisualStudioExtensionsDirectory {
    process {
        return "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\Extensions"
    }
}

function GetLibrariesDirectory {
    process {
        $path = [io.path]::combine($PSScriptRoot, '..', 'Lib\MS\Visual Studio\14.0')
        if (-Not (Test-Path -PathType Container $path)) {
            New-Item -ItemType Directory -Force -Path $path
        }
        return $path
    }
}

$extensionDir = GetVisualStudioExtensionsDirectory
$targetDir = GetLibrariesDirectory
CopyLibraries "$extensionDir" "$targetDir"