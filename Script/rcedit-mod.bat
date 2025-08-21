@echo off
REM .\rcedit-x64.exe "PDXM_CS2_Uploader_v2.0.0.exe" --set-icon "StarQ_Logo.ico" --set-version-string "CompanyName" "StarQ" --set-version-string "ProductName" "StarQ PDXM CS2 Uploader" --set-product-version "2.0.0"

for %%F in (PDXM_CS2_Uploader_v*.exe) do (
    echo Processing %%F...
    .\rcedit-x64.exe "%%F" --set-icon "StarQ_Logo.ico" ^
        --set-version-string "CompanyName" "StarQ" ^
        --set-version-string "ProductName" "StarQ PDXM CS2 Uploader" ^
        --set-product-version "2.0.0"
)
echo Done.
pause