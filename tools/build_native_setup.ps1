# Build-time tooling only. The shipped executable contains C# + XAML/assets/payload,
# and never starts PowerShell or another script interpreter.
param([switch]$Test, [string]$OutputName = 'Forts Setup.exe')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if ([IO.Path]::GetFileName($OutputName) -ne $OutputName -or -not $OutputName.EndsWith('.exe',[StringComparison]::OrdinalIgnoreCase)) { throw 'OutputName must be a simple executable filename.' }
$native=Join-Path $root 'setup\native'
$build=Join-Path $root 'build'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf=Join-Path $framework 'WPF'
$csc=Join-Path $framework 'csc.exe'
New-Item -ItemType Directory -Force $build | Out-Null
New-Item -ItemType Directory -Force (Join-Path $root 'dist') | Out-Null
. (Join-Path $root 'setup\app\strings.ps1')
$script:English | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $root 'setup\app\en.json') -Encoding UTF8
$refs=@('/r:System.Web.Extensions.dll','/r:System.Net.Http.dll','/r:System.Xaml.dll','/r:System.Windows.Forms.dll',
    '/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll',
    "/r:$wpf\PresentationFramework.dll","/r:$wpf\PresentationCore.dll","/r:$wpf\WindowsBase.dll")
$sources=@(Get-ChildItem -LiteralPath $native -Filter '*.cs' | ForEach-Object FullName)
if (-not (Test-Path -LiteralPath (Join-Path $root 'setup\payload\backgrounds\forts-logo.png'))) { throw 'Forts logo is missing. Rebuild the payload with tools/build_setup.ps1.' }
if ($Test) {
    & $csc /nologo /target:exe "/out:$build\NativeDownloadTests.exe" /r:System.Net.Http.dll /r:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'native\DownloadTests.cs') (Join-Path $native 'About.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native download tests did not compile' }
    & (Join-Path $build 'NativeDownloadTests.exe') $root
    if ($LASTEXITCODE -ne 0) { throw 'Native download tests failed' }
    & $csc /nologo /target:exe "/out:$build\NativeEngineTests.exe" /r:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'native\EngineTests.cs') (Join-Path $native 'Engine.cs') (Join-Path $native 'Customization.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native engine tests did not compile' }
    & (Join-Path $build 'NativeEngineTests.exe') $root
    if ($LASTEXITCODE -ne 0) { throw 'Native engine tests failed' }
    & $csc /nologo /target:exe "/out:$build\BackgroundTests.exe" @refs (Join-Path $PSScriptRoot 'native\BackgroundTests.cs') (Join-Path $native 'Engine.cs') (Join-Path $native 'Customization.cs') (Join-Path $native 'BackgroundImage.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Background tests did not compile' }
    & (Join-Path $build 'BackgroundTests.exe') $root
    if ($LASTEXITCODE -ne 0) { throw 'Background tests failed' }
    & $csc /nologo /target:exe /main:NativeUiTests "/out:$build\NativeUiTests.exe" @refs @sources (Join-Path $PSScriptRoot 'native\UiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native UI tests did not compile' }
    & (Join-Path $build 'NativeUiTests.exe') $root
    if ($LASTEXITCODE -ne 0) { throw 'Native UI tests failed' }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stage=Join-Path $build ('native-pack-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force "$stage\app" | Out-Null
try {
    foreach ($dir in @('assets','xaml')) { Copy-Item -LiteralPath (Join-Path $root "setup\app\$dir") -Destination "$stage\app\$dir" -Recurse }
    foreach ($file in @('en.json','version.json','feedback.json')) { Copy-Item -LiteralPath (Join-Path $root "setup\app\$file") -Destination "$stage\app\$file" }
    Copy-Item -LiteralPath (Join-Path $root 'setup\payload') -Destination "$stage\payload" -Recurse
    # Archive destination must be outside its source tree.
    $archive=Join-Path $build ('native-pack-'+[guid]::NewGuid().ToString('N')+'.zip')
    [IO.Compression.ZipFile]::CreateFromDirectory($stage,$archive)
    $exe=Join-Path (Join-Path $root 'dist') $OutputName
    $icon=Join-Path $build 'forts-setup.ico'
    $options=@('/nologo','/target:winexe','/optimize+',"/out:$exe","/win32manifest:$native\app.manifest","/resource:$archive,pack.zip")
    if(Test-Path -LiteralPath $icon){$options+="/win32icon:$icon"}
    & $csc @options @refs @sources
    if($LASTEXITCODE -ne 0){throw 'Native installer compilation failed'}
    $bundle=Join-Path $root 'dist\Forts-Setup'
    if(Test-Path -LiteralPath $bundle){
        if((Resolve-Path -LiteralPath $bundle).Path -ne (Join-Path $root 'dist\Forts-Setup')){throw 'Unexpected distribution path'}
        Remove-Item -LiteralPath $bundle -Recurse -Force
    }
    New-Item -ItemType Directory $bundle | Out-Null
    Copy-Item -LiteralPath $exe -Destination (Join-Path $bundle 'Forts Setup.exe')
    $zip=Join-Path $root 'dist\Forts-Setup.zip'
    if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
    Compress-Archive -LiteralPath $bundle -DestinationPath $zip
    Write-Output "Built native installer: $exe ($((Get-Item -LiteralPath $exe).Length) bytes)"
} finally {
    if($archive -and (Test-Path -LiteralPath $archive)){Remove-Item -LiteralPath $archive}
    if((Resolve-Path -LiteralPath $stage).Path.StartsWith((Join-Path $root 'build\native-pack-'),[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $stage -Recurse -Force}
}
