
write-host("Checking for changes in source code in $([Environment]::CurrentDirectory)")
$srcDirectory = "$([Environment]::CurrentDirectory)\Src\Azure.Functions.Testing\"

$sourceChanged = $false
foreach ($_ in (git diff `@~..@ --name-only $srcDirectory)) 
{
	$sourceChanged = $true
	break
}
if ($sourceChanged)
{
	write-host("Source code change detected, new NuGet package will be build")
	write-host("##vso[task.setvariable variable=sourceupdated]true")
}
else 
{
	write-host("No source code change detected, NuGet package publish will be skipped")
	write-host("##vso[task.setvariable variable=sourceupdated]false")
}