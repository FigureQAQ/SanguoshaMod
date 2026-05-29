param(
  [string[]] $Card,
  [string] $ExternalOutDir,
  [string] $ProjectArtDir = (Join-Path $PSScriptRoot "input\equipment-art"),
  [string] $Size = "1536x1024",
  [switch] $Force
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

function ConvertFrom-Utf8Base64([string] $Value) {
  return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Value))
}

if (-not $ExternalOutDir) {
  $ExternalOutDir = Join-Path (Join-Path ([Environment]::GetFolderPath("MyPictures")) (ConvertFrom-Utf8Base64 "5LiJ5Zu95p2AbW9k")) "image2"
}

$apiKey = $env:PINAI_API_KEY
if (-not $apiKey) {
  throw "PINAI_API_KEY is not set. Set it in the environment before running this script."
}

$cards = @(
  @{
    slug = "zhuge"; file = (ConvertFrom-Utf8Base64 "6K+46JGb6L+e5bypLnBuZw==");
    prompt = "Zhuge Repeating Crossbow from Three Kingdoms, fantasy strategy card illustration. An ornate bronze and dark wood repeating crossbow loaded with many bolts, mechanical gears and carved dragon motifs, lying on a war table with scattered arrows and banners. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "zhangba"; file = (ConvertFrom-Utf8Base64 "5LiI5YWr6JuH55+bLnBuZw==");
    prompt = "Zhangba Serpent Spear from Three Kingdoms, fantasy strategy card illustration. A long black iron spear with twin serpent-shaped blades, wrapped red tassels, crackling battle energy, planted in a stormy ancient battlefield. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "qinggang"; file = (ConvertFrom-Utf8Base64 "6Z2S6Yet5YmRLnBuZw==");
    prompt = "Qinggang Sword from Three Kingdoms, fantasy strategy card illustration. A cold blue steel sword drawn from its scabbard, jade inlay, sharp moonlit edge cutting through armor plates, mist and sparks around it. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "guanshi"; file = (ConvertFrom-Utf8Base64 "6LSv55+z5panLnBuZw==");
    prompt = "Guanshi Stonebreaker Axe from Three Kingdoms, fantasy strategy card illustration. A massive two-handed war axe with a heavy stone-cracking head, bronze runes, dust explosions and shattered shields under it. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "hanbing"; file = (ConvertFrom-Utf8Base64 "5a+S5Yaw5YmRLnBuZw==");
    prompt = "Frost Ice Sword from Three Kingdoms, fantasy strategy card illustration. A slender ancient sword of pale steel covered in frost, blue ice crystals, frozen breath, battlefield silhouettes trapped in cold mist. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "qilin"; file = (ConvertFrom-Utf8Base64 "6bqS6bqf5byTLnBuZw==");
    prompt = "Qilin Bow from Three Kingdoms, fantasy strategy card illustration. A majestic recurved bow carved with golden qilin motifs, glowing arrow nocked, wind and golden sparks swirling over a mountain battlefield. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "guding"; file = (ConvertFrom-Utf8Base64 "5Y+k6ZSt5YiALnBuZw==");
    prompt = "Guding Blade from Three Kingdoms, fantasy strategy card illustration. A brutal ancient saber with a blackened edge and bronze ring pommel, blood-red sunset reflections, broken armor and lonely battlefield dust. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "bagua"; file = (ConvertFrom-Utf8Base64 "5YWr5Y2m6Zi1LnBuZw==");
    prompt = "Eight Trigrams defensive array from Three Kingdoms, fantasy strategy card illustration. A mystical circular shield and Bagua formation floating before an ancient general, ink-like yin yang energy, talisman lines deflecting arrows. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "renwang"; file = (ConvertFrom-Utf8Base64 "5LuB546L55u+LnBuZw==");
    prompt = "Renwang Shield from Three Kingdoms, fantasy strategy card illustration. A dark royal shield with lion mask relief and black lacquer, standing against incoming arrows, gold trim, heavy fortress mood. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "baiyin"; file = (ConvertFrom-Utf8Base64 "55m96ZO254uu5a2QLnBuZw==");
    prompt = "Silver Lion Armor from Three Kingdoms, fantasy strategy card illustration. A shining silver lamellar armor with lion-head shoulders, white cloth mantle, healing light and battlefield rain glinting on metal. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "chitu"; file = (ConvertFrom-Utf8Base64 "6LWk5YWULnBuZw==");
    prompt = "Red Hare horse from Three Kingdoms, fantasy strategy card illustration. A powerful crimson war horse galloping through a burning ancient battlefield, fiery mane, ornate golden horse armor, dust and sparks under its hooves, charging army banners in the background. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "dawan"; file = (ConvertFrom-Utf8Base64 "5aSn5a6bLnBuZw==");
    prompt = "Dawan Ferghana war horse from Three Kingdoms, fantasy strategy card illustration. A tall elegant bay war horse with fine armor and flowing mane, galloping across open plains, wind banners and dust trails. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "dilu"; file = (ConvertFrom-Utf8Base64 "55qE5Y2iLnBuZw==");
    prompt = "Dilu horse from Three Kingdoms, fantasy strategy card illustration. A pale legendary horse leaping across a raging river under moonlight, silver mane, splashing water, heroic escape mood. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "yuxi"; file = (ConvertFrom-Utf8Base64 "546J5466LnBuZw==");
    prompt = "Imperial Jade Seal from Three Kingdoms, fantasy strategy card illustration. A square green jade seal with coiled dragon carving, golden imperial light, silk scrolls and palace shadows, symbol of mandate. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "muniu"; file = (ConvertFrom-Utf8Base64 "5pyo54mb5rWB6amsLnBuZw==");
    prompt = "Wooden Ox and Flowing Horse from Three Kingdoms, fantasy strategy card illustration. A clever wooden mechanical supply cart shaped like an ox-horse automaton, brass gears, grain sacks, mountain road logistics. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  },
  @{
    slug = "taiping"; file = (ConvertFrom-Utf8Base64 "5aSq5bmz6KaB5pyvLnBuZw==");
    prompt = "Taiping Taoist scripture from Three Kingdoms, fantasy strategy card illustration. An ancient yellowed magical scroll and book with Taoist talismans, green-gold healing aura, ritual candles and storm clouds. Dramatic painterly 2D digital art, strong silhouette, no text, no watermark, no card frame."
  }
)

function Get-B64FromPayload([string] $Payload) {
  $match = [regex]::Match($Payload, '"b64_json"\s*:\s*"([^"]+)"')
  if ($match.Success) {
    return $match.Groups[1].Value
  }
  return $null
}

function Invoke-PinAIImageGeneration([hashtable] $CardInfo) {
  $body = @{
    model = "gpt-image-2"
    prompt = $CardInfo.prompt
    size = $Size
    response_format = "b64_json"
    stream = $true
  } | ConvertTo-Json -Compress

  $request = [System.Net.Http.HttpRequestMessage]::new(
    [System.Net.Http.HttpMethod]::Post,
    "https://us.pinai-cn.com/v1/images/generations"
  )
  $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $apiKey)
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
      throw "No b64_json image found in PinAI response for $($CardInfo.slug)."
    }

    return [Convert]::FromBase64String($b64)
  }
  finally {
    $client.Dispose()
  }
}

