$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
try {
    if(-not (Get-Command node.exe -ErrorAction SilentlyContinue)){throw 'Install Node.js LTS from https://nodejs.org then run this file again.'}
    $worker=Join-Path $PSScriptRoot 'feedback-worker'
    if(-not (Test-Path -LiteralPath $worker)){$worker=Join-Path (Split-Path $PSScriptRoot -Parent) 'feedback-worker'}
    Push-Location $worker
    try {
        Write-Host 'Forts feedback: Cloudflare -> your Telegram bot.'
        Write-Host 'Bot token is sent to Cloudflare secrets; it is not saved in the app or on disk.'
        $secure=Read-Host 'Bot token from BotFather' -AsSecureString
        $ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try{$bot=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)}
        if($bot -notmatch '^\d+:[A-Za-z0-9_-]{25,}$'){throw 'Invalid bot token format.'}
        $chat=Read-Host 'Telegram chat ID (leave empty to detect after you send /start to the bot)'
        if([string]::IsNullOrWhiteSpace($chat)){
            [void](Read-Host 'Send /start to your bot in Telegram, then press Enter')
            try{$updates=Invoke-RestMethod -Uri ("https://api.telegram.org/bot"+$bot+'/getUpdates') -TimeoutSec 15}catch{throw 'Cannot read bot updates. If this bot uses a webhook, enter your chat ID manually on the next run.'}
            $chats=@($updates.result | ForEach-Object {$_.message.chat} | Where-Object {$_ -and $_.type -eq 'private'} | Sort-Object id -Unique)
            if($chats.Count -eq 0){throw 'No private chat found. Send /start to the bot or enter the chat ID manually.'}
            for($i=0;$i -lt $chats.Count;$i++){Write-Host ($i+1) $chats[$i].first_name $chats[$i].username $chats[$i].id}
            $choice=0
            if(-not [int]::TryParse((Read-Host 'Choose the recipient number above'),[ref]$choice) -or $choice -lt 1 -or $choice -gt $chats.Count){throw 'Invalid recipient number.'}
            $chat=[string]$chats[$choice-1].id
        }
        if($chat -notmatch '^-?\d+$'){throw 'Chat ID must be numeric.'}
        try {
            $me=Invoke-RestMethod -Uri ("https://api.telegram.org/bot"+$bot+'/getMe') -TimeoutSec 15
            $recipient=Invoke-RestMethod -Method Post -Uri ("https://api.telegram.org/bot"+$bot+'/getChat') -Body @{chat_id=$chat} -TimeoutSec 15
            if(-not $me.ok -or -not $recipient.ok){throw 'validation'}
        } catch {throw 'Cannot validate this bot and chat. Check the token, chat ID, and that the bot can access the chat.'}
        & npm.cmd ci --no-audit --no-fund
        if($LASTEXITCODE -ne 0){throw 'npm ci failed.'}
        & npx.cmd wrangler login
        if($LASTEXITCODE -ne 0){throw 'Cloudflare login failed.'}
        # Windows PowerShell 5 treats redirected native stderr as ErrorRecords.
        # Let Wrangler print the complete API reason before reporting failure.
        $previousErrorAction=$ErrorActionPreference
        try {
            $ErrorActionPreference='Continue'
            $deployment=@(& npx.cmd wrangler deploy 2>&1)
            $deployExit=$LASTEXITCODE
        } finally {$ErrorActionPreference=$previousErrorAction}
        $deployment | ForEach-Object {Write-Host $_}
        if($deployExit -ne 0){throw 'Worker deployment failed. See the complete Cloudflare error above.'}
        $url=[regex]::Match(($deployment -join "`n"),'https://[a-zA-Z0-9.-]+\.workers\.dev').Value
        if(-not $url){throw 'No workers.dev URL returned. Enable your workers.dev subdomain in Cloudflare.'}
        @{BOT_TOKEN=$bot;CHAT_ID=$chat} | ConvertTo-Json -Compress | & npx.cmd wrangler secret bulk
        if($LASTEXITCODE -ne 0){throw 'Cloudflare secret upload failed.'}
        $bot=$null;$secure=$null
        $health=Invoke-RestMethod -Uri ($url+'/health') -TimeoutSec 15
        if(-not $health.configured){throw 'Worker secrets are not active yet. Run the setup again shortly.'}
        $target=$PSScriptRoot
        if(-not (Test-Path -LiteralPath (Join-Path $target 'Forts Setup.exe'))){$target=Join-Path (Split-Path $PSScriptRoot -Parent) 'dist'}
        $exe=Join-Path $target 'Forts Setup.exe'
        if(-not (Test-Path -LiteralPath $exe)){throw 'Place Forts Setup.exe beside this setup script, then run it again.'}
        $config=Join-Path $target 'Forts-Feedback.json'
        [IO.File]::WriteAllText($config,(@{endpoint=$url+'/report'} | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
        Compress-Archive -LiteralPath $exe,$config -DestinationPath (Join-Path $target 'Forts-Setup-Feedback-Configured.zip') -Force
        Write-Host 'Done. Distribute Forts-Setup-Feedback-Configured.zip (EXE plus public endpoint config).'
        Write-Host 'The bot token is stored only in Cloudflare secrets. No test report was sent.'
    } finally {Pop-Location;$bot=$null;$secure=$null}
} catch {Write-Host ('Setup failed: '+$_.Exception.Message) -ForegroundColor Red;exit 1}
