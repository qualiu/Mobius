:: for /F %f in ('get-changed-files.bat') do git add %f
@if not "%1" == "1" @git status | lzmw --nt "(cpp|logs|apps|checkDir)/|\s+../" -PAC | lzmw -it "^.*\s+(\w+\S+)$" --nt "\(|:\s*$|\s+branch\s+" -o "$1" -PAC | lzmw -t / -o \ -a -PAC
@if "%1" == "1" for /F "tokens=*" %%f in ('git status ^| lzmw --nt "(cpp|logs|apps|checkDir)/|\s+../" -PAC ^| lzmw -it "^.*\s+(\w+\S+)$" --nt "\(|:\s*$|\s+branch\s+" -o "$1" -PAC ^| lzmw -t / -o \ -a -PAC ') do git add %%f

