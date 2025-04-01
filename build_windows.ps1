$publishPath = [System.IO.Path]::Combine($env:TEMP, "TypoChecker_Publish")
dotnet publish TypoChecker.UI.Desktop -r win-x64 -c Release -o $publishPath
explorer $publishPath