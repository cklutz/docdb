@echo off
setlocal 
set mydir=%~dp0
set dbName=%~1
set dbOverrideName=%~2
if not defined dbName set dbName=AdventureWorks2022
if not defined dbOverrideName set dbOverrideName=%dbName%
%mydir%..\artifacts\bin\docdb\debug\docdb.exe "Data Source=.;Initial Catalog=%dbName%;Integrated Security=True;Encrypt=False" %mydir%%dbOverrideName% "%dbOverrideName%"