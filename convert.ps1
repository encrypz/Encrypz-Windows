Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile('C:/Users/HP/.gemini/antigravity-ide/brain/3813c9f7-586f-4edc-b2d5-916dbcc5ea5f/.user_uploaded/media_1789994809957.png')
$icoStream = [System.IO.File]::Create('e:/Encrypz-Windows/logo.ico')
$bitmap = New-Object System.Drawing.Bitmap $img
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$icon.Save($icoStream)
$icoStream.Close()
$img.Dispose()
$bitmap.Dispose()
$icon.Dispose()
