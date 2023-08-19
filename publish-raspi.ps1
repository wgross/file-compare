[CmdletBinding()]
param(
    $ProjectToPublish = "$PSScriptRoot/src/FileCompare.Service",
    $PublishDestination = "$PSScriptRoot/raspi-service",
    $LinuxServiceName = "FileCompareSvc.service",
    $Remote = "pi@pi-plex"
)
begin {
    function dotnet_ {
        "dotnet $args" | Write-Debug
        $global:DOTNET_LASTEXITCODE = $null
        dotnet $args
        $global:DOTNET_LASTEXITCODE = $LASTEXITCODE
        "dotnet exitcode: ${global:DOTNET_LASTEXITCODE}" | Write-Debug
    }

    function scp_ {
        "scp $args" | Write-Debug
        $global:SCP_LASTEXITCODE = $null
        scp $args
        $global:SCP_LASTEXITCODE = $LASTEXITCODE
        "scp exitcode: ${global:SCP_LASTEXITCODE}" | Write-Debug
    }

    function ssh_ {
        "ssh $args" | Write-Debug
        $global:SSH_LASTEXITCODE = $null
        ssh $args
        $global:SSH_LASTEXITCODE = $LASTEXITCODE
        "ssh exitcode: ${global:SSH_LASTEXITCODE}" | Write-Debug
    }

    function rsync_ {
        $global:RSYNC_LASTEXITCODE = $null
        "rsync $args" | Write-Debug
        wsl rsync $args
        $global:RSYNC_LASTEXITCODE = $LASTEXITCODE
        "rsync exitcode: ${global:RSYNC_LASTEXITCODE}" | Write-Debug
    }
    
    function tar_ {
        $global:TAR_LASTEXITCODE = $null
        "tar $args" | Write-Debug
        wsl tar $args
        $global:TAR_LASTEXITCODE = $LASTEXITCODE
        "tar exitcode: ${global:TAR_LASTEXITCODE}" | Write-Debug
    }
}
process {
    
    # clean the publish directory first
    Remove-Item -Path "$PublishDestination\*" -Recurse -Force -ErrorAction SilentlyContinue

    # now create a published version of the service pre-compiled for arm
    $ProjectToPublish | fs-dirs\Invoke-AtContainer {
        dotnet_ publish --runtime linux-arm64 --self-contained --output $PublishDestination
    }

    # stop the service at the remote machine
    ssh_ $Remote 'sudo systemctl stop' $LinuxServiceName 

    # If publish want well push the service to raspi
    if($global:DOTNET_LASTEXITCODE -eq 0) {
        scp_  -r "$PublishDestination\*" $Remote`:~/file-compare-service
        #rsync_ -avzr --delete  "$PublishDestination\*" $Remote:~/log-service
    }
    else {
        "Skipped scp: Publish wasn't successful ($global:DOTNET_LASTEXITCODE)" | Write-Warning
        return
    }

    # make the deployed service executable
    ssh_ $Remote 'chmod ogu+x ~/file-compare-service'
    
    # publish the service definition again to the destination system
    ssh_ $Remote "sudo cp ~/file-compare-service/$LinuxServiceName /etc/systemd/system/$LinuxServiceName" 
    ssh_ $Remote 'sudo systemctl daemon-reload'
    
    $global:SSH_LASTEXITCODE | zero_is_true | throw_on_false "Publishing service definition failed"

    # start the service again at the remote machine
    ssh_ $Remote 'sudo systemctl enable' $LinuxServiceName 
    ssh_ $Remote 'sudo systemctl start' $LinuxServiceName

    $global:SSH_LASTEXITCODE | zero_is_true | throw_on_false "Starting the service failed"

    ssh_ $Remote 'sudo systemctl status' $LinuxServiceName 
}
