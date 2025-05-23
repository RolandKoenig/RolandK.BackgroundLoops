# Cleanup solution
dotnet clean "../RolandK.BackgroundLoops.sln"

# Delete all bin and obj directories
$directories = Get-ChildItem "../src/" -include bin,obj -Recurse
foreach ($actDirectory in $directories)
{
	"Deleting $actDirectory"
	remove-item $actDirectory.fullname -Force -Recurse
}

# Clear previous artifacts
if (Test-Path "../publish")
{
	"Clearing ../publish"
	Remove-Item -Path "../publish/*" -Recurse -Force;
}