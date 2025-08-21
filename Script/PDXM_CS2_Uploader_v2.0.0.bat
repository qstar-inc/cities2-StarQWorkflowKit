@echo off
echo ============= Welcome to Cities: Skylines Paradox Mod Publisher =============
echo .............................. Script by StarQ ..............................
set "currentVersion=2.0.0"
echo ............................... Version %currentVersion% ...............................
echo .............................. August 13, 2025 ..............................
echo =============================================================================

setlocal enabledelayedexpansion


set "RESET=[0m"
set "RED=[91m"
set "GREEN=[92m"
set "YELLOW_BLACK=[30;103m"

echo  %RESET%

set "base=%LocalAppData%Low\Colossal Order\Cities Skylines II\.cache\Mods\mods_subscribed"
set "noWFText1=%RED%[WARN] A suitable version of StarQ's Workflow Kit was not found. Subscribe to the latest version of StarQ's Workflow Kit to keep up-to-date.%RESET%"
set "noWFText2=%RED%Click here: https://mods.paradoxplaza.com/mods/92671/Windows%RESET%"

if not exist "%base%" (
	echo mods_subscribed not found
    echo !noWFText1!
    echo !noWFText2!
	pause
    exit /b
)

set "highestNum=0"
set "highestFolder="

set "foundAny="
for /d %%D in ("%base%\92671_*") do (
	if exist "%%D" (
		set "foundAny=1"
        set "folderName=%%~nD"
		for /f "tokens=2 delims=_" %%N in ("!folderName!") do (
			if %%N GTR !highestNum! (
				set "highestNum=%%N"
				set "highestFolder=%%~fD"
			)
		)
	)
)

if not defined highestFolder (
	echo HighestFolder not found
	echo !noWFText1!
    echo !noWFText2!
    pause
    exit /b
)

set "scriptFile="
for %%F in ("!highestFolder!\PDXM_CS2_Uploader*.exe") do (
    if exist "%%~F" (
        set "scriptFile=%%~fF"
    )
)

if not defined scriptFile (
	echo ScriptFile not found
	echo !noWFText1!
    echo !noWFText2!
    pause
    exit /b
)

for %%F in ("!scriptFile!") do (
    for /f "tokens=2 delims=_v" %%V in ("%%~nxF") do (
        set "version=%%V"
		set "version=!version:.exe=!"
    )
)

for /f "tokens=1-3 delims=." %%A in ("%currentVersion%") do (
    set "curMajor=%%A"
    set "curMinor=%%B"
    set "curPatch=%%C"
)

for /f "tokens=1-3 delims=." %%A in ("%version%") do (
    set "verMajor=%%A"
    set "verMinor=%%B"
    set "verPatch=%%C"
)

set "isOld=0"

if !curMajor! LSS !verMajor! (
    set "isOld=1"
) else if !curMajor! EQU !verMajor! (
    if !curMinor! LSS !verMinor! (
        set "isOld=1"
    ) else if !curMinor! EQU !verMinor! (
        if !curPatch! LSS !verPatch! (
            set "isOld=1"
        )
    )
)

if !isOld! EQU 1 (
    echo %YELLOW_BLACK%Current script is outdated. Copying newer version...%RESET%
	
	for %%A in ("!scriptFile!") do set "newScriptName=%%~nxA"
    set "destScript=%~dp0"
	set "destScript=!destScript!!newScriptName!"
	echo !scriptFile!
	echo !destScript!
    copy /y "!scriptFile!" "!destScript!" >error.log 2>&1
	
	if not exist "!scriptFile!" (
		echo %RED%[ERROR] New script not found: "!scriptFile!"%RESET%
		exit /b 1
	)

    if errorlevel 1 (
        echo %RED%[ERROR] Failed to copy newer version.%RESET%
		type error.log
    ) else (
        echo %GREEN%Newer version copied as "!newScriptName!".%RESET%
        echo You can delete this old script manually if you wish.
	)
    pause
    exit /b
)

