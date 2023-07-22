:: Create merge branch for given commit
:: In order to authenticate with ADO you may need to install GIt Credential Manager Core first (https://docs.microsoft.com/en-us/azure/devops/repos/git/set-up-credential-managers?view=azure-devops)
:: This can be run through powershell using .\mastermerge.cmd commit_id
:: 

@echo off
setlocal

if "%~1"=="" (
    echo develop commit id is missing
    exit /b 1
) else (
    set MM_COMMIT_ID=%1
)

git checkout develop || exit /b 1
git pull || exit /b 1

for /f "tokens=2 delims==." %%a in ('wmic os get localdatetime /value') do (set TIMESTAMP=%%a)
set MM_CURRENT_DATE=%TIMESTAMP:~0,4%-%TIMESTAMP:~4,2%-%TIMESTAMP:~6,2%
set MM_MERGE_BRANCH=users/release/mastermerge_%MM_CURRENT_DATE%
echo checkout %MM_MERGE_BRANCH%
git checkout -b %MM_MERGE_BRANCH% %MM_COMMIT_ID% || exit /b 1
git push --set-upstream origin %MM_MERGE_BRANCH%

echo Next step: create pull request in ADO to merge (NOT Squash) branch %MM_MERGE_BRANCH% to master.

endlocal
exit /b 0