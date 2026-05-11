param(
  [string]$SourceDir
)

$Files = Get-ChildItem -Path $SourceDir -File -Recurse
$Id = 0

$Lines = @()
$Lines += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">'
$Lines += '<Fragment>'
$Lines += '<ComponentGroup Id="AppComponents">'

foreach ($File in $Files) {
  $Id++
  $RelPath = $File.FullName.Substring($SourceDir.Length).TrimStart('\', '/')
  $FileId = "File_$Id"
  $CompId = "Comp_$Id"
  $NormalizedPath = $RelPath.Replace('\', '/')

  $Lines += "  <Component Id=`"$CompId`" Directory=`"INSTALLFOLDER`">"
  $Lines += "    <File Id=`"$FileId`" Source=`"`$(var.SourceDir)/$NormalizedPath`" />"
  $Lines += "  </Component>"
}

$Lines += '</ComponentGroup>'
$Lines += '</Fragment>'
$Lines += '</Wix>'

$Lines | Set-Content -Path "Components.wxs" -Encoding UTF8
Write-Host "Generated Components.wxs with $($Files.Count) files"
