param(
  [Parameter(mandatory=$true, helpmessage="Directory with Sourcecode Files")]
  [ValidateNotNullOrEmpty()]
  [string]$SourcesDirectory,
  [Parameter(mandatory=$true, helpmessage="Build Label from TFS for last part of Version Number e.g. 'NexusInt_EngineA_AIT_13.110.20211.4'")]
  [ValidateNotNullOrEmpty()]
  [string]$BuildNumber
)

New-Variable ProjectSettingsFile -value "project.properties" -option ReadOnly

function DetermineVersion()
{
    #Determine full version string; get last element after underline
    $version = $BuildNumber.Split("_")
    $version = $version[-1]

    #Remove year from third position
    $date = $version.Split(".")
    $date[2] = $date[2].Substring(4, 4)

    #Merge new version string with reduced date
    $version = $date -join '.'

    return $version
}

$version = DetermineVersion

$files = (Get-ChildItem -Path $SourcesDirectory -Include 'AssemblyInfo.cs', 'GlobalAssemblyInfo.cs' -Recurse)

ForEach( $file in $files) 
{
  $newVersion = 'AssemblyVersion("' + $version + '")';
  $newFileVersion = 'AssemblyFileVersion("' + $version + '")';

  $tmpFile = $file.FullName + ".tmp"

  get-content $file.FullName | 
    %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $newVersion } |
    %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $newFileVersion }  > $tmpFile

  Move-Item $tmpFile $file.FullName -force

  Write-Host "Version number" $version "has been successfully written to file" $file
}