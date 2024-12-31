@echo off
setlocal 
set mydir=%~dp0
set dbName=AdventureWorks2022
set artifacts=%mydir%..\..\artifacts\
%artifacts%bin\docdb\debug\docdb.exe "Data Source=.;Initial Catalog=%dbName%;Integrated Security=True;Encrypt=False" %mydir%\metadata "%dbName%"