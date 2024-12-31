@echo off
setlocal 
docfx.exe build --maxParallelism 1 --exportRawModel --exportViewModel --serve
