@echo off
setlocal 
set mydir=%~dp0
%mydir%..\artifacts\bin\docdb\debug\docdb.exe "Data Source=.;Initial Catalog=AdventureWorks2022;Integrated Security=True;Encrypt=False" %mydir%AdventureWorks2022 "AdventureWorks2022"