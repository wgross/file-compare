[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateScript({ $_ | Test-Path -PathType Container })]
    $Path,

    [Parameter()]
    $Uri = "http://192.168.178.61:5000/files"
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

        Get-ChildItem -Path $Path -File -Recurse | ForEach-Object {
            $relativePath = $_ | Resolve-Path -Relative
            $relativePath | Write-Verbose
 
            $file = [PSCustomObject]@{
                Host     = $env:ComputerName
                Name     = $_.Name
                FullName = $relativePath
                Hash     = (Get-FileHash $_).Hash 
            }

            $files += $file

            if ($files.Length -eq 10) {
                try {
                    "Uploading.." | Write-Verbose
                    $request.Body = $files | ConvertTo-Json -Depth 10
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
                $request.Body = $files | ConvertTo-Json -Depth 10
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
