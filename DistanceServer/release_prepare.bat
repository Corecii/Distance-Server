rd ".\Build\Release\" /s /q

del ".\Build\Windows\DistanceServer\DistanceServerBaseExternal.dll*" /s /q
xcopy ".\DistanceServerBaseExternal.dll" ".\Build\Windows\DistanceServer\DistanceServerBaseExternal.dll*" /Y
rd ".\Build\Windows\DistanceServer\Plugins\" /s /q
xcopy ".\Plugins\*.dll" ".\Build\Windows\DistanceServer\Plugins\" /Y
del ".\Build\Windows\DistanceServer\Patcher.exe*" /s /q
xcopy "..\Patcher\Patcher\bin\Debug\Patcher.exe" ".\Build\Windows\DistanceServer\Patcher.exe*" /Y
del ".\Build\Windows\DistanceServer\dnlib.dll*" /s /q
xcopy "..\Patcher\Patcher\bin\Debug\dnlib.dll" ".\Build\Windows\DistanceServer\dnlib.dll*" /Y
del ".\Build\Windows\DistanceServer\DistanceServerWindows64.zip*" /s /q
7z a -tzip ".\Build\Windows\DistanceServerWindows64.zip" ".\Build\Windows\DistanceServer\"
xcopy ".\Build\Windows\DistanceServerWindows64.zip" ".\Build\Release\" /Y

del ".\Build\Linux\DistanceServer\DistanceServerBaseExternal.dll*" /s /q
xcopy ".\DistanceServerBaseExternal.dll" ".\Build\Linux\DistanceServer\DistanceServerBaseExternal.dll*" /Y
rd ".\Build\Linux\DistanceServer\Plugins\" /s /q
xcopy ".\Plugins\*.dll" ".\Build\Linux\DistanceServer\Plugins\" /Y
del ".\Build\Linux\DistanceServer\DistanceServerLinux64.zip*" /s /q
7z a -tzip ".\Build\Linux\DistanceServerLinux64.zip" ".\Build\Linux\DistanceServer\"
xcopy ".\Build\Linux\DistanceServerLinux64.zip" ".\Build\Release\" /Y

rd ".\Build\Windows\Patcher\" /s /q
xcopy "..\Patcher\Patcher\bin\Debug\Patcher.exe" ".\Build\Windows\Patcher\Patcher.exe*" /Y
xcopy "..\Patcher\Patcher\bin\Debug\dnlib.dll" ".\Build\Windows\Patcher\dnlib.dll*" /Y
7z a -tzip ".\Build\Windows\PatcherWindows.zip" ".\Build\Windows\Patcher\"
xcopy ".\Build\Windows\PatcherWindows.zip" ".\Build\Release\" /Y