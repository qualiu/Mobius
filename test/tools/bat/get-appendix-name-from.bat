rem input args (submit options) like : --master yarn-cluster --num-executors 100 --executor-cores 28 --executor-memory 30G --conf xxx=xxxx
rem this script try to get brief sumbit options, set them to AppNameAppendix (-ec-28_-em-30G)
call %~dp0..\check-set-tool-path.bat || exit /b 1
echo ## %* | findstr /I /R "[0-9a-z]" >nul || (echo No input args of submit options, exit %0  && exit /b 1)
rem append AppNameAppendix to application name to be obvious in cluster page. curent AppNameAppendix=%AppNameAppendix%
:: set AppNameAppendix=
for /F "tokens=*" %%a in ('echo %* ^| lzmw -it "--([a-z])\w*-(\w)\S*\s+(\d+\w?)\w*" -o "-$1$2-$3" -PAC ^| lzmw -it "--\S+\s+\S+(\s*\w+)" -o "" -PAC -a ^| lzmw -t "\s+" -o "_" -PAC ^| lzmw -t "^_|_\s*$" -o "" -PAC ') do ( set AppNameAppendix=%%a && exit /b 0)
exit /b 1