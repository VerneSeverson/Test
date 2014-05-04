CERTCLILib.dll is generated manually with tlbimp.

This is a COM DLL  used for certificate generation by the ForwardVSLibrary. On Windows 8, the COM functions reside in the CERTCLILib namespace while on Windows 7 they reside in the CERTCLIENTLib namespace. This means that if the reference is added as a COM reference, it will not work properly on both Windows 7 and Windows 8 development PCs. 
See:
https://stackoverflow.com/questions/23445996/best-practice-for-adding-a-com-reference-to-a-version-controlled-visual-studio-p

To resolve this problem, the lib dll is created manually using tlbimp. To do so:
1) Copy windows\system32\certcli.dll to forwardvslibrary\SourceCode\References\CertClientLib 
2) Open up "Developer Command Prompt for VS2012"
3) Run tlbimp certcli.dll

If this COM library is updated, these steps will need to be repeated to pull in the latest updates.

For reference:
http://nach0focht.wordpress.com/tag/exit-module/
http://msdn.microsoft.com/en-us/library/tt0cf3sx%28v=vs.110%29.aspx
