REM %1 %2 are arguments for the solutiondir and configuration
REM cd %1
REM echo "IILMerge - Configuration = " %2
REM echo "Curent dir for run = %CD%"
REM set found_exe = 0

REM for /R %f in (ILMerge.exe) do ( 
REM   @IF EXIST %f (
REM      set found_exe = %f 
REM      goto run_file
REM   )
REM )
REM :run_file

REM set found_exe = ".\packages\ILMerge.3.0.29\tools\net452\ILMerge.exe"

REM %found_exe% ".\packages\ILMerge.3.0.29\tools\net452\ILMerge.exe"

REM echo ".\packages\ILMerge.3.0.29\tools\net452\ILMerge.exe" /log /target:exe /out:".\bin\Debug\SourcegenMerged.exe" ".\bin\Debug\Sourcegen.exe" ".\bin\Debug\Newtonsoft.Json.dll" ".\bin\Debug\Ookii.Dialogs.Wpf.dll"
REM ".\packages\ILMerge.3.0.29\tools\net452\ILMerge.exe" /log /target:exe /out:".\bin\Debug\SourcegenMerged.exe" ".\bin\Debug\Sourcegen.exe" ".\bin\Debug\Newtonsoft.Json.dll" ".\bin\Debug\Ookii.Dialogs.Wpf.dll"

REM %.\bin\Debug\Ookii.Wpf