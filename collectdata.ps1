[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateScript({ $_ | Test-Path -PathType Container })]
    $Path
)
process {
    $PWD | Write-Verbose
    $PWD | Invoke-AtContainer { 
        $files = @()
        $request = @{
            Uri      = "http://localhost:5000/files"
            Method   = "Post"
            ContentType = "application/json"
        }

        Get-ChildItem -Path $Path -File -Recurse | ForEach-Object {
            $relativePath = $_ | Resolve-Path -Relative
            $relativePath | Write-Verbose
 
            $file = [PSCustomObject]@{
                Host     = $env:COMPUTERNAME
                Name     = $_.Name
                FullName = $relativePath
                Hash     = (Get-FileHash $_).Hash 
            }

            $files += $file

            if ($files.Length -eq 10) {
                "Uploading.." | Write-Verbose
                $request.Body = $files | ConvertTo-Json -Depth 10
                Invoke-RestMethod @request
                Write-Host $request.Body
                $files = @()
            }    
        }
    }
}
