pushd %~dp0
for /F %%d in (' dir /A:D /B ') do if exist %%d\pom.xml call mvn package -f %%d\pom.xml
popd