if defined CSII_MODPUBLISHERPATH (
    echo %GREEN%Toolkit installed and active.%RESET%
) else (
    echo %RED%Modding Toolkit not found. Make sure you have installed the Modding Toolchain from Options > Modding in-game.%RESET%
	pause
	exit /b
)

set "ProjectFolder=%CD%"


for %%A in ("!CSII_MODPUBLISHERPATH!\..\..") do set "GrandParent=%%~fA"
set "pkgFile=!GrandParent!\ColossalOrder.ModTemplate.1.0.0.nupkg"
set "file1=content/Properties/PublishConfiguration.xml"
set "file2=content/Properties/Thumbnail.png"
set "dest=%ProjectFolder%\Properties"


if not exist "%ProjectFolder%\Properties" (
    echo 
    echo %RED%[ERROR] 'Properties' folder is missing.%RESET%
	echo Would you like to create the 'Properties' folder now? ^(Y/N^)
	set /p "confirmprop=> "
    set "confirmprop=!confirmprop: =!"
    if /i "!confirmprop!"=="Y" (
        mkdir "%ProjectFolder%\Properties"
        echo %GREEN%[INFO] 'Properties' folder created.%RESET%
		
		if not exist "!pkgFile!" (
            echo %RED%[ERROR] NUPKG file not found: "!pkgFile!"%RESET%
            pause
            exit /b 1
        )

		powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [IO.Compression.ZipFile]::OpenRead('!pkgFile!'); $errorOccurred = $false; foreach ($f in '!file1!','!file2!') { $entry = $zip.Entries | Where-Object { $_.FullName -ieq $f }; if ($entry) { $out = Join-Path '!dest!' ($entry.Name); $stream = $entry.Open(); $file = [System.IO.File]::Open($out,[System.IO.FileMode]::Create); $stream.CopyTo($file); $file.Close(); $stream.Close(); Write-Host 'Extracted:' $out } else { Write-Host 'Not found:' $f; $errorOccurred = $true } }; $zip.Dispose(); if ($errorOccurred) { exit 1 }" > error.log 2>&1

		if errorlevel 1 (
            echo %RED%[ERROR] PublishConfiguration extraction failed.%RESET%
            pause
            exit /b 1
        ) else (
			echo %GREEN%PublishConfiguration.xml saved. Open and edit the file before continuing. Set 0 as ModId to publish a new mod, or the existing ModId to update an existing mod.%RESET%
			pause
			exit
		)
    ) else (
        echo %RED%[INFO] Operation cancelled. Exiting...%RESET%
        pause
        exit /b
    )
)

if not exist "%ProjectFolder%\Properties\PublishConfiguration.xml" (
    echo 
	if not exist "!pkgFile!" (
		echo %RED%[ERROR] NUPKG file not found: "!pkgFile!"%RESET%
		pause
		exit /b 1
	)

	powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [IO.Compression.ZipFile]::OpenRead('!pkgFile!'); $errorOccurred = $false; foreach ($f in '!file1!','!file2!') { $entry = $zip.Entries | Where-Object { $_.FullName -ieq $f }; if ($entry) { $out = Join-Path '!dest!' ($entry.Name); $stream = $entry.Open(); $file = [System.IO.File]::Open($out,[System.IO.FileMode]::Create); $stream.CopyTo($file); $file.Close(); $stream.Close(); Write-Host 'Extracted:' $out } else { Write-Host 'Not found:' $f; $errorOccurred = $true } }; $zip.Dispose(); if ($errorOccurred) { exit 1 }" > error.log 2>&1

	if errorlevel 1 (
		echo %RED%[ERROR] PublishConfiguration extraction failed.%RESET%
		pause
		exit /b 1
	) else (
		echo %GREEN%PublishConfiguration.xml saved. Open and edit the file before continuing. Set 0 as ModId to publish a new mod, or the existing ModId to update an existing mod.%RESET%
		pause
		exit
	)
)

