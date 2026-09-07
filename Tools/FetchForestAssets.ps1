$ErrorActionPreference = 'Stop'
$forestRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Assets/ThirdParty/ForestScans'))
New-Item -ItemType Directory -Path $forestRoot -Force | Out-Null
function Fetch-ForestFile($entry, $destination) {
    if (-not $entry.url) { throw "Missing asset URL for $destination" }
    if (Test-Path -LiteralPath $destination) {
        if ((Get-FileHash -LiteralPath $destination -Algorithm MD5).Hash.ToLowerInvariant() -eq $entry.md5) { return }
        throw "Existing asset differs from upstream: $destination"
    }
    Invoke-WebRequest -Uri $entry.url -OutFile $destination -UseBasicParsing -TimeoutSec 120
    if ((Get-FileHash -LiteralPath $destination -Algorithm MD5).Hash.ToLowerInvariant() -ne $entry.md5) { throw "Asset checksum mismatch: $destination" }
    Write-Output ([IO.Path]::GetFileName($destination))
}
foreach ($assetId in @('rock_moss_set_01', 'fern_02', 'dead_tree_trunk', 'pine_sapling_small')) {
    $files = Invoke-RestMethod -Uri "https://api.polyhaven.com/files/$assetId" -TimeoutSec 30
    $folder = Join-Path $forestRoot $assetId
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    Fetch-ForestFile $files.fbx.'2k'.fbx (Join-Path $folder "$assetId.fbx")
    if ($assetId -eq 'pine_sapling_small') {
        foreach ($mapName in @('bark_diff','bark_nor_gl','twig_diff','twig_nor_gl')) {
            Fetch-ForestFile $files.$mapName.'2k'.jpg (Join-Path $folder ($mapName + '.jpg'))
        }
        Fetch-ForestFile $files.twig_alpha.'2k'.png (Join-Path $folder 'twig_alpha.png')
    } else {
        $resolution = if ($assetId -eq 'fern_02') { '2k' } else { '4k' }
        Fetch-ForestFile $files.Diffuse.$resolution.jpg (Join-Path $folder 'diff.jpg')
        Fetch-ForestFile $files.nor_gl.$resolution.jpg (Join-Path $folder 'nor_gl.jpg')
        Fetch-ForestFile $files.Rough.$resolution.jpg (Join-Path $folder 'rough.jpg')
        if ($assetId -eq 'fern_02') { Fetch-ForestFile $files.Alpha.'2k'.png (Join-Path $folder 'alpha.png') }
    }
}
$ground = Invoke-RestMethod -Uri 'https://api.polyhaven.com/files/forest_leaves_02' -TimeoutSec 30
$groundFolder = Join-Path $forestRoot 'forest_leaves_02'
New-Item -ItemType Directory -Path $groundFolder -Force | Out-Null
Fetch-ForestFile $ground.Diffuse.'4k'.jpg (Join-Path $groundFolder 'diff.jpg')
Fetch-ForestFile $ground.nor_gl.'4k'.jpg (Join-Path $groundFolder 'nor_gl.jpg')
Fetch-ForestFile $ground.Rough.'4k'.jpg (Join-Path $groundFolder 'rough.jpg')

$planks = Invoke-RestMethod -Uri 'https://api.polyhaven.com/files/old_planks_02' -TimeoutSec 30
$plankFolder = Join-Path $forestRoot 'old_planks_02'
New-Item -ItemType Directory -Path $plankFolder -Force | Out-Null
Fetch-ForestFile $planks.Diffuse.'4k'.jpg (Join-Path $plankFolder 'diff.jpg')
Fetch-ForestFile $planks.nor_gl.'4k'.jpg (Join-Path $plankFolder 'nor_gl.jpg')

$fir = Invoke-RestMethod -Uri 'https://api.polyhaven.com/files/fir_tree_01' -TimeoutSec 30
$firFolder = Join-Path $forestRoot 'fir_tree_01'
New-Item -ItemType Directory -Path $firFolder -Force | Out-Null
foreach ($part in @('bark','trunk_a','trunk_b','trunk_c','twig')) {
    foreach ($kind in @('diff','nor_gl')) {
        $mapName = $part + '_' + $kind
        Fetch-ForestFile $fir.$mapName.'2k'.jpg (Join-Path $firFolder ($mapName + '.jpg'))
    }
}
Fetch-ForestFile $fir.twig_alpha.'2k'.png (Join-Path $firFolder 'twig_alpha.png')
# The 249 MB source is deliberately kept outside Assets/Git; PrepareFir.py exports game LODs.
$firSource = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Logs/ForestSource'))
New-Item -ItemType Directory -Path $firSource -Force | Out-Null
Fetch-ForestFile $fir.fbx.'2k'.fbx (Join-Path $firSource 'fir_tree_01.fbx')
