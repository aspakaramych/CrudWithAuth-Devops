# CrudWithAuth Windows Deployment Wrapper
# Automates execution of deploy.sh inside WSL if available, or guides the developer.

$Title = @"
================================================================
          CrudWithAuth Deployment Orchestrator (Windows)         
================================================================
"@

Write-Host $Title -ForegroundColor Cyan

# Check for WSL
$wslCheck = Get-Command wsl -ErrorAction SilentlyContinue

if ($wslCheck) {
    Write-Host "[INFO] WSL (Windows Subsystem for Linux) detected." -ForegroundColor Blue
    Write-Host "[INFO] Forwarding execution to deploy.sh within WSL..." -ForegroundColor Blue
    
    # Format arguments to pass to WSL bash script
    $wslArgs = @()
    if ($args.Count -gt 0) {
        $wslArgs += $args
    }
    
    # Run bash script in WSL. We convert Windows paths if necessary.
    wsl bash -c "chmod +x ./deploy.sh && ./deploy.sh $wslArgs"
} else {
    Write-Host "[WARNING] WSL is not installed on this system." -ForegroundColor Yellow
    Write-Host "[WARNING] Ansible control nodes cannot run natively on Windows without WSL or Cygwin." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To deploy using Ansible from Windows, you have two options:" -ForegroundColor White
    Write-Host "  1. Install WSL: Run 'wsl --install' in an Admin PowerShell and restart." -ForegroundColor Green
    Write-Host "  2. Use a Linux control node: SSH to your control node and execute:" -ForegroundColor Green
    Write-Host "     ansible-playbook -i ansible/k8s_inventory.ini ansible/k8s_deploy.yml" -ForegroundColor Green
    Write-Host ""
    Write-Host "If you have an SSH client installed, you can connect to your master node at 192.168.1.10 directly." -ForegroundColor Yellow
}
