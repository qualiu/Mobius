for /f "tokens=*" %%f in ('lzmw -l -f "\.bat$|^(Build.cmd|Clean.cmd)$" -PAC -rp %CD% --nd apps ') do @unix2dos %%f
