@git status | lzmw --nt "(cpp|logs|apps|checkDir)/|\s+../" -PAC | lzmw -it "^.*\s+(\w+\S+)$" --nt "\(|:\s*$|\s+branch\s+" -o "$1" -PAC | lzmw -t / -o \ -a -PAC
:: for /F %f in ('get-changed-files.bat') do git add %f

