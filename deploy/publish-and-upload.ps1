# =============================================================
#  Bolcko.Web - Windows Publish & Transfer Script (PowerShell)
#  يشغّل من جهاز Windows ويرفع الملفات للسيرفر عبر SCP
# =============================================================

param(
    [string]$ServerIP   = "YOUR_SERVER_IP",    # ← غيّر هنا
    [string]$ServerUser = "root",               # ← غيّر هنا
    [string]$ServerPath = "/tmp/bolcko-deploy"
)

$ErrorActionPreference = "Stop"
$SolutionDir = $PSScriptRoot | Split-Path -Parent
$PublishDir  = "$SolutionDir\publish"
$ProjectPath = "$SolutionDir\Bolcko.Web.App\Bolcko.Web.App.csproj"

Write-Host "`n🔨 Building & Publishing Bolcko.Web in Release mode..." -ForegroundColor Cyan

# 1. Clean old publish
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

# 2. Publish
dotnet publish $ProjectPath `
    -c Release `
    -r linux-x64 `
    --self-contained false `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed!"; exit 1 }

Write-Host "✅ Publish complete: $PublishDir" -ForegroundColor Green

# 3. Copy deploy scripts
Copy-Item "$SolutionDir\deploy\deploy.sh" "$PublishDir\deploy.sh"

# 4. Transfer to server via SCP
if ($ServerIP -eq "YOUR_SERVER_IP") {
    Write-Host "`n⚠️  Server IP not configured. Files published locally at:" -ForegroundColor Yellow
    Write-Host "   $PublishDir" -ForegroundColor Yellow
    Write-Host "`nTo upload manually, run:" -ForegroundColor Cyan
    Write-Host "   scp -r `"$PublishDir`" root@YOUR_SERVER_IP:/tmp/bolcko-deploy" -ForegroundColor White
    Write-Host "   ssh root@YOUR_SERVER_IP 'chmod +x /tmp/bolcko-deploy/deploy.sh && sudo bash /tmp/bolcko-deploy/deploy.sh'" -ForegroundColor White
} else {
    Write-Host "`n📤 Uploading to $ServerUser@$ServerIP ..." -ForegroundColor Cyan
    
    # Create remote directory
    ssh "$ServerUser@$ServerIP" "mkdir -p $ServerPath"
    
    # Upload files
    scp -r "$PublishDir\*" "${ServerUser}@${ServerIP}:${ServerPath}/"
    
    if ($LASTEXITCODE -ne 0) { Write-Error "SCP transfer failed!"; exit 1 }
    
    Write-Host "✅ Files uploaded to $ServerIP:$ServerPath" -ForegroundColor Green
    Write-Host "`n🚀 Running deployment script on server..." -ForegroundColor Cyan
    
    ssh "$ServerUser@$ServerIP" "chmod +x $ServerPath/deploy.sh && sudo bash $ServerPath/deploy.sh"
    
    Write-Host "`n✅ Deployment complete!" -ForegroundColor Green
}
