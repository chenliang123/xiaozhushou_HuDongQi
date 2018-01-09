@echo off
dir
cd Update
del *.exe
dir
copy /Y D:\\如e小助手_Release\\如e小助手.exe
copy /Y D:\\如e小助手_Release\\如e小助手.exe.config
copy /Y D:\\如e小助手_Release\\RueUpdate.exe  RueUpdate2.exe
pause