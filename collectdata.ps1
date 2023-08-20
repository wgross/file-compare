[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateScript({ $_ | Test-Path -PathType Container })]
    $Path,

    [Parameter()]
    $Uri = "http://pi-plex:5000/files",

    [Parameter()]
    [int]$BatchSize = 10
)
process {
    $PWD | Write-Verbose
    $PWD | fs-dirs\Invoke-AtContainer { 
        $files = @()
        $request = @{
            Uri         = $Uri
            Method      = "Post"
            ContentType = "application/json;charset=UTF-16"
        }

        Get-ChildItem -Path $Path -File -Recurse | Where-Object FullName -notlike "*\.*" | ForEach-Object {
            $relativePath = $_ | Resolve-Path -Relative
            $relativePath | Write-Verbose
 
            $file = [PSCustomObject]@{
                Host     = $env:ComputerName
                Name     = $_.Name
                FullName = $relativePath
                Hash     = (Get-FileHash $_).Hash
                Updated  = Get-Date -AsUTC
                Length  = $_.Length
                CreationTimeUtc = $_.CreationTimeUtc
                LastAccessTimeUtc = $_.LastAccessTimeUtc
                LastWriteTimeUtc = $_.LastWriteTimeUtc
            }

            $files += $file

            if ($files.Length -eq $BatchSize) {
                try {
                    "Uploading.." | Write-Verbose
                    $request.Body = $files | ConvertTo-Json -Depth 5
                    $request | ConvertTo-Json -Depth 3 | Write-Verbose
                
                    Invoke-RestMethod @request

                    $files | Select-Object Name
                    $files = @()

                }
                catch {
                    $_ | Write-Error
                    $request | ConvertTo-Json -Depth 3 | Write-Verbose
                }
            }    
        }

        if ($files.Length -gt 0) {
            try {
                "Uploading remainder.." | Write-Verbose
                $request.Body = $files | ConvertTo-Json -Depth 5
                $request | ConvertTo-Json -Depth 3 | Write-Verbose
                
                Invoke-RestMethod @request

                $files | Select-Object Name
            }
            catch {
                $_ | Write-Error
                $request | ConvertTo-Json -Depth 3 | Write-Error
            }
        }
    }
}
