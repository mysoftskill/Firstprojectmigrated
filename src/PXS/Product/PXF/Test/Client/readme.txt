For help with commands, run:

PxsTestClient.exe -help

More info:
----------
More documentation available in OneNote 
Member Services > Member Services Team > PXS > PXS Test Client Readme
https://microsoft.sharepoint.com/teams/osg_unistore/mem/mee/_layouts/OneNote.aspx?id=%2Fteams%2Fosg_unistore%2Fmem%2Fmee%2FShared%20Documents%2FMember%20Services%2FMember%20Services%20Team&wd=target%28PXS.one%7CFEE3BDA5-9B3A-4D52-9F50-A0877EB8856F%2FPXS%20Test%20Client%20Readme%7CF4CEFFD4-F134-418C-AE92-5AEA6E6A2FA4%2F%29


Pre-requisites:
---------------
Client certificates
a. This application requires client certificates to be installed in the local machine. Depending on your environment, this would be either of these:
	pxstest-s2s.api.account.microsoft-int.com


Example usage:
--------------

INT:
	PxsTestClient.exe -e INT -u testuser@outlook-int.com -p password
	
DEV box with server cert validation skipped:
	PxsTestClient.exe -e DEV -d "https://localhost" -s true

PPE targeting a specific VIP,  with server cert validation skipped:
	PxsTestClient.exe -e PPE -d "https://<VIP Address> -s true

PROD:
	PxsTestClient.exe -e PROD