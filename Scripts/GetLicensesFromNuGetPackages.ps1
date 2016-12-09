# Adapted from https://gist.github.com/dmoonfire/29e17fe6325feef03b50#file-download-nuget-licenses-ps1-L2
# This script downloads the license of each package in the solution into the folder ..\Licenses relative to the solution folder.
# Execute the script in the package manager console.

# Make sure we are running inside Visual Studio's NuGet PowerShell.
if (-not($DTE))
{
	Write-Error "You must run this inside Visual Studio's NuGet Powershell."
	return 1
}

$downloader = New-Object System.Net.WebClient;
@( Get-Project -All | 
? { $_.ProjectName } | 
% { Get-Package -ProjectName $_.ProjectName } ) | 
Sort -Unique Id | # remove duplicates and "no packages installed" entries (happens if a project does not have any packages)
% { 
	$pkg = $_;
	Try { 
		$downloader.DownloadFile($pkg.LicenseUrl, [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($DTE.Solution.FullName), '..\Licenses\', $pkg.Id + ".txt"))
		Write-Host "Downloaded license for package $($pkg.Id) from `"$($pkg.LicenseUrl)`"."
	} 
	Catch [system.exception] {
		Write-Host "Could not download license for package $($pkg.Id) from $($pkg.LicenseUrl). Message:  $($_.Exception)"
	}
}