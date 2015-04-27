## Packo 0.0.1
JavaScript bundler and minifier utility

Platform: windows

Requirements: .NET 4.5 or greater

Nuget dependencies: YUICompressor.NET.2.7.0.0

**Usage:** packo.exe "C:\Project\Scripts\build.json"

**"build.json" example**:

    {
      "package":"library",
      "version":"0.0.1",
      "source":"./SourceCode",
      "release":"./Release",
      "actions":[{ "type":"bundle",
                   "filename":"{package}.{version}.js",
				"settings":[{"key":"enclose","value":"release.js"},
							{"key":"license","value":"license.txt"}] 
			  },
			 {"type":"minify","filename":"{filename}.min.{extension}","ignore":["*.min.*"]}]
    }

###Actions:

**Bundle**:
The tool will bundle all ".js" files found in the **source** directory into a new file in the **release** directory.

**Minify**:
The tool fill minify all ".js" files found in the **release** directory into a new file in the same directory in the following format (unless a different format is specified) {filename}.min.{extension}. 




