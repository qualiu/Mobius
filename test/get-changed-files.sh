git status | lzmw --nt "(cpp|logs|apps|checkDir)/|\s+../" -PAC | lzmw -it "^.*\s+(\w+\S+)$" -o '$1' --nt "\(|:\s*$|\s+branch\s+" -PAC
