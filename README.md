# OldImpactor
The OldImpactor tool from my 2024 Masters Thesis

# Notes
Note to CV readers: please see my main account for proper use of Git, I could not get it setup here as VS would not let me have two accounts.
Note to everyone: this is a "cleanroom" version of this project that looks a bit nicer and removes protected content from the project's sponsor.

# Setup
- To make this project work, you need a version of the Ladder Logic Verifier. I used `LLVerifier_BuildMay2024`.
- You have to specify the path to your input/output in DataTypes.cs, you also have to specify the path to Clausegen (`LLVerifier_BuildMay2024\\Run\\bin\\TPTPClausegen\\`) here.
- At the top of the Main method in Program.cs you need to specify a path to a .wt2 formatted ladder file and to a folder of .conds.
- You need to create a folder in your input/output path called "Data".
- You will have to run `findPassingConds(string ladderPath, string condDirPath)` the first time you use a new ladder, this will create a lookupTable.txt in your input/output path.
- Ubar iteration filterting is done with `doUbarFiltering(0);` inserted after `findUbarBackwards(rawLadder, ubarConds);` in `public static bool InjectRelative(string ladderPath, string condPath)`
- Test adjustments can be done in the main method
- The five files specified in the Thesis are generated when the tests are finished in your input/output path `Outcomes.txt`, `ChapterPasses.txt`, `InjectionTypes.txt`, `NoErrors.txt` and `ConsoleOutput.txt`
