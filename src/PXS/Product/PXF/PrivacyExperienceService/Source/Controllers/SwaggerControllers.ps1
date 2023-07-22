$RouteNameCS = "E:\git\MEE.Privacy.Experience.Svc\Product\PXF\Contracts\PrivacyExperience\Source\Service\RouteNames.cs"

$RouteDictionary = @{}
Get-Content -Path $RouteNameCS | Where-Object { $_ -match "(\s*public const string (?<RouteName>[\w\d_]*) = @?`"(?<RoutePath>.*)`")" } | ForEach-Object {
    $RouteDictionary.Add($Matches["RouteName"], $Matches["RoutePath"])
}

Get-ChildItem -Path "E:\git\MEE.Privacy.Experience.Svc\Product\PXF\PrivacyExperienceService\Source\Controllers" -Recurse -Filter "*Controller.cs" | ForEach-Object {
    $lines = Get-Content -Path $_.FullName

    Out-File -FilePath $_.FullName -Encoding utf8 -NoNewline -InputObject "" 
    $currentGroup = ""
    foreach ($line in $lines) {
        if ($line -match "class (?<ControllerName>[\w\d]*)Controller") {
            $currentGroup = $Matches["ControllerName"] -csplit "(?=[A-Z])" -ne "" -join " "
        }

        if ($line -match "\[Http(?<Verb>\w*)") {
            $verb = $Matches["Verb"].ToLowerInvariant()
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <group>$currentGroup</group>"
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <verb>$verb</verb>"
        }

        if ($line -match "\[Route\(RouteNames.(?<RouteKey>[\w\d]*)\)\]") {
            $path = $RouteDictionary[$Matches["RouteKey"]] 
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <url>https://pxs.api.account.microsoft.com/$path</url>"
        }

        if ($line -match "\[Route\(@?`"(?<RouteKey>.*)`"\)\]") {
            $path = $Matches["RouteKey"]
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <url>https://pxs.api.account.microsoft.com/$path</url>"
        }

        if ($line -match "\[ODataRoute\(RouteNames.(?<RouteKey>[\w\d]*)\)\]") {
            $path = $RouteDictionary[$Matches["RouteKey"]] 
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <url>https://pxs.api.account.microsoft.com/$path</url>"
        }

        if ($line -match "\[ODataRoute\(@?`"(?<RouteKey>.*)`"\)\]") {
            $path = $Matches["RouteKey"]
            Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject "        /// <url>https://pxs.api.account.microsoft.com/$path</url>"
        }

        Out-File -FilePath $_.FullName -Encoding utf8 -Append -InputObject $line
    }
}