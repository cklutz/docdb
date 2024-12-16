@echo off
setlocal 
set mydir=%~dp0
set dbName=%~1
if not defined dbName set dbName=AdventureWorks2022
%mydir%..\artifacts\bin\docdb\debug\docdb.exe "Data Source=.;Initial Catalog=%dbName%;Integrated Security=True;Encrypt=False" %mydir%%dbName% "%dbName%"