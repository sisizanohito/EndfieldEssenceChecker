param(
    [Parameter(Mandatory=$true)]
    [string]$ImagePath,
    [string]$OutputPath = "",
    [string]$LanguageTag = "ja"
)

$ErrorActionPreference = "Stop"
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8NoBom
$OutputEncoding = $utf8NoBom
Add-Type -AssemblyName System.Runtime.WindowsRuntime

$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime]
$null = [Windows.Globalization.Language, Windows.Globalization, ContentType=WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType=WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType=WindowsRuntime]

function Await-Operation($Operation, [Type]$ResultType) {
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object {
            $_.Name -eq "AsTask" -and $_.IsGenericMethodDefinition -and $_.GetParameters().Count -eq 1 -and
            $_.GetParameters()[0].ParameterType.IsGenericType -and
            $_.GetParameters()[0].ParameterType.GetGenericTypeDefinition().FullName -eq 'Windows.Foundation.IAsyncOperation`1'
        } |
        Select-Object -First 1
    if ($null -eq $asTask) {
        throw "System.WindowsRuntimeSystemExtensions.AsTask(IAsyncOperation<T>) was not found."
    }
    $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

function Await-Action($Operation) {
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object {
            $_.Name -eq "AsTask" -and
            $_.GetParameters().Count -eq 1 -and
            $_.GetParameters()[0].ParameterType.Name -eq "IAsyncAction"
        } |
        Select-Object -First 1
    $task = $asTask.Invoke($null, @($Operation))
    $task.Wait()
}

$fullPath = [System.IO.Path]::GetFullPath($ImagePath)
$language = [Windows.Globalization.Language]::new($LanguageTag)
$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage($language)
if ($null -eq $engine) {
    throw "Windows OCR language is not available: $LanguageTag"
}

$file = Await-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($fullPath)) ([Windows.Storage.StorageFile])
$stream = Await-Operation ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
try {
    $decoder = Await-Operation ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
    $bitmap = Await-Operation ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
    $result = Await-Operation ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $result.Text
    }
    else {
        [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($OutputPath), $result.Text, $utf8NoBom)
    }
}
finally {
    if ($stream -ne $null) {
        $stream.Dispose()
    }
}
