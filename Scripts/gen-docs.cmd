@echo off
cd %~dp0

if not exist ..\bin\net46\Microsoft.Coyote.dll goto :nobuild
XmlDocMarkdown --namespace Microsoft.Coyote ..\bin\net46\Microsoft.Coyote.dll ..\docs\_learn\ref --front-matter ..\docs\assets\data\_front.md --visibility protected --toc --toc-prefix /learn/ref --clean

goto :eof

:nobuild
echo please build coyote project first
goto :eof

