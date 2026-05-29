param(
  [string] $BatchFile = (Join-Path $PSScriptRoot "trick-cards.json"),
  [string[]] $Card,
  [string] $ExternalOutDir,
  [string] $ProjectArtDir,
  [string] $Size = "1536x1024",
  [switch] $Force
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

function ConvertFrom-Utf8Base64([string] $Value) {
  return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Value))
}

function Get-B64FromPayload([string] $Payload) {
  $match = [regex]::Match($Payload, '"b64_json"\s*:\s*"([^"]+)"')
  if ($match.Success) {
    return $match.Groups[1].Value
  }
  return $null
}

function Invoke-PinAIImageGeneration([string] $Prompt) {
  $body = @{
    model = "gpt-image-2"
    prompt = $Prompt
    size = $Size
    response_format = "b64_json"
    stream = $true
  } | ConvertTo-Json -Compress

  $request = [System.Net.Http.HttpRequestMessage]::new(
    [System.Net.Http.HttpMethod]::Post,
    "https://us.pinai-cn.com/v1/images/generations"
  )
  $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $script:ApiKey)
  $request.Content = [System.Net.Http.StringContent]::new($body, [System.Text.Encoding]::UTF8, "application/json")

  $client = [System.Net.Http.HttpClient]::new()
  $client.Timeout = [System.Threading.Timeout]::InfiniteTimeSpan
  try {
    $response = $client.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
    $response.EnsureSuccessStatusCode() | Out-Null

    $stream = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
    $reader = [System.IO.StreamReader]::new($stream)
    $raw = [System.Text.StringBuilder]::new()
    $b64 = $null

    while (-not $reader.EndOfStream) {
      $line = $reader.ReadLine()
      if (-not $line) { continue }
      [void] $raw.AppendLine($line)

      if ($line.StartsWith("data:")) {
        $payload = $line.Substring(5).Trim()
        if ($payload -eq "[DONE]") { continue }
        if ($payload -match '"error"\s*:') {
          throw "PinAI image generation returned an error: $payload"
        }
        $found = Get-B64FromPayload $payload
        if ($found) { $b64 = $found }
      }
    }

    if (-not $b64) {
      $b64 = Get-B64FromPayload $raw.ToString()
    }
    if (-not $b64) {
      throw "No b64_json image found in PinAI response."
    }

    return [Convert]::FromBase64String($b64)
  }
  finally {
    $client.Dispose()
  }
}

$script:ApiKey = $env:PINAI_API_KEY
if (-not $script:ApiKey) {
  throw "PINAI_API_KEY is not set. Set it in the environment before running this script."
}

$batch = Get-Content -LiteralPath $BatchFile -Raw -Encoding UTF8 | ConvertFrom-Json
$batchDir = Split-Path -Parent (Resolve-Path -LiteralPath $BatchFile)
$artDefaults = $batch.artDefaults
$externalSubdir = if ($artDefaults.externalSubdir) { $artDefaults.externalSubdir } else { "generated" }

if (-not $ExternalOutDir) {
  $ExternalOutDir = Join-Path (Join-Path ([Environment]::GetFolderPath("MyPictures")) (ConvertFrom-Utf8Base64 "5LiJ5Zu95p2AbW9k")) "image2"
  $ExternalOutDir = Join-Path $ExternalOutDir $externalSubdir
}
if (-not $ProjectArtDir) {
  $ProjectArtDir = Join-Path (Join-Path $batchDir "input") ($externalSubdir + "-art")
}

New-Item -ItemType Directory -Force -Path $ExternalOutDir | Out-Null
New-Item -ItemType Directory -Force -Path $ProjectArtDir | Out-Null

$selected = @($batch.cards)
if ($Card -and $Card.Count -gt 0) {
  $wanted = @{}
  foreach ($name in $Card) {
    foreach ($part in $name.Split(",", [System.StringSplitOptions]::RemoveEmptyEntries)) {
      $wanted[$part.Trim().ToLowerInvariant()] = $true
    }
  }
  $selected = @($batch.cards | Where-Object {
    $wanted.ContainsKey($_.slug.ToLowerInvariant()) -or
    $wanted.ContainsKey($_.id.ToLowerInvariant()) -or
    $wanted.ContainsKey($_.title.ToLowerInvariant())
  })
}

foreach ($item in $selected) {
  $externalFile = if ($item.externalFileB64) {
    ConvertFrom-Utf8Base64 $item.externalFileB64
  } elseif ($item.externalFile) {
    $item.externalFile
  } else {
    $item.art
  }
  $externalPath = Join-Path $ExternalOutDir $externalFile
  $projectPath = Join-Path $ProjectArtDir $item.art

  if ((Test-Path -LiteralPath $externalPath) -and -not $Force) {
    Copy-Item -LiteralPath $externalPath -Destination $projectPath -Force
    Write-Host "COPY $externalFile -> $($item.art)"
    continue
  }

  $prompt = $item.prompt
  if ($artDefaults.promptSuffix) {
    $prompt = "$prompt $($artDefaults.promptSuffix)"
  }

  Write-Host "GENERATE $($item.slug) $externalFile $Size"
  $bytes = Invoke-PinAIImageGeneration $prompt
  [System.IO.File]::WriteAllBytes($externalPath, $bytes)
  Copy-Item -LiteralPath $externalPath -Destination $projectPath -Force
  Write-Host "SAVED $externalPath"
}