if not exist "%ProjectFolder%\content" (
    echo 
    echo %RED%[ERROR] 'content' folder is missing.%RESET%
    echo Make sure the 'content' folder with everything you want to upload is in the same folder as this script.
	echo Would you like to create the 'content' folder now? ^(Y/N^)
    set /p "confirmCont=> "
    set "confirmCont=!confirmCont: =!"
    if /i "!confirmCont!"=="Y" (
        mkdir "%ProjectFolder%\content"
        echo %GREEN%[INFO] 'content' folder created.%RESET%
    ) else (
        echo %RED%[INFO] Operation cancelled. Exiting...%RESET%
        pause
        exit /b
    )
)

echo 
echo %GREEN%[INFO] Listing files in 'content' folder by extension:%RESET%

set "extensions="
set "hasFiles=false"
set "hasUnsupportedFiles=false"

for /r "%ProjectFolder%\content" %%F in (*.*) do (
    set "hasFiles=true"
    set "ext=%%~xF"
    if /i "!ext:~1,2!"=="VT" (
        set "hasUnsupportedFiles=true"
    )

    set "found=false"
    for %%E in (!extensions!) do (
        if /i "%%E"=="!ext!" (
            set "found=true"
        )
    )
	
	if not !found! == true (
        set "extensions=!extensions! !ext!"
    )
)

if "%hasFiles%"=="false" (
    echo 
    echo %RED%[INFO] No files found in 'content' folder.%RESET%
    pause
    exit /b
)

if "!hasUnsupportedFiles!"=="true" (
    echo 
    echo %RED%[ERROR] Unsupported files detected.%RESET%
)

for /r "%ProjectFolder%\content" %%F in (*.cok *.Prefab *.Texture *.Geometry *.Surface) do (
    set "fullpath=%%F"
    set "filename=%%~nF"
    set "ext=%%~xF"
    set "parent=%%~dpF"

    if not exist "!parent!!filename!!ext!.cid" (
        echo 
        echo %RED%[ERROR] Missing CID file for: !fullpath:%ProjectFolder%\content\=content\!%RESET%
    )
)

set "rootHasFiles=false"
for %%F in ("%ProjectFolder%\content\*.*") do (
    set "rootHasFiles=true"
    goto :breakRootCheck
)
:breakRootCheck

if "!rootHasFiles!"=="false" (
    echo 
    echo No files found in the root of 'content'. Creating .dummy.txt...
    echo This is a dummy file to keep the folder from being empty. Do not delete before publishing/updating. > "%ProjectFolder%\content\.dummy.txt"
)

for %%E in (!extensions!) do (
    set /a fileCount=0
	for /r "%ProjectFolder%\content" %%F in (*%%E) do (
        set /a fileCount+=1
    )
    
	echo Files with extension: %%E ^(!fileCount!^)
    for /r "%ProjectFolder%\content" %%F in (*%%E) do (
        echo  - %%~nxF
	)
)

for /f "tokens=2 delims==/" %%A in ('findstr /i "<ModId" "%ProjectFolder%\Properties\PublishConfiguration.xml"') do (
    set "ModId=%%A"
)

:: Extract the DisplayName value
for /f "tokens=2 delims==/" %%A in ('findstr /i "<DisplayName" "%ProjectFolder%\Properties\PublishConfiguration.xml"') do (
    set "ModName=%%A"
)
:: Confirm action
set "ModId=!ModId:"=!"
set "ModId=!ModId: =!"
if "!ModId!"=="0" (
    set "action=publish"
    set "toolkit=Publish"
) else (
    set "action=update"
    set "toolkit=NewVersion"
)

echo %YELLOW_BLACK%[INFO] Are you sure you want to !action! the mod !ModName!? ^(Y/N^)%RESET%
set /p "confirm=> "
if /i "!confirm!"=="Y" (
    echo %GREEN%[INFO] Proceeding to %action% the mod "!ModName!"...%RESET%
) else (
    echo %RED%[INFO] Operation cancelled.%RESET%
	pause
    exit /b
)
echo 
echo %YELLOW_BLACK%Starting Mod !action!%RESET%
echo 
"%CSII_MODPUBLISHERPATH%" "!toolkit!" "Properties/PublishConfiguration.xml" -c "content" -v
echo Done
endlocal
pause

endlocal
pause