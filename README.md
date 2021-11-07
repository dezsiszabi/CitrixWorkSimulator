### Description

A simple console application which sends a keystroke (the letter `A`) at regular intervals to a running Citrix session.

### Usage

```
C:\> CitrixWorkSimulator.exe F:\path\to\file.ica
```

### Note

You need to have the Simulation API enabled in the registry for this application to work.

Under `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM` create a DWORD called `AllowSimulationAPI` with the value `1`.

In order to build the application you need a valid Citrix Workspace installation and the `WFICALibPath` property in `Directory.Build.props` needs to point to the folder containing the `WfIcaLib.dll` file.