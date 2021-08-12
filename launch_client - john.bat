@echo off
set username=john
title CLIENT - %username%
python client.py --username=%username%
pause