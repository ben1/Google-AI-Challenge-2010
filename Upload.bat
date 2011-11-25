rem Make a zip
del zcsharp_shooter.zip
C:\Progra~1\7-Zip\7z.exe a zcsharp_shooter.zip csharp_shooter

rem Make a backup
del old\csharp_shooter
mkdir old\csharp_shooter
copy csharp_shooter old\csharp_shooter\
copy bin\debug\planetwars.exe old\csharp_shooter\




















