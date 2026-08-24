@echo off
chcp 65001 > nul
title Axon POS Launcher
start "" "%~dp0AxonPOS\Axon.UI\bin\Debug\net9.0-windows\Axon.UI.exe"
