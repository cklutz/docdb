@echo off
setlocal 
set mydir=%~dp0
set dbName=AdventureWorks2022
set artifacts=%mydir%..\..\artifacts\
%artifacts%bin\docdb\debug_net8.0\docdb.exe metadata -o "%mydir%\metadata" --display-database-name "%dbName%" "Data Source=.;Initial Catalog=%dbName%;Integrated Security=True;Encrypt=False"