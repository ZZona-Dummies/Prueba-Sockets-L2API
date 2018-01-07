@echo off
"SonarQube/SonarQube.Scanner.MSBuild.exe" begin /k:"prueba-sockets" /o:"ikillnukes-github" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="6b8c2a27638d51285c5177deef36f12f0c26325c"
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Dummy-Socket.sln /t:Rebuild /p:VisualStudioversion=14.0;Configuration=Release;Platform="Any CPU"
"SonarQube/SonarQube.Scanner.MSBuild.exe" end /d:sonar.login="6b8c2a27638d51285c5177deef36f12f0c26325c"
pause