New-Item -ItemType Directory -Force -Path $ExternalOutDir | Out-Null
New-Item -ItemType Directory -Force -Path $ProjectArtDir | Out-Null

$selected = $cards
if ($Card -and $Card.Count -gt 0) {
  $wanted = @{}
  foreach ($name in $Card) { $wanted[$name.ToLowerInvariant()] = $true }
  $selected = $cards | Where-Object {
    $wanted.ContainsKey($_.slug.ToLowerInvariant()) -or $wanted.ContainsKey([System.IO.Path]::GetFileNameWithoutExtension($_.file).ToLowerInvariant())
  }
}

foreach ($item in $selected) {
  $externalPath = Join-Path $ExternalOutDir $item.file
  $projectPath = Join-Path $ProjectArtDir ($item.slug + ".png")

  if ((Test-Path -LiteralPath $externalPath) -and -not $Force) {
    Copy-Item -LiteralPath $externalPath -Destination $projectPath -Force
    Write-Host "COPY $($item.file) -> $($item.slug).png"
    continue
  }

  Write-Host "GENERATE $($item.slug) $($item.file) $Size"
  $bytes = Invoke-PinAIImageGeneration $item
  [System.IO.File]::WriteAllBytes($externalPath, $bytes)
  Copy-Item -LiteralPath $externalPath -Destination $projectPath -Force
  Write-Host "SAVED $externalPath"
}
