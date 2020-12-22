# ShapeScriptMagic
Tiny tool to manipulate ShapeScripts in Sparx Enterprise Architect's MDG Technologies

## Requirements
.Net Core 3.1

## Usage
This tool is a Command Line program. It supports two commands **export** and **modify** 

### export
The export command extracts the EAShapeScripts from all Stereotypes within a MDG-File and stores themn into the specified folder. 

Example:

```
export "%USERPROFILE%\Desktop\ArchiMate3.xml" "%USERPROFILE%\Desktop\ArchiMateShapes"
```

Each Shapescript is stored under the name of the stereotype it belongs to with the fileextension `.shapescript`

### modify
The modify command iterates over all stereotypes in a MDG-File and checks if there is a coresponding `.shapescript`-File in the specified folder. If so the shapescript in the MDG-File is replaced by the one from the folder. The new resulting MDG-File is printed to Standard Out and can be redirected to a new file.

Example:

```
modify "%USERPROFILE%\Desktop\ArchiMate3" "%USERPROFILE%\Desktop\ArchiMate3.xml" > ArchiMate3.xml
```
